﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Samba.Domain;
using Samba.Domain.Models.Inventory;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;
using Samba.Localization.Properties;
using Samba.Modules.BasicReports.Reports;
using Samba.Modules.BasicReports.Reports.AccountReport;
using Samba.Modules.BasicReports.Reports.CashReport;
using Samba.Modules.BasicReports.Reports.CSVBuilder;
using Samba.Modules.BasicReports.Reports.EndOfDayReport;
using Samba.Modules.BasicReports.Reports.InventoryReports;
using Samba.Modules.BasicReports.Reports.Payroll;
using Samba.Modules.BasicReports.Reports.ProductReport;
using Samba.Persistance.Data;
using Samba.Services;

namespace Samba.Modules.BasicReports
{
    public static class ReportContext
    {
        public static IList<ReportViewModelBase> Reports { get; private set; }

        private static IWorkspace _workspace;
        public static IWorkspace Workspace { get { return _workspace ?? (_workspace = WorkspaceFactory.Create()); } }

        private static IEnumerable<Ticket> _tickets;
        public static IEnumerable<Ticket> Tickets { get { return _tickets ?? (_tickets = GetTickets(Workspace)); } }

        private static IEnumerable<Department> _departments;
        public static IEnumerable<Department> Departments { get { return _departments ?? (_departments = GetDepartments()); } }

        private static IEnumerable<MenuItem> _menutItems;
        public static IEnumerable<MenuItem> MenuItems { get { return _menutItems ?? (_menutItems = GetMenuItems()); } }

        private static IEnumerable<Transaction> _transactions;
        public static IEnumerable<Transaction> Transactions { get { return _transactions ?? (_transactions = GetTransactions()); } }

        private static IEnumerable<PeriodicConsumption> _periodicConsumptions;
        public static IEnumerable<PeriodicConsumption> PeriodicConsumptions { get { return _periodicConsumptions ?? (_periodicConsumptions = GetPeriodicConsumtions()); } }

        private static IEnumerable<InventoryItem> _inventoryItems;
        public static IEnumerable<InventoryItem> InventoryItems { get { return _inventoryItems ?? (_inventoryItems = GetInventoryItems()); } }

        private static IEnumerable<CashTransactionData> _cashTransactions;
        public static IEnumerable<CashTransactionData> CashTransactions { get { return _cashTransactions ?? (_cashTransactions = GetCashTransactions()); } }

        private static IEnumerable<TicketTagGroup> _ticketTagGroups;
        public static IEnumerable<TicketTagGroup> TicketTagGroups
        {
            get { return _ticketTagGroups ?? (_ticketTagGroups = Dao.Query<TicketTagGroup>()); }
        }

        public static IEnumerable<User> Users { get { return AppServices.MainDataContext.Users; } }

        private static IEnumerable<WorkPeriod> _workPeriods;
        public static IEnumerable<WorkPeriod> WorkPeriods
        {
            get { return _workPeriods ?? (_workPeriods = Dao.Query<WorkPeriod>()); }
        }

        private static IEnumerable<TaxServiceTemplate> _taxServiceTemplates;
        public static IEnumerable<TaxServiceTemplate> TaxServiceTemplates
        {
            get { return _taxServiceTemplates ?? (_taxServiceTemplates = Dao.Query<TaxServiceTemplate>()); }
        }

       
        private static WorkPeriod _currentWorkPeriod;
        public static WorkPeriod CurrentWorkPeriod
        {
            get { return _currentWorkPeriod ?? (_currentWorkPeriod = AppServices.MainDataContext.CurrentWorkPeriod); }
            set
            {
                _currentWorkPeriod = value;
                _tickets = null;
                _cashTransactions = null;
                _periodicConsumptions = null;
                _transactions = null;
                StartDate = CurrentWorkPeriod.StartDate;
                EndDate = CurrentWorkPeriod.EndDate;
                if (StartDate == EndDate) EndDate = DateTime.Now;
            }
        }

        public static DateTime StartDate { get; set; }
        public static DateTime EndDate { get; set; }

        public static string StartDateString { get { return StartDate.ToShortDateString(); } set { StartDate = StrToDate(value); } }
        public static string EndDateString { get { return EndDate.ToShortDateString(); } set { EndDate = StrToDate(value); } }

