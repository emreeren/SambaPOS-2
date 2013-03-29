using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Documents;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Printing;

namespace Samba.Services.Printing
{
    public class CachePrinterJob : AbstractPrintJob
    {
        public static string[] LastPrintedContent { get; private set; }
       
        private static readonly object syncObject = new object();
        public CachePrinterJob(Printer printer)
            : base(printer)
        {
        }

        public override void DoPrint(string[] lines)
        {
            lock (syncObject)
            {
                LastPrintedContent = new string[lines.Count()];
                lines.CopyTo(LastPrintedContent, 0);
            }
        }
        public override void DoPrint(FlowDocument document)
        {
            DoPrint(PrinterTools.FlowDocumentToSlipPrinterFormat(document, Printer.CharsPerLine));
        }
    }
}
