﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Presentation.ViewModels;
using Samba.Services;

namespace Samba.Modules.TicketModule
{
    public class PaymentEditorViewModel : ObservableObject
    {
        private bool _resetAmount;
        private readonly ICaptionCommand _manualPrintCommand;

        public PaymentEditorViewModel()
        {
            _manualPrintCommand = new CaptionCommand<PrintJob>(Resources.Print, OnManualPrint, CanManualPrint);
            SubmitCashPaymentCommand = new CaptionCommand<string>(Resources.Cash, OnSubmitCashPayment, CanSubmitCashPayment);
            SubmitCreditCardPaymentCommand = new CaptionCommand<string>(Resources.CreditCard_r, OnSubmitCreditCardPayment,
                                                                        CanSubmitCashPayment);
            SubmitTicketPaymentCommand = new CaptionCommand<string>(Resources.Voucher_r, OnSubmitTicketPayment, CanSubmitCashPayment);
            SubmitAccountPaymentCommand = new CaptionCommand<string>(Resources.AccountBalance_r, OnSubmitAccountPayment, CanSubmitAccountPayment);
            ClosePaymentScreenCommand = new CaptionCommand<string>(Resources.Close, OnClosePaymentScreen, CanClosePaymentScreen);
            TenderAllCommand = new CaptionCommand<string>(Resources.All, OnTenderAllCommand);
            TypeValueCommand = new DelegateCommand<string>(OnTypeValueExecuted);
            SetValueCommand = new DelegateCommand<string>(OnSetValue);
            DivideValueCommand = new DelegateCommand<string>(OnDivideValue);
            SelectMergedItemCommand = new DelegateCommand<MergedItem>(OnMergedItemSelected);

            SetDiscountAmountCommand = new CaptionCommand<string>(Resources.Round, OnSetDiscountAmountCommand, CanSetDiscount);
            AutoSetDiscountAmountCommand = new CaptionCommand<string>(Resources.Flat, OnAutoSetDiscount, CanAutoSetDiscount);
            SetDiscountRateCommand = new CaptionCommand<string>(Resources.DiscountPercentSign, OnSetDiscountRateCommand, CanSetDiscountRate);

            MergedItems = new ObservableCollection<MergedItem>();
            ReturningAmountVisibility = Visibility.Collapsed;

            LastTenderedAmount = "1";
            EventServiceFactory.EventService.GetEvent<GenericEvent<CreditCardProcessingResult>>().Subscribe(OnProcessCreditCard);
        }

        public CaptionCommand<string> SubmitCashPaymentCommand { get; set; }
        public CaptionCommand<string> SubmitCreditCardPaymentCommand { get; set; }
        public CaptionCommand<string> SubmitTicketPaymentCommand { get; set; }
        public CaptionCommand<string> SubmitAccountPaymentCommand { get; set; }
        public CaptionCommand<string> ClosePaymentScreenCommand { get; set; }
        public CaptionCommand<string> TenderAllCommand { get; set; }
        public DelegateCommand<string> TypeValueCommand { get; set; }
        public DelegateCommand<string> SetValueCommand { get; set; }
        public DelegateCommand<string> DivideValueCommand { get; set; }
        public DelegateCommand<MergedItem> SelectMergedItemCommand { get; set; }
        public CaptionCommand<string> SetDiscountRateCommand { get; set; }
        public CaptionCommand<string> SetDiscountAmountCommand { get; set; }
        public CaptionCommand<string> AutoSetDiscountAmountCommand { get; set; }

        public ObservableCollection<MergedItem> MergedItems { get; set; }

        public string SelectedTicketTitle { get { return SelectedTicket != null ? SelectedTicket.Title : ""; } }

        private string _paymentAmount;
        public string PaymentAmount
        {
            get { return _paymentAmount; }
            set
            {
                _paymentAmount = value;
                RaisePropertyChanged("PaymentAmount");
            }
        }

        private string _tenderedAmount;
        public string TenderedAmount
        {
            get { return _tenderedAmount; }
            set
            {
                _tenderedAmount = value;
                RaisePropertyChanged("TenderedAmount");
            }
        }

        private string _lastTenderedAmount;
        public string LastTenderedAmount
        {
            get { return _lastTenderedAmount; }
            set { _lastTenderedAmount = value; RaisePropertyChanged("LastTenderedAmount"); }
        }

