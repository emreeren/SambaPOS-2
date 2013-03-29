﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using Samba.Domain;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Data.Serializer;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Persistance.Data;

namespace Samba.Services.Printing
{
    internal class PrinterData
    {
        public Printer Printer { get; set; }
        public PrinterTemplate PrinterTemplate { get; set; }
        public Ticket Ticket { get; set; }
    }

    internal class TicketData
    {
        public Ticket Ticket { get; set; }
        public IEnumerable<TicketItem> TicketItems { get; set; }
        public PrintJob PrintJob { get; set; }
    }

    public static class TicketPrinter
    {
        private static PrinterMap GetPrinterMapForItem(IEnumerable<PrinterMap> printerMaps, Ticket ticket, TicketItem ticketItem)
        {
            var menuItemGroupCode = Dao.Single<MenuItem, string>(ticketItem.MenuItemId, x => x.GroupCode);

            var maps = printerMaps;

            maps = maps.Count(x => !string.IsNullOrEmpty(x.TicketTag) && !string.IsNullOrEmpty(ticket.GetTagValue(x.TicketTag))) > 0
                ? maps.Where(x => !string.IsNullOrEmpty(x.TicketTag) && !string.IsNullOrEmpty(ticket.GetTagValue(x.TicketTag)))
                : maps.Where(x => string.IsNullOrEmpty(x.TicketTag));

            maps = maps.Count(x => x.Department != null && x.Department.Id == ticketItem.DepartmentId) > 0
                       ? maps.Where(x => x.Department != null && x.Department.Id == ticketItem.DepartmentId)
                       : maps.Where(x => x.Department == null);

            maps = maps.Count(x => x.MenuItemGroupCode == menuItemGroupCode) > 0
                       ? maps.Where(x => x.MenuItemGroupCode == menuItemGroupCode)
                       : maps.Where(x => x.MenuItemGroupCode == null);

            maps = maps.Count(x => x.MenuItem != null && x.MenuItem.Id == ticketItem.MenuItemId) > 0
                       ? maps.Where(x => x.MenuItem != null && x.MenuItem.Id == ticketItem.MenuItemId)
                       : maps.Where(x => x.MenuItem == null);

            return maps.FirstOrDefault();
        }
        private static IEnumerable<PrinterMap> GetPrinterMapsForItem(IEnumerable<PrinterMap> printerMaps, Ticket ticket, TicketItem ticketItem)
        {
            var menuItemGroupCode = Dao.Single<MenuItem, string>(ticketItem.MenuItemId, x => x.GroupCode);

            var maps = printerMaps;

            maps = maps.Count(x => !string.IsNullOrEmpty(x.TicketTag) && !string.IsNullOrEmpty(ticket.GetTagValue(x.TicketTag))) > 0
                ? maps.Where(x => !string.IsNullOrEmpty(x.TicketTag) && !string.IsNullOrEmpty(ticket.GetTagValue(x.TicketTag)))
                : maps.Where(x => string.IsNullOrEmpty(x.TicketTag));

            maps = maps.Count(x => x.Department != null && x.Department.Id == ticketItem.DepartmentId) > 0
                       ? maps.Where(x => x.Department != null && x.Department.Id == ticketItem.DepartmentId)
                       : maps.Where(x => x.Department == null);

            maps = maps.Count(x => x.MenuItemGroupCode == menuItemGroupCode) > 0
                       ? maps.Where(x => x.MenuItemGroupCode == menuItemGroupCode)
                       : maps.Where(x => x.MenuItemGroupCode == null);

            maps = maps.Count(x => x.MenuItem != null && x.MenuItem.Id == ticketItem.MenuItemId) > 0
                       ? maps.Where(x => x.MenuItem != null && x.MenuItem.Id == ticketItem.MenuItemId)
                       : maps.Where(x => x.MenuItem == null);

            return maps;
        }

        public static void AutoPrintTicket(Ticket ticket)
        {
            foreach (var customPrinter in AppServices.CurrentTerminal.PrintJobs.Where(x => !x.UseForPaidTickets))
            {
                if (ShouldAutoPrint(ticket, customPrinter))
                    ManualPrintTicket(ticket, customPrinter);
            }
        }

        public static void ManualPrintTicket(Ticket ticket, PrintJob customPrinter)
        {
            if (customPrinter.LocksTicket) ticket.RequestLock();
            ticket.AddPrintJob(customPrinter.Id);
            PrintOrders(customPrinter, ticket);
        }

