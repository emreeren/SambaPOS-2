using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Samba.Domain;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Presentation.ViewModels
{
    public class TicketViewModel : ObservableObject
    {
        private readonly Ticket _model;
        private readonly bool _forcePayment;

        public TicketViewModel(Ticket model, bool forcePayment)
        {
            _forcePayment = forcePayment;
            _model = model;
            _items = new ObservableCollection<TicketItemViewModel>(model.TicketItems.Select(x => new TicketItemViewModel(x)).OrderBy(x => x.Model.CreatedDateTime));
            _payments = new ObservableCollection<PaymentViewModel>(model.Payments.Select(x => new PaymentViewModel(x)));
            _discounts = new ObservableCollection<DiscountViewModel>(model.Discounts.Select(x => new DiscountViewModel(x)));

            _itemsViewSource = new CollectionViewSource { Source = _items };
            _itemsViewSource.GroupDescriptions.Add(new PropertyGroupDescription("GroupObject"));

            PrintJobButtons = AppServices.CurrentTerminal.PrintJobs
                .Where(x => (!string.IsNullOrEmpty(x.ButtonText))
                    && (x.PrinterMaps.Count(y => y.Department == null || y.Department.Id == AppServices.MainDataContext.SelectedDepartment.Id) > 0))
                .OrderBy(x => x.Order)
                .Select(x => new PrintJobButton(x, Model));

            if (PrintJobButtons.Count(x => x.Model.UseForPaidTickets) > 0)
            {
                PrintJobButtons = IsPaid
                    ? PrintJobButtons.Where(x => x.Model.UseForPaidTickets)
                    : PrintJobButtons.Where(x => !x.Model.UseForPaidTickets);
            }
        }

        public Ticket Model
        {
            get { return _model; }
        }

        private readonly ObservableCollection<TicketItemViewModel> _items;
        public ObservableCollection<TicketItemViewModel> Items
        {
            get { return _items; }
        }

        private CollectionViewSource _itemsViewSource;
        public CollectionViewSource ItemsViewSource
        {
            get { return _itemsViewSource; }
            set { _itemsViewSource = value; }
        }

        private readonly ObservableCollection<PaymentViewModel> _payments;
        public ObservableCollection<PaymentViewModel> Payments
        {
            get { return _payments; }
        }

        private readonly ObservableCollection<DiscountViewModel> _discounts;
        public ObservableCollection<DiscountViewModel> Discounts
        {
            get { return _discounts; }
        }

        public ObservableCollection<TicketItemViewModel> SelectedItems
        {
            get { return new ObservableCollection<TicketItemViewModel>(Items.Where(x => x.Selected)); }
        }

        public IEnumerable<PrintJobButton> PrintJobButtons { get; set; }

        public DateTime Date
        {
            get { return Model.Date; }
            set { Model.Date = value; }
        }

        public int Id
        {
            get { return Model.Id; }
        }

        public string Note { get { return Model.Note; } set { Model.Note = value; RaisePropertyChanged("Note"); } }
        public string TagDisplay { get { return Model.GetTagData().Split('\r').Select(x => !string.IsNullOrEmpty(x) && x.Contains(":") && x.Split(':')[0].Trim() == x.Split(':')[1].Trim() ? x.Split(':')[0] : x).Aggregate("", (c, v) => c + v + "\r").Trim('\r'); } }

        public bool IsTicketNoteVisible { get { return !string.IsNullOrEmpty(Note); } }

        public bool IsPaid { get { return Model.IsPaid; } }

        public decimal TicketTotalValue
        {
            get { return Model.GetSum(); }
        }

        public decimal TicketVatValue
        {
            get { return Model.CalculateTax(); }
        }

        public decimal TicketTaxServiceValue
        {
            get { return Model.GetTaxServicesTotal(); }
        }

        public decimal TicketPaymentValue
        {
            get { return Model.GetPaymentAmount(); }
        }

        public decimal TicketRemainingValue
        {
            get { return Model.GetRemainingAmount(); }
        }

        public decimal TicketPlainTotalValue
        {
            get { return Model.GetPlainSum(); }
        }

        public string TicketPlainTotalLabel
        {
            get { return TicketPlainTotalValue.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public string TicketTotalLabel
        {
            get { return TicketTotalValue.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public decimal TicketDiscountAmount
        {
            get { return Model.GetDiscountTotal(); }
        }

        public string TicketDiscountLabel
        {
            get { return TicketDiscountAmount.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public decimal TicketRoundingAmount
        {
            get { return Model.GetRoundingTotal(); }
        }

        public string TicketRoundingLabel
        {
            get { return TicketRoundingAmount.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public string TicketVatLabel
        {
            get { return TicketVatValue.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public string TicketTaxServiceLabel
        {
            get { return TicketTaxServiceValue.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public string TicketPaymentLabel
        {
            get { return TicketPaymentValue.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public string TicketRemainingLabel
        {
            get { return TicketRemainingValue.ToString(LocalSettings.DefaultCurrencyFormat); }
        }

        public string TicketCreationDate
        {
            get
            {
                if (IsPaid) return Model.Date.ToString();
                var time = new TimeSpan(DateTime.Now.Ticks - Model.Date.Ticks).TotalMinutes.ToString("#");

                return !string.IsNullOrEmpty(time)
                    ? string.Format(Resources.TicketTimeDisplay_f, Model.Date.ToShortTimeString(), time)
                    : Model.Date.ToShortTimeString();
            }
        }

        public string TicketLastOrderDate
        {
            get
            {
                if (IsPaid) return Model.LastOrderDate.ToString();
                var time = new TimeSpan(DateTime.Now.Ticks - Model.LastOrderDate.Ticks).TotalMinutes.ToString("#");
                return !string.IsNullOrEmpty(time)
                    ? string.Format(Resources.TicketTimeDisplay_f, Model.LastOrderDate.ToShortTimeString(), time)
                    : Model.LastOrderDate.ToShortTimeString();
            }
        }

        public string TicketLastPaymentDate
        {
            get
            {
                if (!IsPaid) return Model.LastPaymentDate != Model.Date ? Model.LastPaymentDate.ToShortTimeString() : "-";
                var time = new TimeSpan(Model.LastPaymentDate.Ticks - Model.Date.Ticks).TotalMinutes.ToString("#");
                return !string.IsNullOrEmpty(time)
                    ? string.Format(Resources.TicketTimeDisplay_f, Model.LastPaymentDate, time)
                    : Model.LastPaymentDate.ToString();
            }
        }

        public bool IsTicketTimeVisible { get { return Model.Id != 0; } }
        public bool IsLastPaymentDateVisible { get { return Model.Payments.Count > 0; } }
        public bool IsLastOrderDateVisible
        {
            get
            {
                return Model.TicketItems.Count > 1 && Model.TicketItems[Model.TicketItems.Count - 1].OrderNumber != 0 &&
                    Model.TicketItems[0].OrderNumber != Model.TicketItems[Model.TicketItems.Count - 1].OrderNumber;
            }
        }

        public void ClearSelectedItems()
        {
            LastSelectedTicketTag = null;

            foreach (var item in Items)
                item.NotSelected();

            RefreshVisuals();

            this.PublishEvent(EventTopicNames.SelectedItemsChanged);
        }

        public TicketItemViewModel AddNewItem(int menuItemId, decimal quantity, bool gift, string defaultProperties, string portionName)
        {
            if (!Model.CanSubmit) return null;
            ClearSelectedItems();
            var menuItem = AppServices.DataAccessService.GetMenuItem(menuItemId);
            if (menuItem.Portions.Count == 0) return null;

            var portion = menuItem.Portions[0];

            if (!string.IsNullOrEmpty(portionName) && menuItem.Portions.Count(x => x.Name == portionName) > 0)
            {
                portion = menuItem.Portions.First(x => x.Name == portionName);
            }

            var departmentId = AppServices.CurrentTerminal.DepartmentId > 0
                                   ? AppServices.MainDataContext.SelectedDepartment.Id
                                   : Model.DepartmentId;

            var ti = Model.AddTicketItem(AppServices.CurrentLoggedInUser.Id, departmentId, menuItem, portion.Name, AppServices.MainDataContext.SelectedDepartment.PriceTag, defaultProperties);

            ti.Quantity = quantity > 9 ? decimal.Round(quantity / portion.Multiplier, LocalSettings.Decimals) : quantity;

            ti.Gifted = gift;
            var ticketItemViewModel = new TicketItemViewModel(ti);
            _items.Add(ticketItemViewModel);
            RecalculateTicket();
            RuleExecutor.NotifyEvent(RuleEventNames.TicketLineAdded, new
            {
                Ticket = AppServices.MainDataContext.SelectedTicket,
                TicketItem = ti,
                TicketId = AppServices.MainDataContext.SelectedTicket.Id,
                TicketTag = AppServices.MainDataContext.SelectedTicket.Tag,
                Quantity = quantity,
                Model.CustomerId,
                Model.CustomerName,
                Model.CustomerGroupCode,
                MenuItemName = menuItem.Name,
                MenuItemGroupCode = menuItem.GroupCode
            });
            ticketItemViewModel.PublishEvent(EventTopicNames.TicketItemAdded);
            return ticketItemViewModel;
        }

        private void RegenerateItemViewModels()
        {
            _items.Clear();
            _items.AddRange(Model.TicketItems.Select(x => new TicketItemViewModel(x)));
            RecalculateTicket();
            ClearSelectedItems();
        }

        public void RefreshVisuals()
        {
            RaisePropertyChanged("TicketTotalLabel");
            RaisePropertyChanged("TicketRemainingLabel");
            RaisePropertyChanged("TicketDiscountLabel");
            RaisePropertyChanged("TicketPlainTotalLabel");
            RaisePropertyChanged("TicketTaxServiceLabel");
            RaisePropertyChanged("TicketRoundingLabel");
            RaisePropertyChanged("TicketVatLabel");

            RaisePropertyChanged("IsTicketRemainingVisible");
            RaisePropertyChanged("IsTicketPaymentVisible");
            RaisePropertyChanged("IsTicketDiscountVisible");
            RaisePropertyChanged("IsTicketTotalVisible");
            RaisePropertyChanged("IsTicketVatTotalVisible");
            RaisePropertyChanged("IsTicketRoundingVisible");
            RaisePropertyChanged("IsTicketTaxServiceVisible");
            RaisePropertyChanged("IsTagged");
        }

        private void VoidItems(IEnumerable<TicketItemViewModel> ticketItems, int reasonId, int userId)
        {
            ticketItems.ToList().ForEach(x => Model.VoidItem(x.Model, reasonId, userId));
            RegenerateItemViewModels();
        }

        private void GiftItems(IEnumerable<TicketItemViewModel> ticketItems, int reasonId, int userId)
        {
            ticketItems.ToList().ForEach(x => Model.GiftItem(x.Model, reasonId, userId));
            RegenerateItemViewModels();
        }

        public void CancelItems(IEnumerable<TicketItemViewModel> ticketItems, int userId)
        {
            VoidItems(ticketItems, 0, userId);
            foreach (var ticketItemViewModel in ticketItems)
            {
                var mi = AppServices.DataAccessService.GetMenuItem(ticketItemViewModel.Model.MenuItemId);
                RuleExecutor.NotifyEvent(RuleEventNames.TicketLineCancelled, new
                {
                    Ticket = AppServices.MainDataContext.SelectedTicket,
                    TicketItem = ticketItemViewModel.Model,
                    TicketTag = AppServices.MainDataContext.SelectedTicket.Tag,
                    ticketItemViewModel.Quantity,
                    Model.CustomerId,
                    Model.CustomerName,
                    Model.CustomerGroupCode,
                    ticketItemViewModel.Model.MenuItemName,
                    MenuItemGroupCode = mi.GroupCode
                });
            }
        }

        public void CancelSelectedItems()
        {
            CancelItems(SelectedItems.ToArray(), AppServices.CurrentLoggedInUser.Id);
        }

        public void GiftSelectedItems(int reasonId)
        {
            FixSelectedItems();
            GiftItems(SelectedItems.ToArray(), reasonId, AppServices.CurrentLoggedInUser.Id);
        }

        public void VoidSelectedItems(int reasonId)
        {
            FixSelectedItems();
            VoidItems(SelectedItems.ToArray(), reasonId, AppServices.CurrentLoggedInUser.Id);
        }

        public bool CanVoidSelectedItems()
        {
            return Model.CanVoidSelectedItems(SelectedItems.Select(x => x.Model));
        }

        public bool CanGiftSelectedItems()
        {
            return Model.CanGiftSelectedItems(SelectedItems.Select(x => x.Model));
        }

        public bool CanCancelSelectedItems()
        {
            return Model.CanCancelSelectedItems(SelectedItems.Select(x => x.Model));
        }

        public bool CanCloseTicket()
        {
            return !_forcePayment || Model.GetRemainingAmount() <= 0 || !string.IsNullOrEmpty(Location) || !string.IsNullOrEmpty(CustomerName) || IsTagged || Items.Count == 0;
        }

        public bool IsTicketTotalVisible
        {
            get { return TicketPaymentValue > 0 && TicketTotalValue > 0; }
        }

        public bool IsTicketPaymentVisible
        {
            get { return TicketPaymentValue > 0; }
        }

        public bool IsTicketRemainingVisible
        {
            get { return TicketRemainingValue > 0; }
        }

        public bool IsTicketVatTotalVisible
        {
            get { return TicketVatValue > 0; }
        }

        public bool IsPlainTotalVisible
        {
            get { return IsTicketDiscountVisible || IsTicketVatTotalVisible || IsTicketRoundingVisible || IsTicketTaxServiceVisible; }
        }

        public bool IsTicketDiscountVisible
        {
            get { return TicketDiscountAmount != 0; }
        }

        public bool IsTicketRoundingVisible
        {
            get { return TicketRoundingAmount != 0; }
        }

        public bool IsTicketTaxServiceVisible
        {
            get { return TicketTaxServiceValue > 0; }
        }

        public string Location
        {
            get { return Model.LocationName; }
            set { Model.LocationName = value; }
        }

        public int CustomerId
        {
            get { return Model.CustomerId; }
            set { Model.CustomerId = value; }
        }

        public string CustomerName
        {
            get { return Model.CustomerName; }
            set { Model.CustomerName = value; }
        }

        public bool IsLocked { get { return Model.Locked; } set { Model.Locked = value; } }
        public bool IsTagged { get { return !string.IsNullOrEmpty(Model.Tag); } }

        public void UpdatePaidItems(IEnumerable<PaidItem> paidItems)
        {
            Model.PaidItems.Clear();
            foreach (var paidItem in paidItems)
            {
                Model.PaidItems.Add(paidItem);
            }
        }

        public void FixSelectedItems()
        {
            var selectedItems = SelectedItems.Where(x => x.SelectedQuantity > 0 && x.SelectedQuantity < x.Quantity).ToList();
            var newItems = Model.ExtractSelectedTicketItems(selectedItems.Select(x => x.Model));
            foreach (var newItem in newItems)
            {
                AppServices.MainDataContext.AddItemToSelectedTicket(newItem);
                _items.Add(new TicketItemViewModel(newItem) { Selected = true });
            }
            selectedItems.ForEach(x => x.Selected = false);
        }

        public string Title
        {
            get
            {
                if (Model == null) return "";

                string selectedTicketTitle;

                if (!string.IsNullOrEmpty(Location) && Model.Id == 0)
                    selectedTicketTitle = string.Format(Resources.Table_f, Location);
                else if (!string.IsNullOrEmpty(CustomerName) && Model.Id == 0)
                    selectedTicketTitle = string.Format(Resources.Account_f, CustomerName);
                else if (string.IsNullOrEmpty(CustomerName)) selectedTicketTitle = string.IsNullOrEmpty(Location)
                     ? string.Format("# {0}", Model.TicketNumber)
                     : string.Format(Resources.TicketNumberAndTable_f, Model.TicketNumber, Location);
                else if (string.IsNullOrEmpty(Location)) selectedTicketTitle = string.IsNullOrEmpty(CustomerName)
                     ? string.Format("# {0}", Model.TicketNumber)
                     : string.Format(Resources.TicketNumberAndAccount_f, Model.TicketNumber, CustomerName);
                else selectedTicketTitle = string.Format(Resources.AccountNameAndTableName_f, Model.TicketNumber, CustomerName, Location);

                return selectedTicketTitle;
            }
        }

        public string CustomPrintData { get { return Model.PrintJobData; } set { Model.PrintJobData = value; } }

        public TicketTagGroup LastSelectedTicketTag { get; set; }

        public void MergeLines()
        {
            Model.MergeLinesAndUpdateOrderNumbers(0);
            _items.Clear();
            _items.AddRange(Model.TicketItems.Select(x => new TicketItemViewModel(x)));
        }

        public bool CanMoveSelectedItems()
        {
            if (IsLocked) return false;
            if (!Model.CanRemoveSelectedItems(SelectedItems.Select(x => x.Model))) return false;
            if (SelectedItems.Where(x => x.Model.Id == 0).Count() > 0) return false;
            if (SelectedItems.Where(x => x.IsLocked).Count() == 0
                && AppServices.IsUserPermittedFor(PermissionNames.MoveUnlockedTicketItems))
                return true;
            return AppServices.IsUserPermittedFor(PermissionNames.MoveTicketItems);
        }

        public bool CanChangeTable()
        {
            if (IsLocked || Items.Count == 0 || (Payments.Count > 0 && !string.IsNullOrEmpty(Location)) || !Model.CanSubmit) return false;
            return string.IsNullOrEmpty(Location) || AppServices.IsUserPermittedFor(PermissionNames.ChangeTable);
        }

        public string GetPrintError()
        {
            if (Items.Count(x => x.TotalPrice == 0 && !x.IsGifted && !x.IsVoided) > 0)
                return Resources.CantCompleteOperationWhenThereIsZeroPricedProduct;
            if (!IsPaid && Items.Count > 0)
            {
                var tg = AppServices.MainDataContext.SelectedDepartment.TicketTagGroups.FirstOrDefault(
                        x => x.ForceValue && !IsTaggedWith(x.Name));
                if (tg != null) return string.Format(Resources.TagCantBeEmpty_f, tg.Name);
            }
            return "";
        }

        public void UpdateTag(TicketTagGroup tagGroup, TicketTag ticketTag)
        {
            Model.SetTagValue(tagGroup.Name, ticketTag.Name);
            if (tagGroup.Numerator != null)
            {
                Model.TicketNumber = "";
                AppServices.MainDataContext.UpdateTicketNumber(Model, tagGroup.Numerator);
            }

            if (ticketTag.AccountId > 0)
                AppServices.MainDataContext.AssignCustomerToTicket(Model,
                    Dao.SingleWithCache<Customer>(x => x.Id == ticketTag.AccountId));

            ClearSelectedItems();

            var tagData = new TicketTagData { Action = tagGroup.Action, TagName = tagGroup.Name, TagValue = ticketTag.Name, NumericValue = tagGroup.NumericTags || tagGroup.PriceTags ? Convert.ToDecimal(ticketTag.Name) : 0 };

            RuleExecutor.NotifyEvent(RuleEventNames.TicketTagSelected,
                        new
                        {
                            Ticket = Model,
                            tagData.TagName,
                            tagData.TagValue,
                            tagData.NumericValue,
                            TicketTag = Model.Tag
                        });

            tagData.PublishEvent(EventTopicNames.TagSelectedForSelectedTicket);
        }

        public bool IsTaggedWith(string tagGroup)
        {
            return !string.IsNullOrEmpty(Model.GetTagValue(tagGroup));
        }

        public void RecalculateTicket()
        {
            RecalculateTicket(Model);
        }

        public static void RecalculateTicket(Ticket ticket)
        {
            var total = ticket.TotalAmount;
            AppServices.MainDataContext.Recalculate(ticket);
            if (total != ticket.TotalAmount)
            {
                RuleExecutor.NotifyEvent(RuleEventNames.TicketTotalChanged,
                    new
                    {
                        Ticket = ticket,
                        PreviousTotal = total,
                        TicketTotal = ticket.GetSum(),
                        DiscountTotal = ticket.GetDiscountAndRoundingTotal(),
                        GiftTotal = ticket.GetTotalGiftAmount(),
                        PaymentTotal = ticket.GetPaymentAmount(),
                        ticket.CustomerName,
                        ticket.CustomerId,
                        ticket.CustomerGroupCode
                    });
            }
        }

        public static void CreateNewTicket()
        {
            AppServices.MainDataContext.CreateNewTicket();
            RuleExecutor.NotifyEvent(RuleEventNames.TicketCreated, new { Ticket = AppServices.MainDataContext.SelectedTicket });
        }

        public static void AddPaymentToSelectedTicket(decimal amount, PaymentType paymentType)
        {
            var ticket = AppServices.MainDataContext.SelectedTicket;

            var departmentId = AppServices.CurrentTerminal.DepartmentId > 0
               ? AppServices.MainDataContext.SelectedDepartment.Id
               : ticket.DepartmentId;
            ticket.AddPayment(DateTime.Now, amount, paymentType, AppServices.CurrentLoggedInUser.Id, departmentId);
            string paymentName = Resources.Cash;
            if (paymentType == PaymentType.CreditCard) paymentName = Resources.CreditCard;
            if (paymentType == PaymentType.Ticket) paymentName = Resources.Voucher;

            RuleExecutor.NotifyEvent(RuleEventNames.PaymentReceived,
                new { Ticket = ticket, PaymentType = paymentName, Amount = amount, TicketTag = ticket.Tag, ticket.CustomerId, ticket.CustomerName, ticket.CustomerGroupCode, SelectedLinesCount = ticket.GetPaidItems().Count() });
        }

        public static void PaySelectedTicket(PaymentType paymentType)
        {
            AddPaymentToSelectedTicket(AppServices.MainDataContext.SelectedTicket.GetRemainingAmount(), paymentType);
        }

        public static void AssignTableToSelectedTicket(int locationId)
        {
            var oldLocation = AppServices.MainDataContext.SelectedTicket != null ? AppServices.MainDataContext.SelectedTicket.LocationName : "";
            AppServices.MainDataContext.AssignTableToSelectedTicket(locationId);
            RuleExecutor.NotifyEvent(RuleEventNames.TicketLocationChanged, new { Ticket = AppServices.MainDataContext.SelectedTicket, OldLocation = oldLocation, NewLocation = AppServices.MainDataContext.SelectedTicket.LocationName });
        }

        public static void RegenerateVatRates(Ticket ticket)
        {
            foreach (var ticketItem in ticket.TicketItems)
            {
                var mi = AppServices.DataAccessService.GetMenuItem(ticketItem.MenuItemId);
                if (mi == null) continue;
                var item = ticketItem;
                var portion = mi.Portions.FirstOrDefault(x => x.Name == item.PortionName);
                if (portion != null) ticketItem.UpdatePortion(portion, ticketItem.PriceTag, mi.VatTemplate);
            }
        }
    }
}