        public string ReturningAmount { get; set; }
        private Visibility _returningAmountVisibility;
        public Visibility ReturningAmountVisibility { get { return _returningAmountVisibility; } set { _returningAmountVisibility = value; RaisePropertyChanged("ReturningAmountVisibility"); } }

        public Visibility PaymentsVisibility
        {
            get
            {
                return AppServices.MainDataContext.SelectedTicket != null && AppServices.MainDataContext.SelectedTicket.Payments.Count() > 0
                           ? Visibility.Visible
                           : Visibility.Collapsed;
            }
        }

        public IEnumerable<CommandButtonViewModel> CommandButtons { get; set; }

        private IEnumerable<CommandButtonViewModel> CreateCommandButtons()
        {
            var result = new List<CommandButtonViewModel>();

            result.Add(new CommandButtonViewModel
                           {
                               Caption = SetDiscountRateCommand.Caption,
                               Command = SetDiscountRateCommand
                           });

            result.Add(new CommandButtonViewModel
                           {
                               Caption = SetDiscountAmountCommand.Caption,
                               Command = SetDiscountAmountCommand
                           });

            result.Add(new CommandButtonViewModel
                           {
                               Caption = AutoSetDiscountAmountCommand.Caption,
                               Command = AutoSetDiscountAmountCommand
                           });

            if (SelectedTicket != null)
            {
                result.AddRange(SelectedTicket.PrintJobButtons.Where(x => x.Model.UseFromPaymentScreen)
                    .Select(x => new CommandButtonViewModel
                            {
                                Caption = x.Caption,
                                Command = _manualPrintCommand,
                                Parameter = x.Model
                            }));
            }
            return result;
        }

        private bool CanManualPrint(PrintJob arg)
        {
            return arg != null && SelectedTicket != null && (!SelectedTicket.IsLocked || SelectedTicket.Model.GetPrintCount(arg.Id) == 0);
        }

        private void OnManualPrint(PrintJob obj)
        {
            AppServices.PrintService.ManualPrintTicket(SelectedTicket.Model, obj);
            SelectedTicket.PrintJobButtons.Where(x => x.Model.UseFromPaymentScreen).ToList().ForEach(
                x => CommandButtons.First(u => u.Parameter == x.Model).Caption = x.Caption);
        }

        private static bool CanAutoSetDiscount(string arg)
        {
            return AppServices.MainDataContext.SelectedTicket != null
                   && AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() > 0
                   && AppServices.IsUserPermittedFor(PermissionNames.RoundPayment);
        }

        private bool CanSetDiscount(string arg)
        {
            if (GetTenderedValue() == 0) return true;
            return AppServices.MainDataContext.SelectedTicket != null
                && GetTenderedValue() <= AppServices.MainDataContext.SelectedTicket.GetRemainingAmount()
                && (AppServices.MainDataContext.SelectedTicket.TotalAmount > 0)
                && AppServices.IsUserPermittedFor(PermissionNames.MakeDiscount)
                && AppServices.IsUserPermittedFor(PermissionNames.RoundPayment);
        }

        private bool CanSetDiscountRate(string arg)
        {
            if (GetTenderedValue() == 0) return true;
            return AppServices.MainDataContext.SelectedTicket != null
                && AppServices.MainDataContext.SelectedTicket.TotalAmount > 0
                && AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() > 0
                && GetTenderedValue() <= 100 && AppServices.IsUserPermittedFor(PermissionNames.MakeDiscount);
        }

        private bool CanClosePaymentScreen(string arg)
        {
            return string.IsNullOrEmpty(TenderedAmount) || (SelectedTicket != null && SelectedTicket.Model.GetRemainingAmount() == 0);
        }

        private void OnTenderAllCommand(string obj)
        {
            TenderedAmount = PaymentAmount;
            _resetAmount = true;
        }

        private void OnSubmitAccountPayment(string obj)
        {
            SubmitPayment(PaymentType.Account);
        }

        private void OnSubmitTicketPayment(string obj)
        {
            SubmitPayment(PaymentType.Ticket);
        }

        private void OnSubmitCreditCardPayment(string obj)
        {
            SubmitPayment(PaymentType.CreditCard);
        }

        private void OnSubmitCashPayment(string obj)
        {
            SubmitPayment(PaymentType.Cash);
        }

