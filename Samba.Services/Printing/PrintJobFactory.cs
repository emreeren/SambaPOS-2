using Samba.Domain.Models.Settings;

namespace Samba.Services.Printing
{
    public static class PrintJobFactory
    {
        public static AbstractPrintJob CreatePrintJob(Printer printer)
        {
            if (printer.PrinterType == 1)
                return new TextPrinterJob(printer);
            if (printer.PrinterType == 2)
                return new HtmlPrinterJob(printer);
            if (printer.PrinterType == 3)
                return new PortPrinterJob(printer);
            if (printer.PrinterType == 4)
                return new DemoPrinterJob(printer);
            if (printer.PrinterType == 5)
                return new CachePrinterJob(printer);
            if (printer.PrinterType == 6)
                return new ZmqPrinterJob(printer);
            return new SlipPrinterJob(printer);
        }
    }
}