        private static DateTime StrToDate(string value)
        {
            var vals = value.Split(new[]{' ','/'}).Select(x => Convert.ToInt32(x)).ToList();
            if (vals.Count == 1) vals.Add(DateTime.Now.Month);
            if (vals.Count == 2) vals.Add(DateTime.Now.Year);

            if (vals[2] < 1) { vals[2] = DateTime.Now.Year; }
            if (vals[2] < 1000) { vals[2] += 2000; }

            if (vals[1] < 1) { vals[1] = 1; }
            if (vals[1] > 12) { vals[1] = 12; }

            var dim = DateTime.DaysInMonth(vals[0], vals[1]);
            if (vals[0] < 1) { vals[0] = 1; }
            if (vals[0] > dim) { vals[0] = dim; }
            return new DateTime(vals[2], vals[1], vals[0]);
        }

        private static IEnumerable<InventoryItem> GetInventoryItems()
        {
            return Dao.Query<InventoryItem>();
        }

        private static IEnumerable<Transaction> GetTransactions()
        {
            if (CurrentWorkPeriod.StartDate != CurrentWorkPeriod.EndDate)
                return Dao.Query<Transaction>(x => x.Date >= CurrentWorkPeriod.StartDate && x.Date < CurrentWorkPeriod.EndDate, x => x.TransactionItems, x => x.TransactionItems.Select(y => y.InventoryItem));
            return Dao.Query<Transaction>(x => x.Date >= CurrentWorkPeriod.StartDate, x => x.TransactionItems, x => x.TransactionItems.Select(y => y.InventoryItem));
        }

        private static IEnumerable<PeriodicConsumption> GetPeriodicConsumtions()
        {
            if (CurrentWorkPeriod.StartDate != CurrentWorkPeriod.EndDate)
                return Dao.Query<PeriodicConsumption>(x => x.StartDate >= CurrentWorkPeriod.StartDate && x.EndDate <= CurrentWorkPeriod.EndDate, x => x.CostItems, x => x.CostItems.Select(y => y.Portion), x => x.PeriodicConsumptionItems, x => x.PeriodicConsumptionItems.Select(y => y.InventoryItem));
            return Dao.Query<PeriodicConsumption>(x => x.StartDate >= CurrentWorkPeriod.StartDate, x => x.CostItems, x => x.CostItems.Select(y => y.Portion), x => x.PeriodicConsumptionItems, x => x.PeriodicConsumptionItems.Select(y => y.InventoryItem));
        }

        private static IEnumerable<MenuItem> GetMenuItems()
        {
            return Dao.Query<MenuItem>();
        }

        private static IEnumerable<Department> GetDepartments()
        {
            return Dao.Query<Department>();
        }

        private static IEnumerable<Ticket> GetTickets(IWorkspace workspace)
        {
            try
            {
                if (CurrentWorkPeriod.StartDate == CurrentWorkPeriod.EndDate)
                    return Dao.Query<Ticket>(x => x.LastPaymentDate >= CurrentWorkPeriod.StartDate,
                                             x => x.Payments, x => x.TaxServices,
                                             x => x.Discounts, x => x.TicketItems,
                                             x => x.TicketItems.Select(y => y.Properties));
                return
                    Dao.Query<Ticket>(x =>
                        x.LastPaymentDate >= CurrentWorkPeriod.StartDate && x.LastPaymentDate < CurrentWorkPeriod.EndDate,
                        x => x.Payments, x => x.TaxServices, x => x.Discounts, x => x.TicketItems.Select(y => y.Properties));
            }
            catch (SqlException)
            {
                if (CurrentWorkPeriod.StartDate == CurrentWorkPeriod.EndDate)
                    return workspace.All<Ticket>(x => x.LastPaymentDate >= CurrentWorkPeriod.StartDate);
                return
                    workspace.All<Ticket>(x =>
                        x.LastPaymentDate >= CurrentWorkPeriod.StartDate && x.LastPaymentDate < CurrentWorkPeriod.EndDate);
            }
        }

        private static IEnumerable<CashTransactionData> GetCashTransactions()
        {
            return AppServices.CashService.GetTransactionsWithCustomerData(CurrentWorkPeriod);
        }

        private static IEnumerable<TimeCardEntry> _timeCardEntries;
        public static IEnumerable<TimeCardEntry> TimeCardEntries
        {
            get { return _timeCardEntries ?? (_timeCardEntries = GetTimeCardEntries()); }
            set { _timeCardEntries = value; }
        }