        private void OnDivideValue(string obj)
        {
            decimal tenderedValue = GetTenderedValue();
            CancelMergedItems();
            _resetAmount = true;
            string dc = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            obj = obj.Replace(",", dc);
            obj = obj.Replace(".", dc);

            decimal value = Convert.ToDecimal(obj);
            var remainingTicketAmount = SelectedTicket.Model.GetRemainingAmount();

            if (value > 0)
            {
                var amount = remainingTicketAmount / value;
                if (amount > remainingTicketAmount) amount = remainingTicketAmount;
                TenderedAmount = amount.ToString("#,#0.00");
            }
            else
            {
                value = tenderedValue;
                if (value > 0)
                {
                    var amount = remainingTicketAmount / value;
                    if (amount > remainingTicketAmount) amount = remainingTicketAmount;
                    TenderedAmount = (amount).ToString("#,#0.00");
                }
            }
        }

        private void OnSetValue(string obj)
        {
            _resetAmount = true;
            ReturningAmountVisibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(obj))
            {
                TenderedAmount = "";
                PaymentAmount = "";
                CancelMergedItems();
                return;
            }

            var value = Convert.ToDecimal(obj);
            if (string.IsNullOrEmpty(TenderedAmount))
                TenderedAmount = "0";
            var tenderedValue = Convert.ToDecimal(TenderedAmount.Replace(
                CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, ""));
            tenderedValue += value;
            TenderedAmount = tenderedValue.ToString("#,#0.00");
        }

        private void OnTypeValueExecuted(string obj)
        {
            if (_resetAmount) TenderedAmount = "";
            _resetAmount = false;
            ReturningAmountVisibility = Visibility.Collapsed;
            TenderedAmount = Helpers.AddTypedValue(TenderedAmount, obj, "#,#0.");
        }

        private void OnClosePaymentScreen(string obj)
        {
            ClosePaymentScreen();
        }

        private void ClosePaymentScreen()
        {
            var paidItems = MergedItems.SelectMany(x => x.PaidItems);
            SelectedTicket.UpdatePaidItems(paidItems);
            AppServices.MainDataContext.SelectedTicket.PublishEvent(EventTopicNames.PaymentSubmitted);
            TenderedAmount = "";
            ReturningAmount = "";
            ReturningAmountVisibility = Visibility.Collapsed;
            SelectedTicket = null;
        }

        private bool CanSubmitAccountPayment(string arg)
        {
            return AppServices.MainDataContext.SelectedTicket != null
                && AppServices.MainDataContext.SelectedTicket.CustomerId > 0
                && GetTenderedValue() == GetPaymentValue()
                && AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() > 0;
        }

        private bool CanSubmitCashPayment(string arg)
        {
            return AppServices.MainDataContext.SelectedTicket != null
                && GetTenderedValue() > 0
                && AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() > 0
                && (AppServices.CurrentTerminal.DepartmentId == 0 ||
                !AppServices.MainDataContext
                .SelectedTicket
                .Payments.Any(x => x.DepartmentId != AppServices.MainDataContext.SelectedDepartment.Id));
        }

        private decimal GetTenderedValue()
        {
            decimal result;
            decimal.TryParse(TenderedAmount, out result);
            return result;
        }

        private decimal GetPaymentValue()
        {
            decimal result;
            decimal.TryParse(PaymentAmount, out result);
            return result;
        }

        private void SubmitPayment(PaymentType paymentType)
        {
            if (paymentType == PaymentType.CreditCard && CreditCardProcessingService.CanProcessCreditCards)
            {
                if (SelectedTicket.Id == 0)
                {
                    var result = AppServices.MainDataContext.CloseTicket();
                    AppServices.MainDataContext.OpenTicket(result.TicketId);
                }
                var ccpd = new CreditCardProcessingData
                               {
                                   TenderedAmount = GetTenderedValue(),
                                   Ticket = AppServices.MainDataContext.SelectedTicket
                               };
                CreditCardProcessingService.Process(ccpd);
                return;
            }
            ProcessPayment(paymentType);
        }

