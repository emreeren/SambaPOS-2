using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Presentation.Common.Services;

namespace Samba.Modules.SettingsModule
{
    public class TerminalViewModel : EntityViewModelBase<Terminal>
    {
        public TerminalViewModel(Terminal model)
            : base(model)
        {
            SelectPrintJobsCommand = new CaptionCommand<string>(Resources.SelectPrintJob, OnAddPrintJob);
        }

        private IWorkspace _workspace;

        public bool IsDefault { get { return Model.IsDefault; } set { Model.IsDefault = value; } }
        public bool AutoLogout { get { return Model.AutoLogout; } set { Model.AutoLogout = value; } }
        public bool HideExitButton { get { return Model.HideExitButton; } set { Model.HideExitButton = value; } }
        public int? DepartmentId { get { return Model.DepartmentId; } set { Model.DepartmentId = value.GetValueOrDefault(0); } }
        public Printer SlipReportPrinter { get { return Model.SlipReportPrinter; } set { Model.SlipReportPrinter = value; } }
        public Printer ReportPrinter { get { return Model.ReportPrinter; } set { Model.ReportPrinter = value; } }
        public ObservableCollection<PrintJob> PrintJobs { get; set; }
        public ObservableCollection<Department> Departments { get; set; }
        public IEnumerable<Printer> Printers { get; private set; }
        public IEnumerable<PrinterTemplate> PrinterTemplates { get; private set; }

        public ICaptionCommand SelectPrintJobsCommand { get; set; }

        public override Type GetViewType()
        {
            return typeof(TerminalView);
        }

        public override string GetModelTypeString()
        {
            return Resources.Terminal;
        }

        protected override void Initialize(IWorkspace workspace)
        {
            _workspace = workspace;
            Printers = workspace.All<Printer>();
            PrinterTemplates = workspace.All<PrinterTemplate>();
            PrintJobs = new ObservableCollection<PrintJob>(Model.PrintJobs);
            Departments = new ObservableCollection<Department>(workspace.All<Department>());
        }

        private void OnAddPrintJob(string obj)
        {
            IList<IOrderable> values = new List<IOrderable>(_workspace.All<PrintJob>()
                .Where(x => PrintJobs.SingleOrDefault(y => y.Id == x.Id) == null));

            IList<IOrderable> selectedValues = new List<IOrderable>(PrintJobs.Select(x => x));

            var choosenValues =
                InteractionService.UserIntraction.ChooseValuesFrom(values, selectedValues, Resources.PrintJobList,
                string.Format(Resources.SelectPrintJobsForTerminalHint_f, Model.Name), Resources.PrintJob, Resources.PrintJobs);

            PrintJobs.Clear();
            Model.PrintJobs.Clear();

            foreach (PrintJob choosenValue in choosenValues)
            {
                Model.PrintJobs.Add(choosenValue);
                PrintJobs.Add(choosenValue);
            }
        }

        protected override string GetSaveErrorMessage()
        {
            if (Model.IsDefault)
            {
                var terminal = Dao.Query<Terminal>(x => x.IsDefault).SingleOrDefault();
                if (terminal != null && terminal.Id != Model.Id)
                    return Resources.SaveErrorMultipleDefaultTerminals;
            }
            return base.GetSaveErrorMessage();
        }
    }
}