        private static IEnumerable<TimeCardEntry> GetTimeCardEntries()
        {
            if (CurrentWorkPeriod.StartDate != CurrentWorkPeriod.EndDate)
                return Dao.Query<TimeCardEntry>(x => x.DateTime >= CurrentWorkPeriod.StartDate && x.DateTime < CurrentWorkPeriod.EndDate);
            return Dao.Query<TimeCardEntry>(x => x.DateTime >= CurrentWorkPeriod.StartDate);
     
        }


        public static string CurrencyFormat { get { return "#,#0.00;-#,#0.00;-"; } }

        static ReportContext()
        {
            Reports = new List<ReportViewModelBase>
                          {
                              new EndDayReportViewModel(),
                              new ProductReportViewModel(),
                              new CashReportViewModel(),
                              new LiabilityReportViewModel(),
                              new ReceivableReportViewModel(),
                              new InternalAccountsViewModel(),
                              new PurchaseReportViewModel(),
                              new InventoryReportViewModel(),
                              new CostReportViewModel(),
                              new CsvBuilderViewModel(),
                              new PayrollReportViewModel()
                          };
        }

        public static void ResetCache()
        {
            _tickets = null;
            _transactions = null;
            _periodicConsumptions = null;
            _currentWorkPeriod = null;
            _cashTransactions = null;
            _thisMonthWorkPeriod = null;
            _lastMonthWorkPeriod = null;
            _thisWeekWorkPeriod = null;
            _lastWeekWorkPeriod = null;
            _yesterdayWorkPeriod = null;
            _todayWorkPeriod = null;
            _workPeriods = null;
            _taxServiceTemplates = null;
            _timeCardEntries = null;
            _workspace = null;
        }

        private static WorkPeriod _thisMonthWorkPeriod;
        public static WorkPeriod ThisMonthWorkPeriod
        {
            get { return _thisMonthWorkPeriod ?? (_thisMonthWorkPeriod = CreateThisMonthWorkPeriod()); }
        }
        private static WorkPeriod _lastMonthWorkPeriod;
        public static WorkPeriod LastMonthWorkPeriod
        {
            get { return _lastMonthWorkPeriod ?? (_lastMonthWorkPeriod = CreateLastMonthWorkPeriod()); }
        }

        private static WorkPeriod _thisWeekWorkPeriod;
        public static WorkPeriod ThisWeekWorkPeriod
        {
            get { return _thisWeekWorkPeriod ?? (_thisWeekWorkPeriod = CreateThisWeekWorkPeriod()); }
        }

        private static WorkPeriod _lastWeekWorkPeriod;
        public static WorkPeriod LastWeekWorkPeriod
        {
            get { return _lastWeekWorkPeriod ?? (_lastWeekWorkPeriod = CreateLastWeekWorkPeriod()); }
        }

        private static WorkPeriod _yesterdayWorkPeriod;
        public static WorkPeriod YesterdayWorkPeriod
        {
            get { return _yesterdayWorkPeriod ?? (_yesterdayWorkPeriod = CreateYesterdayWorkPeriod()); }
        }

        private static WorkPeriod _todayWorkPeriod;
        public static WorkPeriod TodayWorkPeriod
        {
            get { return _todayWorkPeriod ?? (_todayWorkPeriod = CreteTodayWorkPeriod()); }
        }

        private static WorkPeriod CreteTodayWorkPeriod()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            return CreateCustomWorkPeriod(Resources.Today, start, start.AddDays(1).AddSeconds(-1));
        }

