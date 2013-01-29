using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Modules.SettingsModule
{
    class PrintJobViewModel : EntityViewModelBase<PrintJob>
    {
        public PrintJobViewModel(PrintJob model)
            : base(model)
        {
            _newPrinterMaps = new List<PrinterMap>();
            AddPrinterMapCommand = new CaptionCommand<string>(Resources.Add, OnAddPrinterMap);
            DeletePrinterMapCommand = new CaptionCommand<string>(Resources.Delete, OnDelete, CanDelete);
        }

        private IWorkspace _workspace;

        public ICaptionCommand AddPrinterMapCommand { get; set; }
        public ICaptionCommand DeletePrinterMapCommand { get; set; }

        private readonly IList<string> _whenToPrintTypes = new[] { Resources.Manual, Resources.WhenNewLinesAddedToTicket, Resources.WhenTicketPaid };
        private readonly IList<string> _whatToPrintTypes = new[] { Resources.AllLines, Resources.OnlyNewLines, Resources.LinesGroupedByBarcode, Resources.LinesGroupedByGroupCode, Resources.LinesGroupedByTag, Resources.LastLinesByPrinterLineCount, Resources.LastPaidLines };
        public IList<string> WhenToPrintTypes { get { return _whenToPrintTypes; } }
        public IList<string> WhatToPrintTypes { get { return _whatToPrintTypes; } }

        public IEnumerable<Department> Departments { get { return GetAllDepartments(); } }
        public IEnumerable<Printer> Printers { get { return _workspace.All<Printer>(); } }
        public IEnumerable<PrinterTemplate> PrinterTemplates { get { return _workspace.All<PrinterTemplate>(); } }

        private readonly IList<PrinterMap> _newPrinterMaps;

        private ObservableCollection<PrinterMapViewModel> _printerMaps;
        public ObservableCollection<PrinterMapViewModel> PrinterMaps { get { return _printerMaps ?? (_printerMaps = GetPrinterMaps()); } }

        public PrinterMapViewModel SelectedPrinterMap { get; set; }

        public string ButtonText { get { return Model.ButtonText; } set { Model.ButtonText = value; } }

        public string WhenToPrint { get { return _whenToPrintTypes[Model.WhenToPrint]; } set { Model.WhenToPrint = _whenToPrintTypes.IndexOf(value); } }
        public string WhatToPrint { get { return _whatToPrintTypes[Model.WhatToPrint]; } set { Model.WhatToPrint = _whatToPrintTypes.IndexOf(value); } }
        public bool LocksTicket { get { return Model.LocksTicket; } set { Model.LocksTicket = value; } }
        public bool CloseTicket { get { return Model.CloseTicket; } set { Model.CloseTicket = value; } }
        public bool UseFromPos { get { return Model.UseFromPos; } set { Model.UseFromPos = value; } }
        public bool UseFromPaymentScreen { get { return Model.UseFromPaymentScreen; } set { Model.UseFromPaymentScreen = value; } }
        public bool UseFromTerminal { get { return Model.UseFromTerminal; } set { Model.UseFromTerminal = value; } }
        public bool UseForPaidTickets { get { return Model.UseForPaidTickets; } set { Model.UseForPaidTickets = value; } }
        public bool ExcludeVat { get { return Model.ExcludeVat; } set { Model.ExcludeVat = value; } }
        public string PrintJobHint
        {
            get
            {
                if (Model.Id > 0 && !AppServices.Terminals.Any(x => x.PrintJobs.Any(y => y.Id == Model.Id)))
                {
                    if (Dao.Count<Terminal>(x => x.PrintJobs.Count(y => y.Id == Model.Id) > 0) > 0) return "";
                    return Resources.PrintJobHint;
                }
                return "";
            }
        }

        public bool AutoPrintIfCash
        {
            get { return Model.AutoPrintIfCash; }
            set
            {
                Model.AutoPrintIfCash = value;
                RaisePropertyChanged("AutoPrintIfCash");
            }
        }

        public bool AutoPrintIfCreditCard
        {
            get { return Model.AutoPrintIfCreditCard; }
            set
            {
                Model.AutoPrintIfCreditCard = value;
                RaisePropertyChanged("AutoPrintIfCreditCard");
            }
        }

        public bool AutoPrintIfTicket
        {
            get { return Model.AutoPrintIfTicket; }
            set
            {
                Model.AutoPrintIfTicket = value;
                RaisePropertyChanged("AutoPrintIfTicket");
            }
        }

        private IEnumerable<Department> GetAllDepartments()
        {
            IList<Department> result = new List<Department>(_workspace.All<Department>().OrderBy(x => x.Name));
            result.Insert(0, Department.All);
            return result;
        }

        private ObservableCollection<PrinterMapViewModel> GetPrinterMaps()
        {
            return new ObservableCollection<PrinterMapViewModel>(
                    Model.PrinterMaps.Select(
                    printerMap => new PrinterMapViewModel(printerMap, _workspace)));
        }

        public override Type GetViewType()
        {
            return typeof(PrintJobView);
        }

        public override string GetModelTypeString()
        {
            return Resources.PrintJob;
        }

        protected override void Initialize(IWorkspace workspace)
        {
            _workspace = workspace;
        }

        protected override void OnSave(string value)
        {
            foreach (var printerMap in _printerMaps)
            {
                if (printerMap.Department == Department.All) printerMap.Department = null;
                if (printerMap.MenuItem == MenuItem.All) printerMap.MenuItem = null;
                if (printerMap.MenuItemGroupCode == "*") printerMap.MenuItemGroupCode = null;
                if (printerMap.TicketTag == "*") printerMap.TicketTag = null;
            }

            foreach (var newPrinterMap in _newPrinterMaps)
            {
                if (newPrinterMap.Printer != null)
                {
                    if (newPrinterMap.Department == Department.All) newPrinterMap.Department = null;
                    if (newPrinterMap.MenuItem == MenuItem.All) newPrinterMap.MenuItem = null;
                    if (newPrinterMap.MenuItemGroupCode == "*") newPrinterMap.MenuItemGroupCode = null;
                    if (newPrinterMap.TicketTag == "*") newPrinterMap.TicketTag = null;
                    _workspace.Add(newPrinterMap);
                }
            }
            base.OnSave(value);
            _newPrinterMaps.Clear();
        }

        private void OnDelete(string obj)
        {
            if (InteractionService.UserIntraction.AskQuestion(Resources.DeleteSelectedMappingQuestion))
            {
                var map = SelectedPrinterMap.Model;
                PrinterMaps.Remove(SelectedPrinterMap);
                Model.PrinterMaps.Remove(map);
                if (_newPrinterMaps.Contains(map))
                    _newPrinterMaps.Remove(map);
                else
                {
                    _workspace.Delete(map);
                    _workspace.CommitChanges();
                }
            }
        }

        private bool CanDelete(string arg)
        {
            return SelectedPrinterMap != null;
        }

        private void OnAddPrinterMap(object obj)
        {
            var map = new PrinterMap { Department = Department.All, MenuItem = MenuItem.All, MenuItemGroupCode = "*" };
            var mapModel = new PrinterMapViewModel(map, _workspace);
            Model.PrinterMaps.Add(map);
            PrinterMaps.Add(mapModel);
            _newPrinterMaps.Add(map);
        }
    }
}
