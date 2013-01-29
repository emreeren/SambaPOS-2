using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Samba.Domain.Models.Actions;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tables;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Users;
using Samba.Infrastructure.Data;
using Samba.Infrastructure.Data.Serializer;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Persistance.Data;

namespace Samba.Services
{
    public class TicketCommitResult
    {
        public int TicketId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class MainDataContext
    {
        private class TicketWorkspace
        {
            private IWorkspace _workspace;
            public Ticket Ticket { get; private set; }

            public void CreateTicket(Department department)
            {
                Debug.Assert(_workspace == null);
                Debug.Assert(Ticket == null);
                Debug.Assert(department != null);

                _workspace = WorkspaceFactory.Create();
                Ticket = Ticket.Create(department);
            }

            public void OpenTicket(int ticketId)
            {
                Debug.Assert(_workspace == null);
                Debug.Assert(Ticket == null);
                _workspace = WorkspaceFactory.Create();
                if (LocalSettings.DatabaseLabel == "CE")
                    Ticket = _workspace.Single<Ticket>(ticket => ticket.Id == ticketId);
                else
                {
                    Ticket = _workspace.Single<Ticket>(ticket => ticket.Id == ticketId,
                        x => x.TicketItems.Select(y => y.Properties),
                        x => x.Payments, x => x.Discounts, x => x.TaxServices);
                }
            }

            public void CommitChanges()
            {
                Debug.Assert(_workspace != null);
                Debug.Assert(Ticket != null);
                Debug.Assert(Ticket.Id > 0 || Ticket.TicketItems.Count > 0);
                if (Ticket.Id == 0 && Ticket.TicketNumber != null)
                    _workspace.Add(Ticket);
                Ticket.LastUpdateTime = DateTime.Now;
                _workspace.CommitChanges();
            }

            public void Reset()
            {
                Debug.Assert(Ticket != null);
                Debug.Assert(_workspace != null);
                Ticket = null;
                _workspace = null;
            }

            public Table LoadTable(string locationName)
            {
                return _workspace.Single<Table>(x => x.Name == locationName);
            }

            public Customer UpdateCustomer(Customer customer)
            {
                if (customer == Customer.Null)
                    return Customer.Null;

                if (customer.Id == 0)
                {
                    using (var workspace = WorkspaceFactory.Create())
                    {
                        workspace.Add(customer);
                        workspace.CommitChanges();
                    }
                    return customer;
                }

                var result = _workspace.Single<Customer>(
                        x => x.Id == customer.Id
                        && x.Name == customer.Name
                        && x.Address == customer.Address
                        && x.PhoneNumber == customer.PhoneNumber
                        && x.Note == customer.Note);

                if (result == null)
                {
                    result = _workspace.Single<Customer>(x => x.Id == customer.Id);
                    Debug.Assert(result != null);
                    result.Address = customer.Address;
                    result.Name = customer.Name;
                    result.PhoneNumber = customer.PhoneNumber;
                    result.Note = customer.Note;
                }
                return result;
            }

            public Table GetTableWithId(int tableId)
            {
                return _workspace.Single<Table>(x => x.Id == tableId);
            }

            public Table GetTicketTable()
            {
                Debug.Assert(!string.IsNullOrEmpty(Ticket.LocationName));
                Debug.Assert(Ticket != null);
                return _workspace.Single<Table>(x => x.Name == Ticket.LocationName);
            }

            public void ResetTableData(Ticket ticket)
            {
                var tables = _workspace.All<Table>(x => x.TicketId == ticket.Id);
                foreach (var table in tables)
                {
                    table.TicketId = 0;
                    table.IsTicketLocked = false;
                }
            }

            public void RemoveTicketItems(IEnumerable<TicketItem> selectedItems)
            {
                foreach (var ticketItem in selectedItems)
                {
                    Ticket.TicketItems.Remove(ticketItem);
                    if (Ticket.Id > 0)
                    {
                        ticketItem.Properties.ToList().ForEach(_workspace.Delete);
                        _workspace.Delete(ticketItem);
                    }
                }
            }

            public void RemoveTaxServices(IEnumerable<TaxService> taxServices)
            {
                foreach (var taxService in taxServices)
                {
                    _workspace.Delete(taxService);
                }
            }

            public void AddItemToSelectedTicket(TicketItem model)
            {
                _workspace.Add(model);
            }

        }

        public int CustomerCount { get; set; }
        public int TableCount { get; set; }
        public string NumeratorValue { get; set; }

        private IWorkspace _tableWorkspace;
        private readonly TicketWorkspace _ticketWorkspace = new TicketWorkspace();

        private IEnumerable<AppRule> _rules;
        public IEnumerable<AppRule> Rules { get { return _rules ?? (_rules = Dao.Query<AppRule>(x => x.Actions)); } }

        private IEnumerable<AppAction> _actions;
        public IEnumerable<AppAction> Actions { get { return _actions ?? (_actions = Dao.Query<AppAction>()); } }

        private IEnumerable<TableScreen> _tableScreens;
        public IEnumerable<TableScreen> TableScreens { get { return _tableScreens ?? (_tableScreens = Dao.Query<TableScreen>(x => x.Tables)); } }

        private IEnumerable<Department> _departments;
        public IEnumerable<Department> Departments
        {
            get
            {
                return _departments ?? (_departments = Dao.Query<Department>(x => x.TicketNumerator, x => x.OrderNumerator,
                    x => x.TaxServiceTemplates, x => x.TicketTagGroups.Select(y => y.Numerator), x => x.TicketTagGroups.Select(y => y.TicketTags)));
            }
        }

        private IEnumerable<Department> _permittedDepartments;
        public IEnumerable<Department> PermittedDepartments
        {
            get
            {
                return _permittedDepartments ?? (
                    _permittedDepartments = Departments.Where(
                      x => AppServices.IsUserPermittedFor(PermissionNames.UseDepartment + x.Id)));
            }
        }

        private IDictionary<int, Reason> _reasons;
        public IDictionary<int, Reason> Reasons { get { return _reasons ?? (_reasons = Dao.BuildDictionary<Reason>()); } }

        private IEnumerable<WorkPeriod> _lastTwoWorkPeriods;
        public IEnumerable<WorkPeriod> LastTwoWorkPeriods
        {
            get { return _lastTwoWorkPeriods ?? (_lastTwoWorkPeriods = GetLastTwoWorkPeriods()); }
        }

        private IEnumerable<User> _users;
        public IEnumerable<User> Users { get { return _users ?? (_users = Dao.Query<User>(x => x.UserRole)); } }

        private IEnumerable<VatTemplate> _vatTemplates;
        public IEnumerable<VatTemplate> VatTemplates
        {
            get { return _vatTemplates ?? (_vatTemplates = Dao.Query<VatTemplate>()); }
        }

        private IEnumerable<TaxServiceTemplate> _taxServiceTemplates;
        public IEnumerable<TaxServiceTemplate> TaxServiceTemplates
        {
            get { return _taxServiceTemplates ?? (_taxServiceTemplates = Dao.Query<TaxServiceTemplate>()); }
        }

        public WorkPeriod CurrentWorkPeriod { get { return LastTwoWorkPeriods.LastOrDefault(); } }
        public WorkPeriod PreviousWorkPeriod { get { return LastTwoWorkPeriods.Count() > 1 ? LastTwoWorkPeriods.FirstOrDefault() : null; } }

        public TableScreen SelectedTableScreen { get; set; }
        public Ticket SelectedTicket { get { return _ticketWorkspace.Ticket; } }

        private Department _selectedDepartment;
        public Department SelectedDepartment
        {
            get { return _selectedDepartment; }
            set
            {
                if (value != null && (_selectedDepartment == null || _selectedDepartment.Id != value.Id))
                {
                    SelectedTableScreen = TableScreens.FirstOrDefault(x => x.Id == value.TableScreenId);
                }
                _selectedDepartment = value;
            }
        }

        public bool IsCurrentWorkPeriodOpen
        {
            get
            {
                return CurrentWorkPeriod != null &&
                 CurrentWorkPeriod.StartDate == CurrentWorkPeriod.EndDate;
            }
        }

        public MainDataContext()
        {
            _ticketWorkspace = new TicketWorkspace();
        }

        private static IEnumerable<WorkPeriod> GetLastTwoWorkPeriods()
        {
            return Dao.Last<WorkPeriod>(2);
        }

        public void ResetUserData()
        {
            _permittedDepartments = null;
            ThreadPool.QueueUserWorkItem(ResetTableCustomerCounts);
        }

        private void ResetTableCustomerCounts(object state)
        {
            CustomerCount = Dao.Count<Customer>(null);
            TableCount = Dao.Count<Table>(null);
        }

        public void StartWorkPeriod(string description, decimal cashAmount, decimal creditCardAmount, decimal ticketAmount)
        {
            using (var workspace = WorkspaceFactory.Create())
            {
                _lastTwoWorkPeriods = null;

                var latestWorkPeriod = workspace.Last<WorkPeriod>();
                if (latestWorkPeriod != null && latestWorkPeriod.StartDate == latestWorkPeriod.EndDate)
                {
                    return;
                }

                var now = DateTime.Now;
                var newPeriod = new WorkPeriod
                                    {
                                        StartDate = now,
                                        EndDate = now,
                                        StartDescription = description,
                                        CashAmount = cashAmount,
                                        CreditCardAmount = creditCardAmount,
                                        TicketAmount = ticketAmount
                                    };

                workspace.Add(newPeriod);
                workspace.CommitChanges();
                _lastTwoWorkPeriods = null;
            }
        }

        public void StopWorkPeriod(string description)
        {
            using (var workspace = WorkspaceFactory.Create())
            {
                var period = workspace.Last<WorkPeriod>();
                if (period.EndDate == period.StartDate)
                {
                    period.EndDate = DateTime.Now;
                    period.EndDescription = description;
                    workspace.CommitChanges();
                }
                _lastTwoWorkPeriods = null;
            }
        }

        public string GetReason(int reasonId)
        {
            return Reasons.ContainsKey(reasonId) ? Reasons[reasonId].Name : Resources.UndefinedWithBrackets;
        }

        public void UpdateTicketTable(Ticket ticket)
        {
            if (string.IsNullOrEmpty(ticket.LocationName)) return;
            var table = _ticketWorkspace.LoadTable(ticket.LocationName);
            if (table != null)
            {
                if (ticket.IsPaid || ticket.TicketItems.Count == 0)
                {
                    if (table.TicketId == ticket.Id)
                    {
                        table.TicketId = 0;
                        table.IsTicketLocked = false;
                    }
                }
                else
                {
                    table.TicketId = ticket.Id;
                    table.IsTicketLocked = ticket.Locked;
                }
            }
            else ticket.LocationName = "";
        }

        public void UpdateTableData(TableScreen selectedTableScreen, int pageNo)
        {
            var set = selectedTableScreen.Tables.Select(x => x.Id);
            if (selectedTableScreen.PageCount > 1)
            {
                set = selectedTableScreen.Tables
                    .OrderBy(x => x.Order)
                    .Skip(pageNo * selectedTableScreen.ItemCountPerPage)
                    .Take(selectedTableScreen.ItemCountPerPage)
                    .Select(x => x.Id);
            }

            var result = Dao.Select<Table, dynamic>(x => new { x.Id, Tid = x.TicketId, Locked = x.IsTicketLocked },
                                                   x => set.Contains(x.Id));
            foreach (var td in result)
            {
                var tid = td.Id;
                var table = selectedTableScreen.Tables.Single(x => x.Id == tid);
                table.TicketId = td.Tid;
                table.IsTicketLocked = td.Locked;
            }
        }

        public void AssignCustomerToTicket(Ticket ticket, Customer customer)
        {
            Debug.Assert(ticket != null);
            ticket.UpdateCustomer(_ticketWorkspace.UpdateCustomer(customer));
        }

        public void AssignCustomerToSelectedTicket(Customer customer)
        {
            if (SelectedTicket == null)
            {
                CreateNewTicket();
            }
            AssignCustomerToTicket(SelectedTicket, customer);
        }

        public void AssignTableToSelectedTicket(int tableId)
        {
            if (SelectedTicket == null)
            {
                CreateNewTicket();
            }

            var table = _ticketWorkspace.GetTableWithId(tableId);

            Debug.Assert(SelectedTicket != null);

            if (!string.IsNullOrEmpty(SelectedTicket.LocationName))
            {
                var oldTable = _ticketWorkspace.GetTicketTable();
                if (oldTable.TicketId == SelectedTicket.Id)
                {
                    oldTable.IsTicketLocked = false;
                    oldTable.TicketId = 0;
                }
            }

            if (table.TicketId > 0 && table.TicketId != SelectedTicket.Id)
            {
                MoveTicketItems(SelectedTicket.TicketItems.ToList(), table.TicketId);
                OpenTicket(table.TicketId);
            }

            SelectedTicket.DepartmentId = AppServices.CurrentTerminal.DepartmentId > 0
                ? AppServices.CurrentTerminal.DepartmentId
                : SelectedDepartment.Id;

            SelectedTicket.LocationName = table.Name;
            table.TicketId = SelectedTicket.GetRemainingAmount() > 0 ? SelectedTicket.Id : 0;
        }

        public void UpdateTables(int tableScreenId, int pageNo)
        {
            SelectedTableScreen = null;
            if (tableScreenId > 0)
            {
                SelectedTableScreen = TableScreens.Single(x => x.Id == tableScreenId);
                AppServices.MainDataContext.UpdateTableData(SelectedTableScreen, pageNo);
            }
        }

        public void OpenTicket(int ticketId)
        {
            _ticketWorkspace.OpenTicket(ticketId);
        }

        public TicketCommitResult CloseTicket()
        {
            var result = new TicketCommitResult();
            Debug.Assert(SelectedTicket != null);
            var changed = false;
            if (SelectedTicket.Id > 0)
            {
                var lup = Dao.Single<Ticket, DateTime>(SelectedTicket.Id, x => x.LastUpdateTime);
                if (SelectedTicket.LastUpdateTime.CompareTo(lup) != 0)
                {
                    var currentTicket = Dao.Single<Ticket>(x => x.Id == SelectedTicket.Id, x => x.TicketItems, x => x.Payments);
                    if (currentTicket.LocationName != SelectedTicket.LocationName)
                    {
                        result.ErrorMessage = string.Format(Resources.TicketMovedRetryLastOperation_f, currentTicket.LocationName);
                        changed = true;
                    }

                    if (currentTicket.IsPaid != SelectedTicket.IsPaid)
                    {
                        if (currentTicket.IsPaid)
                        {
                            result.ErrorMessage = Resources.TicketPaidChangesNotSaved;
                        }
                        if (SelectedTicket.IsPaid)
                        {
                            result.ErrorMessage = Resources.TicketChangedRetryLastOperation;
                        }
                        changed = true;
                    }
                    else if (SelectedTicket.RemainingAmount == 0 && currentTicket.TotalAmount != SelectedTicket.TotalAmount)
                    {
                        result.ErrorMessage = Resources.TicketChangedRetryLastOperation;
                        changed = true;
                    }
                    else if (currentTicket.LastPaymentDate != SelectedTicket.LastPaymentDate)
                    {
                        var currentPaymentIds = SelectedTicket.Payments.Select(x => x.Id).Distinct();
                        var unknownPayments = currentTicket.Payments.Where(x => !currentPaymentIds.Contains(x.Id)).FirstOrDefault();
                        if (unknownPayments != null)
                        {
                            result.ErrorMessage = Resources.TicketPaidLastChangesNotSaved;
                            changed = true;
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(SelectedTicket.LocationName) && SelectedTicket.Id == 0)
            {
                var ticketId = Dao.Select<Table, int>(x => x.TicketId, x => x.Name == SelectedTicket.LocationName).FirstOrDefault();
                {
                    if (ticketId > 0)
                    {
                        result.ErrorMessage = string.Format(Resources.TableChangedRetryLastOperation_f, SelectedTicket.LocationName);
                        changed = true;
                    }
                }
            }

            var canSumbitTicket = !changed && SelectedTicket.CanSubmit; // Fişi kaydedebilmek için gün sonu yapılmamış ve fişin ödenmemiş olması gerekir.

            if (canSumbitTicket)
            {
                _ticketWorkspace.RemoveTicketItems(SelectedTicket.PopRemovedTicketItems());
                _ticketWorkspace.RemoveTaxServices(SelectedTicket.PopRemovedTaxServices());
                Recalculate(SelectedTicket);

                if (!SelectedTicket.IsPaid && SelectedTicket.RemainingAmount == 0 && AppServices.CurrentTerminal.DepartmentId > 0)
                    SelectedTicket.DepartmentId = AppServices.CurrentTerminal.DepartmentId;

                SelectedTicket.IsPaid = SelectedTicket.RemainingAmount == 0;


                if (SelectedTicket.TicketItems.Count > 0)
                {
                    if (SelectedTicket.TicketItems.Where(x => !x.Locked).FirstOrDefault() != null)
                    {
                        SelectedTicket.MergeLinesAndUpdateOrderNumbers(NumberGenerator.GetNextNumber(SelectedDepartment.OrderNumerator.Id));
                    }

                    if (SelectedTicket.Id == 0)
                    {
                        UpdateTicketNumber(SelectedTicket);
                        SelectedTicket.LastOrderDate = DateTime.Now;
                        _ticketWorkspace.CommitChanges();
                    }

                    Debug.Assert(!string.IsNullOrEmpty(SelectedTicket.TicketNumber));
                    Debug.Assert(SelectedTicket.Id > 0);

                    //Otomatik yazdırma
                    AppServices.PrintService.AutoPrintTicket(SelectedTicket);
                    SelectedTicket.LockTicket();
                }

                UpdateTicketTable(SelectedTicket);

                if (SelectedTicket.Id > 0)  // eğer adisyonda satır yoksa ID burada 0 olmalı.
                    _ticketWorkspace.CommitChanges();

                Debug.Assert(SelectedTicket.TicketItems.Count(x => x.OrderNumber == 0) == 0);
            }
            result.TicketId = SelectedTicket.Id;
            _ticketWorkspace.Reset();

            return result;
        }

        public void UpdateTicketNumber(Ticket ticket)
        {
            UpdateTicketNumber(ticket, SelectedDepartment.TicketNumerator);
        }

        public void UpdateTicketNumber(Ticket ticket, Numerator numerator)
        {
            if (numerator == null) numerator = SelectedDepartment.TicketNumerator;
            if (string.IsNullOrEmpty(ticket.TicketNumber))
                ticket.TicketNumber = NumberGenerator.GetNextString(numerator.Id);
        }

        public IList<Table> LoadTables(string selectedTableScreen)
        {
            if (_tableWorkspace != null)
            {
                _tableWorkspace.CommitChanges();
            }
            _tableWorkspace = WorkspaceFactory.Create();
            return _tableWorkspace.Single<TableScreen>(x => x.Name == selectedTableScreen).Tables;
        }

        public void SaveTables()
        {
            if (_tableWorkspace != null)
            {
                _tableWorkspace.CommitChanges();
                _tableWorkspace = null;
                _tableScreens = null;
            }
        }

        public void ResetCache()
        {
            Debug.Assert(_ticketWorkspace.Ticket == null);

            if (_tableWorkspace == null)
            {
                var selectedDepartment = SelectedDepartment != null ? SelectedDepartment.Id : 0;
                var selectedTableScreen = SelectedTableScreen != null ? SelectedTableScreen.Id : 0;

                SelectedTableScreen = null;
                SelectedDepartment = null;

                _tableScreens = null;
                _departments = null;
                _permittedDepartments = null;
                _reasons = null;
                _lastTwoWorkPeriods = null;
                _users = null;
                _rules = null;
                _actions = null;
                _vatTemplates = null;
                _taxServiceTemplates = null;

                if (selectedTableScreen > 0 && TableScreens.Count(x => x.Id == selectedTableScreen) > 0)
                    SelectedTableScreen = TableScreens.Single(x => x.Id == selectedTableScreen);
                if (selectedDepartment > 0 && Departments.Count(x => x.Id == selectedDepartment) > 0)
                    SelectedDepartment = Departments.Single(x => x.Id == selectedDepartment);
            }
        }

        public void OpenTicketFromTableName(string tableName)
        {
            var table = Dao.SingleWithCache<Table>(x => x.Name == tableName);
            if (table != null)
            {
                if (table.TicketId > 0)
                    OpenTicket(table.TicketId);
                AssignTableToSelectedTicket(table.Id);
            }
        }

        public void OpenTicketFromTicketNumber(string ticketNumber)
        {
            Debug.Assert(_ticketWorkspace.Ticket == null);
            var id = Dao.Select<Ticket, int>(x => x.Id, x => x.TicketNumber == ticketNumber).FirstOrDefault();
            if (id > 0) OpenTicket(id);
        }

        public string GetUserName(int userId)
        {
            return userId > 0 ? Users.Single(x => x.Id == userId).Name : "-";
        }

        public void CreateNewTicket()
        {
            var department = SelectedDepartment;
            if (AppServices.CurrentTerminal.DepartmentId > 0 && AppServices.CurrentTerminal.DepartmentId != department.Id)
                department = Departments.Single(x => x.Id == AppServices.CurrentTerminal.DepartmentId);
            _ticketWorkspace.CreateTicket(department);
        }

        public TicketCommitResult MoveTicketItems(IEnumerable<TicketItem> selectedItems, int targetTicketId)
        {
            var clonedItems = selectedItems.Select(ObjectCloner.Clone).ToList();

            _ticketWorkspace.RemoveTicketItems(selectedItems);

            if (SelectedTicket.TicketItems.Count == 0)
            {
                var info = targetTicketId.ToString();
                if (targetTicketId > 0)
                {
                    var tData = Dao.Single<Ticket, dynamic>(targetTicketId, x => new { x.LocationName, x.TicketNumber });
                    info = tData.LocationName + " - " + tData.TicketNumber;
                }
                if (!string.IsNullOrEmpty(SelectedTicket.Note)) SelectedTicket.Note += "\r";
                SelectedTicket.Note += SelectedTicket.LocationName + " => " + info;
            }

            CloseTicket();

            if (targetTicketId == 0)
                CreateNewTicket();
            else OpenTicket(targetTicketId);

            foreach (var ticketItem in clonedItems)
            {
                SelectedTicket.TicketItems.Add(ticketItem);
            }

            SelectedTicket.LastOrderDate = DateTime.Now;
            return CloseTicket();
        }

        public void ResetTableDataForSelectedTicket()
        {
            _ticketWorkspace.ResetTableData(SelectedTicket);
            AppServices.MainDataContext.UpdateTicketTable(SelectedTicket);
            _ticketWorkspace.CommitChanges();
        }

        public void AddItemToSelectedTicket(TicketItem model)
        {
            _ticketWorkspace.AddItemToSelectedTicket(model);
        }

        public void Recalculate(Ticket ticket)
        {
            ticket.Recalculate(AppServices.SettingService.AutoRoundDiscount, AppServices.CurrentLoggedInUser.Id);
        }

        public VatTemplate GetVatTemplate(int menuItemId)
        {
            return AppServices.DataAccessService.GetMenuItem(menuItemId).VatTemplate;
        }
    }
}