        private void ProcessPayment(PaymentType paymentType)
        {
            var tenderedAmount = GetTenderedValue();
            var originalTenderedAmount = tenderedAmount;
            var remainingTicketAmount = GetPaymentValue();
            var returningAmount = 0m;
            if (tenderedAmount == 0) tenderedAmount = remainingTicketAmount;

            if (tenderedAmount > remainingTicketAmount)
            {
                returningAmount = tenderedAmount - remainingTicketAmount;
                tenderedAmount = remainingTicketAmount;
            }

            if (tenderedAmount > 0)
            {
                if (tenderedAmount > AppServices.MainDataContext.SelectedTicket.GetRemainingAmount())
                    tenderedAmount = AppServices.MainDataContext.SelectedTicket.GetRemainingAmount();

                TicketViewModel.AddPaymentToSelectedTicket(tenderedAmount, paymentType);

                PaymentAmount = (GetPaymentValue() - tenderedAmount).ToString("#,#0.00");

                LastTenderedAmount = tenderedAmount <= AppServices.MainDataContext.SelectedTicket.GetRemainingAmount()
                    ? tenderedAmount.ToString("#,#0.00")
                    : AppServices.MainDataContext.SelectedTicket.GetRemainingAmount().ToString("#,#0.00");
            }

            ReturningAmount = string.Format(Resources.ChangeAmount_f, returningAmount.ToString(LocalSettings.DefaultCurrencyFormat));
            ReturningAmountVisibility = returningAmount > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (returningAmount > 0)
            {
                RuleExecutor.NotifyEvent(RuleEventNames.ChangeAmountChanged,
                    new { Ticket = SelectedTicket.Model, TicketAmount = SelectedTicket.Model.TotalAmount, ChangeAmount = returningAmount, TenderedAmount = originalTenderedAmount });
            }

            if (returningAmount == 0 && AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() == 0)
            {
                ClosePaymentScreen();
            }
            else PersistMergedItems();
        }

        private TicketViewModel _selectedTicket;
        public TicketViewModel SelectedTicket
        {
            get { return _selectedTicket; }
            private set
            {
                _selectedTicket = value;
                RaisePropertyChanged("SelectedTicket");
                RaisePropertyChanged("SelectedTicketTitle");
            }
        }

        public decimal TicketRemainingValue { get; set; }

        private void OnSetDiscountRateCommand(string obj)
        {
            var tenderedvalue = GetTenderedValue();
            if (tenderedvalue == 0 && SelectedTicket.Model.GetDiscountTotal() == 0)
            {
                InteractionService.UserIntraction.GiveFeedback(Resources.EmptyDiscountRateFeedback);
            }
            if (tenderedvalue > 0 && SelectedTicket.Model.GetPlainSum() > 0)
            {
                //var discounts = SelectedTicket.Model.GetDiscountAndRoundingTotal();
                //var discountAmount = SelectedTicket.Model.GetRemainingAmount() + discounts-SelectedTicket.Model.CalculateVat()-SelectedTicket.Model.GetTaxServicesTotal();
                //discountAmount = discountAmount * (GetTenderedValue() / 100);
                //var discountRate = SelectedTicket.Model.GetPlainSum();
                //discountRate = (discountAmount * 100) / discountRate;
                //discountRate = decimal.Round(discountRate, LocalSettings.Decimals);
                SelectedTicket.Model.AddTicketDiscount(DiscountType.Percent, GetTenderedValue(), AppServices.CurrentLoggedInUser.Id);
            }
            else SelectedTicket.Model.AddTicketDiscount(DiscountType.Percent, 0, AppServices.CurrentLoggedInUser.Id);
            PaymentAmount = "";
            RefreshValues();
        }

        private void OnAutoSetDiscount(string obj)
        {
            if (GetTenderedValue() == 0) return;
            if (!AppServices.IsUserPermittedFor(PermissionNames.FixPayment) && GetTenderedValue() > GetPaymentValue()) return;
            if (!AppServices.IsUserPermittedFor(PermissionNames.RoundPayment)) return;
            SelectedTicket.Model.AddTicketDiscount(DiscountType.Amount, 0, AppServices.CurrentLoggedInUser.Id);
            SelectedTicket.Model.AddTicketDiscount(DiscountType.Auto, 0, AppServices.CurrentLoggedInUser.Id);

            SelectedTicket.Model.AddTicketDiscount(DiscountType.Amount, AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() - GetTenderedValue(), AppServices.CurrentLoggedInUser.Id);
            PaymentAmount = "";
            RefreshValues();
        }

