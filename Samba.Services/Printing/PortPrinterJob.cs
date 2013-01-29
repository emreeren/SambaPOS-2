using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Printing;

namespace Samba.Services.Printing
{
    class PortPrinterJob : AbstractPrintJob
    {
        public PortPrinterJob(Printer printer)
            : base(printer)
        { }

        public override void DoPrint(string[] lines)
        {
            foreach (var line in lines)
            {
                var data = line.Contains("<") ? line.Split('<').Where(x => !string.IsNullOrEmpty(x)).Select(x => '<' + x) : line.Split('#');
                data = PrinterHelper.AlignLines(data, Printer.CharsPerLine, false);
                data = PrinterHelper.ReplaceChars(data, Printer.ReplacementPattern);
                foreach (var s in data)
                {
                    if (s.Trim().ToLower() == "<w>")
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                    else if (s.ToLower().StartsWith("<lb"))
                    {
                        SerialPortService.WritePort(Printer.ShareName, RemoveTag(s) + "\n\r");
                    }
                    else if (s.ToLower().StartsWith("<xct"))
                    {
                        var lineData = s.ToLower().Replace("<xct", "").Trim(new[] { ' ', '<', '>' });
                        SerialPortService.WriteCommand(Printer.ShareName, lineData, Printer.CodePage);
                    }
                    else
                    {
                        SerialPortService.WritePort(Printer.ShareName, RemoveTag(s), Printer.CodePage);
                    }
                }
            }
        }

        public override void DoPrint(FlowDocument document)
        {
            return;
        }
    }
}
