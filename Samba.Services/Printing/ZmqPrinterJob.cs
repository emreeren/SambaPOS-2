using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Documents;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Printing;
using ZeroMQ;

namespace Samba.Services.Printing
{
    class ZmqPrinterJob : AbstractPrintJob
    {

        private static readonly ZmqContext Context = ZmqContext.Create();

        private static  ZmqSocket _socket; 


        public ZmqPrinterJob(Printer printer)
            : base(printer)
        {
            if (string.IsNullOrEmpty(Printer.ShareName))
            {
                throw new FormatException("ZmqPrinter Sharename must not be empty and be defined as 'PUSH/PULL#tcp://host:port#HWM");
            }
            string[] tokens = printer.ShareName.Split('#');
            if (tokens.Length < 2)
            {
                throw new FormatException("ZmqPrinter Sharename must be defined as 'PUSH/PULL#tcp://host:port#HWM");
            }
            if (_socket != null)
            {
                return;
            }

            if (String.Compare("PUSH", tokens[0], true, CultureInfo.CurrentCulture) == 0)
            {
                _socket = Context.CreateSocket(SocketType.PUSH);
                _socket.Connect(tokens[1]);
            }
            else if (String.Compare("PUB", tokens[0], true, CultureInfo.CurrentCulture) == 0)
            {
                _socket = Context.CreateSocket(SocketType.XPUB);
                _socket.Bind(tokens[1]);
            }
            else
            {
                throw new FormatException("ZmqPrinter Sharename must be defined as 'PUSH/PULL#tcp://host:port#HWM");          
            }
            _socket.SendHighWatermark = tokens.Length == 3 ? int.Parse(tokens[2]) : 1;

        }

        public override void DoPrint(string[] lines)
        {
            Debug.Assert(!string.IsNullOrEmpty(Printer.ShareName));

            lines = PrinterHelper.AlignLines(lines, Printer.CharsPerLine, false).ToArray();
            lines = PrinterHelper.ReplaceChars(lines, Printer.ReplacementPattern).ToArray();
            var text = lines.Aggregate("", (current, s) => current + RemoveTagFmt(s));
            _socket.Send(text, Encoding.UTF8);

        }


        private static string RemoveTagFmt(string s)
        {
            var result = RemoveTag(s.Replace("|", " "));
            if (!string.IsNullOrEmpty(result)) return result + "\r\n";
            return "";
        }

        public override void DoPrint(FlowDocument document)
        {
            DoPrint(PrinterTools.FlowDocumentToSlipPrinterFormat(document, Printer.CharsPerLine));
        }
    }
}