        private static bool ShouldAutoPrint(Ticket ticket, PrintJob customPrinter)
        {
            if (customPrinter.WhenToPrint == (int)WhenToPrintTypes.Manual) return false;
            if (customPrinter.WhenToPrint == (int)WhenToPrintTypes.Paid)
            {
                if (ticket.DidPrintJobExecuted(customPrinter.Id)) return false;
                if (!ticket.IsPaid) return false;
                if (!customPrinter.AutoPrintIfCash && !customPrinter.AutoPrintIfCreditCard && !customPrinter.AutoPrintIfTicket) return false;
                if (customPrinter.AutoPrintIfCash && ticket.Payments.Count(x => x.PaymentType == (int)PaymentType.Cash) > 0) return true;
                if (customPrinter.AutoPrintIfCreditCard && ticket.Payments.Count(x => x.PaymentType == (int)PaymentType.CreditCard) > 0) return true;
                if (customPrinter.AutoPrintIfTicket && ticket.Payments.Count(x => x.PaymentType == (int)PaymentType.Ticket) > 0) return true;
            }
            if (customPrinter.WhenToPrint == (int)WhenToPrintTypes.NewLinesAdded && ticket.GetUnlockedLines().Count() > 0) return true;
            return false;
        }

        public static void PrintOrders(PrintJob printJob, Ticket ticket)
        {
            if (printJob.ExcludeVat)
            {
                ticket = ObjectCloner.Clone(ticket);
                ticket.TicketItems.ToList().ForEach(x => x.VatIncluded = false);
            }

            IEnumerable<TicketItem> ti;
            switch (printJob.WhatToPrint)
            {
                case (int)WhatToPrintTypes.NewLines:
                    ti = ticket.GetUnlockedLines();
                    break;
                case (int)WhatToPrintTypes.GroupedByBarcode:
                    ti = GroupLinesByValue(ticket, x => x.Barcode ?? "", "1", true);
                    break;
                case (int)WhatToPrintTypes.GroupedByGroupCode:
                    ti = GroupLinesByValue(ticket, x => x.GroupCode ?? "", Resources.UndefinedWithBrackets);
                    break;
                case (int)WhatToPrintTypes.GroupedByTag:
                    ti = GroupLinesByValue(ticket, x => x.Tag ?? "", Resources.UndefinedWithBrackets);
                    break;
                case (int)WhatToPrintTypes.LastLinesByPrinterLineCount:
                    ti = GetLastItems(ticket, printJob);
                    break;
                case (int)WhatToPrintTypes.LastPaidItems:
                    ti = GetLastPaidItems(ticket).ToList();
                    ticket = ObjectCloner.Clone(ticket);
                    ticket.TicketItems.Clear();
                    ticket.PaidItems.Clear();
                    ticket.Payments.Clear();
                    ti.ToList().ForEach(x => ticket.TicketItems.Add(x));
                    break;
                default:
                    ti = ticket.TicketItems.OrderBy(x => x.Id).ToList();
                    break;
            }

            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new Action(
                    delegate
                    {
                        try
                        {
                            InternalPrintOrders(printJob, ticket, ti);
                        }
                        catch (Exception e)
                        {
                            AppServices.LogError(e, string.Format(Resources.PrintingErrorMessage_f, e.Message));
                        }
                    }));
        }

        private static IEnumerable<TicketItem> GetLastPaidItems(Ticket ticket)
        {
            var result = ticket.GetPaidItems().Select(x => ticket.TicketItems.First(y => y.MenuItemId == x)).ToList();
            result = result.Select(ObjectCloner.Clone).ToList();
            foreach (var ticketItem in result)
            {
                ticketItem.Quantity = ticket.GetPaidItemQuantity(ticketItem.MenuItemId);
            }
            return result;
        }

        private static IEnumerable<TicketItem> GetLastItems(Ticket ticket, PrintJob printJob)
        {
            if (ticket.TicketItems.Count > 1)
            {
                var printer = printJob.PrinterMaps.Count == 1 ? printJob.PrinterMaps[0]
                    : GetPrinterMapForItem(printJob.PrinterMaps, ticket, ticket.TicketItems.Last());
                var result = ticket.TicketItems.OrderByDescending(x => x.CreatedDateTime).ToList();
                if (printer.Printer.PageHeight > 0)
                    result = result.Take(printer.Printer.PageHeight).ToList();
                return result;
            }
            return ticket.TicketItems.ToList();
        }