        private void OnSetDiscountAmountCommand(string obj)
        {
            if (GetTenderedValue() > GetPaymentValue()) return;
            SelectedTicket.Model.AddTicketDiscount(DiscountType.Amount, GetTenderedValue(), AppServices.CurrentLoggedInUser.Id);
            PaymentAmount = "";
            RefreshValues();
        }

        public void RefreshValues()
        {
            SelectedTicket.RecalculateTicket();
            if (SelectedTicket.Model.GetRemainingAmount() < 0)
            {
                SelectedTicket.Model.Discounts.Clear();
                SelectedTicket.RecalculateTicket();
                InteractionService.UserIntraction.GiveFeedback(Resources.AllDiscountsRemoved);
            }
            if (GetPaymentValue() <= 0)
                PaymentAmount = AppServices.MainDataContext.SelectedTicket != null
                    ? AppServices.MainDataContext.SelectedTicket.GetRemainingAmount().ToString("#,#0.00")
                    : "";
            SelectedTicket.Discounts.Clear();
            SelectedTicket.Discounts.AddRange(SelectedTicket.Model.Discounts.Select(x => new DiscountViewModel(x)));

            RaisePropertyChanged("SelectedTicket");
            RaisePropertyChanged("ReturningAmountVisibility");
            RaisePropertyChanged("PaymentsVisibility");
            RaisePropertyChanged("ReturningAmount");
            TenderedAmount = "";
        }

        public void PrepareMergedItems()
        {
            MergedItems.Clear();
            PaymentAmount = "";
            _selectedTotal = 0;

            var serviceAmount = SelectedTicket.Model.GetTaxServicesTotal();
            var sum = SelectedTicket.Model.GetSumWithoutTax();
            if (sum == 0) return;

            foreach (var item in SelectedTicket.Model.TicketItems)
            {
                if (!item.Voided && !item.Gifted)
                {
                    var ticketItem = item;
                    var price = ticketItem.GetItemPrice();
                    price += (price * serviceAmount) / sum;
                    if (!ticketItem.VatIncluded) price += ticketItem.VatAmount;
                    var mitem = MergedItems.SingleOrDefault(x => x.MenuItemId == ticketItem.MenuItemId && x.Price == price);
                    if (mitem == null)
                    {
                        mitem = new MergedItem { Description = item.MenuItemName + item.GetPortionDesc(), Price = price, MenuItemId = item.MenuItemId };
                        MergedItems.Add(mitem);
                    }
                    mitem.Quantity += item.Quantity;
                }
            }

            foreach (var paidItem in SelectedTicket.Model.PaidItems)
            {
                var item = paidItem;
                var mi = MergedItems.SingleOrDefault(x => x.MenuItemId == item.MenuItemId && x.Price == item.Price);
                if (mi != null)
                    mi.PaidItems.Add(paidItem);
            }
        }

        private decimal _selectedTotal;

        private void OnMergedItemSelected(MergedItem obj)
        {
            if (obj.RemainingQuantity > 0)
            {
                decimal quantity = 1;
                if (GetTenderedValue() > 0) quantity = GetTenderedValue();
                if (quantity > obj.RemainingQuantity) quantity = obj.RemainingQuantity;
                _selectedTotal += obj.Price * quantity;
                if (_selectedTotal > AppServices.MainDataContext.SelectedTicket.GetRemainingAmount())
                    _selectedTotal = AppServices.MainDataContext.SelectedTicket.GetRemainingAmount();
                PaymentAmount = _selectedTotal.ToString("#,#0.00");
                TenderedAmount = "";
                _resetAmount = true;
                obj.IncQuantity(quantity);
                AppServices.MainDataContext.SelectedTicket.UpdatePaidItems(obj.MenuItemId);
            }
            ReturningAmountVisibility = Visibility.Collapsed;
        }

        private void PersistMergedItems()
        {
            _selectedTotal = 0;
            foreach (var mergedItem in MergedItems)
            {
                mergedItem.PersistPaidItems();
            }
            RefreshValues();
            AppServices.MainDataContext.SelectedTicket.CancelPaidItems();
        }

        private void CancelMergedItems()
        {
            _selectedTotal = 0;
            foreach (var mergedItem in MergedItems)
            {
                mergedItem.CancelPaidItems();
            }
            RefreshValues();
            ReturningAmountVisibility = Visibility.Collapsed;
            AppServices.MainDataContext.SelectedTicket.CancelPaidItems();
        }