        private static WorkPeriod CreateYesterdayWorkPeriod()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-1);
            var end = start.AddDays(1).AddSeconds(-1);
            return CreateCustomWorkPeriod(Resources.Yesterday, start, end);
        }

        private static WorkPeriod CreateLastMonthWorkPeriod()
        {
            var lastmonth = DateTime.Now.AddMonths(-1);
            var start = new DateTime(lastmonth.Year, lastmonth.Month, 1);
            var end = new DateTime(lastmonth.Year, lastmonth.Month, DateTime.DaysInMonth(lastmonth.Year, lastmonth.Month));
            end = end.AddDays(1).AddSeconds(-1);
            return CreateCustomWorkPeriod(Resources.PastMonth + ": " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(start.Month), start, end);
        }

        private static WorkPeriod CreateThisMonthWorkPeriod()
        {
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            return CreateCustomWorkPeriod(Resources.ThisMonth + ": " + CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(start.Month), start, end);
        }

        private static WorkPeriod CreateThisWeekWorkPeriod()
        {
            var w = (int)DateTime.Now.DayOfWeek;
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-w + 1);
            var end = DateTime.Now.Date.AddDays(1).AddSeconds(-1);
            return CreateCustomWorkPeriod(Resources.ThisWeek, start, end);
        }

        private static WorkPeriod CreateLastWeekWorkPeriod()
        {
            var w = (int)DateTime.Now.DayOfWeek;
            var start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(-6 - w);
            var end = start.AddDays(7).AddSeconds(-1);
            return CreateCustomWorkPeriod(Resources.PastWeek, start, end);
        }

        public static IEnumerable<WorkPeriod> GetWorkPeriods(DateTime startDate, DateTime endDate)
        {
            var wp = WorkPeriods.Where(x => x.EndDate >= endDate && x.StartDate < startDate);
            if (wp.Count() == 0)
                wp = WorkPeriods.Where(x => x.StartDate >= startDate && x.StartDate < endDate);
            if (wp.Count() == 0)
                wp = WorkPeriods.Where(x => x.EndDate >= startDate && x.EndDate < endDate);
            if (wp.Count() == 0 && AppServices.MainDataContext.CurrentWorkPeriod.StartDate < startDate)
                wp = new List<WorkPeriod> { AppServices.MainDataContext.CurrentWorkPeriod };
            return wp.OrderBy(x => x.StartDate);
        }

        public static WorkPeriod CreateCustomWorkPeriod(string name, DateTime startDate, DateTime endDate)
        {
            DateTime start, end;
            if (startDate == null || endDate == null)
            {
                var periods = GetWorkPeriods(startDate, endDate);
                var startPeriod = periods.FirstOrDefault();
                var endPeriod = periods.LastOrDefault();
                start = startPeriod != null ? startPeriod.StartDate : startDate;
                end = endPeriod != null ? endPeriod.EndDate : endDate;
                if (endPeriod != null && end == endPeriod.StartDate)
                    end = DateTime.Now;
            }
            else
            {
                start = startDate;
                end = endDate;
            }
           
            var result = new WorkPeriod { Name = name, StartDate = start, EndDate = end };
            return result;
        }

        public static string GetUserName(int userId)
        {
            var user = Users.SingleOrDefault(x => x.Id == userId);
            return user != null ? user.Name : Resources.UndefinedWithBrackets;
        }

        public static string GetReasonName(int reasonId)
        {
            if (AppServices.MainDataContext.Reasons.ContainsKey(reasonId))
                return AppServices.MainDataContext.Reasons[reasonId].Name;
            return Resources.UndefinedWithBrackets;
        }

        internal static string GetDepartmentName(int departmentId)
        {
            var d = AppServices.MainDataContext.Departments.SingleOrDefault(x => x.Id == departmentId);
            return d != null ? d.Name : Resources.UndefinedWithBrackets;
        }

        internal static AmountCalculator GetOperationalAmountCalculator()
        {
            var groups = Tickets
                .SelectMany(x => x.Payments)
                .GroupBy(x => new { x.PaymentType })
                .Select(x => new TenderedAmount { PaymentType = x.Key.PaymentType, Amount = x.Sum(y => y.Amount) });
            return new AmountCalculator(groups);
        }

        internal static decimal GetCashTotalAmount()
        {
            return
                CashTransactions.Where(x => x.PaymentType == (int)PaymentType.Cash && x.TransactionType == (int)TransactionType.Income).Sum(x => x.Amount) -
                CashTransactions.Where(x => x.PaymentType == (int)PaymentType.Cash && x.TransactionType == (int)TransactionType.Expense).Sum(x => x.Amount);
        }

        internal static decimal GetCreditCardTotalAmount()
        {
            return
                CashTransactions.Where(x => x.PaymentType == (int)PaymentType.CreditCard && x.TransactionType == (int)TransactionType.Income).Sum(x => x.Amount) -
                CashTransactions.Where(x => x.PaymentType == (int)PaymentType.CreditCard && x.TransactionType == (int)TransactionType.Expense).Sum(x => x.Amount);
        }

        internal static decimal GetTicketTotalAmount()
        {
            return
                CashTransactions.Where(x => x.PaymentType == (int)PaymentType.Ticket && x.TransactionType == (int)TransactionType.Income).Sum(x => x.Amount) -
                CashTransactions.Where(x => x.PaymentType == (int)PaymentType.Ticket && x.TransactionType == (int)TransactionType.Expense).Sum(x => x.Amount);
        }

        public static PeriodicConsumption GetCurrentPeriodicConsumption()
        {
            var workspace = WorkspaceFactory.Create();
            return InventoryService.GetCurrentPeriodicConsumption(workspace);
        }
    }
}