        private static IEnumerable<TicketItem> GroupLinesByValue(Ticket ticket, Func<MenuItem, object> selector, string defaultValue, bool calcDiscounts = false)
        {
            var discounts = calcDiscounts ? ticket.GetDiscountAndRoundingTotal() : 0;
            var di = discounts > 0 ? discounts / ticket.GetPlainSum() : 0;
            var cache = new Dictionary<string, decimal>();
            foreach (var ticketItem in ticket.TicketItems.OrderBy(x => x.Id).ToList())
            {
                var item = ticketItem;
                var value = selector(AppServices.DataAccessService.GetMenuItem(item.MenuItemId)).ToString();
                if (string.IsNullOrEmpty(value)) value = defaultValue;
                if (!cache.ContainsKey(value))
                    cache.Add(value, 0);
                var total = (item.GetTotal());
                cache[value] += Decimal.Round(total - (total * di), 2);
            }
            return cache.Select(x => new TicketItem
                                         {
                                             MenuItemName = x.Key,
                                             Price = x.Value,
                                             Quantity = 1,
                                             PortionCount = 1,
                                             CurrencyCode = LocalSettings.CurrencySymbol
                                         });
        }

        private static void InternalPrintOrders(PrintJob printJob, Ticket ticket, IEnumerable<TicketItem> ticketItems)
        {
            if (printJob.PrinterMaps.Count == 1
                && printJob.PrinterMaps[0].TicketTag == null
                && printJob.PrinterMaps[0].MenuItem == null
                && printJob.PrinterMaps[0].MenuItemGroupCode == null
                && printJob.PrinterMaps[0].Department == null)
            {
                PrintOrderLines(ticket, ticketItems, printJob.PrinterMaps[0]);
                return;
            }

            var ordersCache = new Dictionary<PrinterMap, IList<TicketItem>>();

            foreach (var item in ticketItems)
            {
                var ps = GetPrinterMapsForItem(printJob.PrinterMaps, ticket, item);
                if (ps != null)
                {
                    foreach (var p in ps)
                    {
                        var lmap = p;
                        var pmap = ordersCache.SingleOrDefault(
                            x => x.Key.Printer == lmap.Printer && x.Key.PrinterTemplate == lmap.PrinterTemplate).Key;
                        if (pmap == null)
                        {
                            ordersCache.Add(p, new List<TicketItem>());
                            pmap = p;
                        }
                       
                        ordersCache[pmap].Add(item);
                    }
                }
            }

            foreach (var order in ordersCache)
            {
                PrintOrderLines(ticket, order.Value, order.Key);
            }
        }

        private static void PrintOrderLines(Ticket ticket, IEnumerable<TicketItem> lines, PrinterMap p)
        {
            if (p == null)
            {
                MessageBox.Show("Yazdırma sırasında bir problem tespit edildi: Yazıcı Haritası null");
                AppServices.Log("Yazıcı Haritası NULL problemi tespit edildi.");
                return;
            }
            if (!string.IsNullOrEmpty(p.PrinterTemplate.LineTemplate) && lines.Count() <= 0) return;
            if (p.Printer == null || string.IsNullOrEmpty(p.Printer.ShareName) || p.PrinterTemplate == null) return;
            var ticketLines = TicketFormatter.GetFormattedTicket(ticket, lines, p.PrinterTemplate);
            PrintJobFactory.CreatePrintJob(p.Printer).DoPrint(ticketLines);
        }

        public static void PrintReport(FlowDocument document)
        {
            var printer = AppServices.CurrentTerminal.ReportPrinter;
            if (printer == null || string.IsNullOrEmpty(printer.ShareName)) return;
            PrintJobFactory.CreatePrintJob(printer).DoPrint(document);
        }

        public static void PrintSlipReport(FlowDocument document)
        {
            var printer = AppServices.CurrentTerminal.SlipReportPrinter;
            if (printer == null || string.IsNullOrEmpty(printer.ShareName)) return;
            PrintJobFactory.CreatePrintJob(printer).DoPrint(document);
        }

        public static void ExecutePrintJob(PrintJob printJob)
        {
            if (printJob.PrinterMaps.Count > 0)
            {
                var printerMap = printJob.PrinterMaps[0];
                var content = printerMap
                    .PrinterTemplate
                    .HeaderTemplate
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (!string.IsNullOrEmpty(printerMap.Printer.ShareName))
                {
                    try
                    {
                        PrintJobFactory.CreatePrintJob(printerMap.Printer).DoPrint(content);
                    }
                    catch (Exception e)
                    {
                        AppServices.LogError(e, string.Format(Resources.PrintingErrorMessage_f, e.Message));
                    }
                }
            }
        }
    }
}