        public void CreateButtons()
        {
            CommandButtons = CreateCommandButtons();
            RaisePropertyChanged("CommandButtons");
        }

        public void Prepare()
        {
            if (SelectedTicket == null)
            {
                SelectedTicket = new TicketViewModel(AppServices.MainDataContext.SelectedTicket, AppServices.MainDataContext.SelectedDepartment.IsFastFood);
                TicketRemainingValue = AppServices.MainDataContext.SelectedTicket.GetRemainingAmount();
                PrepareMergedItems();
                RefreshValues();
                LastTenderedAmount = PaymentAmount;
                CreateButtons();
                OnSetValue("");
                if (CreditCardProcessingService.ForcePayment(SelectedTicket.Id))
                {
                    TenderedAmount = TicketRemainingValue.ToString("#,#0.00");
                    SubmitPayment(PaymentType.CreditCard);
                }
            }
        }

        private void OnProcessCreditCard(EventParameters<CreditCardProcessingResult> obj)
        {
            if (obj.Topic == EventTopicNames.PaymentProcessed && AppServices.ActiveAppScreen == AppScreens.Payment)
            {
                if (obj.Value.ProcessType == ProcessType.Force || obj.Value.ProcessType == ProcessType.External)
                {
                    SelectedTicket = null;
                    Prepare();
                    Debug.Assert(SelectedTicket != null);
                    PaymentAmount = SelectedTicket.Model.GetRemainingAmount().ToString("#,#0.00");
                    TenderedAmount = obj.Value.Amount.ToString("#,#0.00");
                    ProcessPayment(PaymentType.CreditCard);
                }
                if (obj.Value.ProcessType == ProcessType.PreAuth)
                    ClosePaymentScreen();
                if (obj.Value.ProcessType == ProcessType.Cancel)
                    AppServices.MainDataContext.SelectedTicket.PublishEvent(EventTopicNames.MakePayment);
            }
        }
    }

    public class MergedItem : ObservableObject
    {
        public int MenuItemId { get; set; }
        private decimal _quantity;
        public decimal Quantity { get { return _quantity; } set { _quantity = value; RaisePropertyChanged("Quantity"); } }
        public string Description { get; set; }
        public string Label { get { return GetPaidItemsQuantity() > 0 ? string.Format("{0} ({1:#.##})", Description, GetPaidItemsQuantity()) : Description; } }
        public decimal Price { get; set; }
        public decimal Total { get { return (Price * Quantity) - PaidItems.Sum(x => x.Price * x.Quantity); } }
        public string TotalLabel { get { return Total > 0 ? Total.ToString("#,#0.00") : ""; } }
        public List<PaidItem> PaidItems { get; set; }
        public List<PaidItem> NewPaidItems { get; set; }
        public FontWeight FontWeight { get; set; }

        public MergedItem()
        {
            PaidItems = new List<PaidItem>();
            NewPaidItems = new List<PaidItem>();
            FontWeight = FontWeights.Normal;
        }

        private decimal GetPaidItemsQuantity()
        {
            return PaidItems.Sum(x => x.Quantity) + NewPaidItems.Sum(x => x.Quantity);
        }

        public decimal RemainingQuantity { get { return Quantity - GetPaidItemsQuantity(); } }

        public void IncQuantity(decimal quantity)
        {
            var pitem = new PaidItem { MenuItemId = MenuItemId, Price = Price };
            NewPaidItems.Add(pitem);
            pitem.Quantity += quantity;
            FontWeight = FontWeights.Bold;
            Refresh();
        }

        public void PersistPaidItems()
        {
            foreach (var newPaidItem in NewPaidItems)
            {
                var item = newPaidItem;
                var pitem = PaidItems.SingleOrDefault(
                        x => x.MenuItemId == item.MenuItemId && x.Price == item.Price);
                if (pitem != null)
                {
                    pitem.Quantity += newPaidItem.Quantity;
                }
                else PaidItems.Add(newPaidItem);
            }

            NewPaidItems.Clear();
            FontWeight = FontWeights.Normal;
            Refresh();
        }

        public void CancelPaidItems()
        {
            NewPaidItems.Clear();
            FontWeight = FontWeights.Normal;
            Refresh();
        }

        public void Refresh()
        {
            RaisePropertyChanged("Label");
            RaisePropertyChanged("TotalLabel");
            RaisePropertyChanged("FontWeight");
        }

    }
}