﻿using System;
using System.Collections.Generic;
using Samba.Domain.Models.Settings;
using Samba.Localization.Properties;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.SettingsModule
{
    public class PrinterViewModel : EntityViewModelBase<Printer>
    {
        public PrinterViewModel(Printer model)
            : base(model)
        {
            
        }

        public IList<string> PrinterTypes { get { return new[] { Resources.TicketPrinter, Resources.Text, Resources.Html, Resources.PortPrinter, Resources.DemoPrinter, Resources.CachePrinter, Resources.ZmqPrinter }; } }

        public string ShareName { get { return Model.ShareName; } set { Model.ShareName = value; } }
        public string PrinterType
        {
            get { return PrinterTypes[Model.PrinterType]; }
            set { Model.PrinterType = PrinterTypes.IndexOf(value); }
        }

        public int CodePage { get { return Model.CodePage; } set { Model.CodePage = value; } }
        public int CharsPerLine { get { return Model.CharsPerLine; } set { Model.CharsPerLine = value; } }
        public int PageHeight { get { return Model.PageHeight; } set { Model.PageHeight = value; } }
        public string ReplacementPattern { get { return Model.ReplacementPattern; } set { Model.ReplacementPattern = value; } }

        private IEnumerable<string> _printerNames;
        public IEnumerable<string> PrinterNames
        {
            get { return _printerNames ?? (_printerNames = GetPrinterNames()); }
        }

        private static IEnumerable<string> GetPrinterNames()
        {
            return AppServices.PrintService.GetPrinterNames();
        }

        public override Type GetViewType()
        {
            return typeof(PrinterView);
        }

        public override string GetModelTypeString()
        {
            return Resources.Printer;
        }
    }
}
