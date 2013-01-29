using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Samba.Domain;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Interaction;
using Samba.Presentation.Common.Services;
using Samba.Presentation.ViewModels;
using Samba.Services;


namespace Samba.Modules.TicketModule
{
    public class TicketListViewModel : ObservableObject
    {
        private const int OpenTicketListView = 0;
        private const int SingleTicketView = 1;

        private readonly Timer _timer;

        public DelegateCommand<ScreenMenuItemData> AddMenuItemCommand { get; set; }
        public CaptionCommand<string> CloseTicketCommand { get; set; }
        public DelegateCommand<int?> OpenTicketCommand { get; set; }
        public CaptionCommand<string> MakePaymentCommand { get; set; }

        public CaptionCommand<string> MakeCashPaymentCommand { get; set; }
        public CaptionCommand<string> MakeCreditCardPaymentCommand { get; set; }
        public CaptionCommand<string> MakeTicketPaymentCommand { get; set; }
        public CaptionCommand<string> SelectTableCommand { get; set; }
        public CaptionCommand<string> SelectCustomerCommand { get; set; }
        public CaptionCommand<string> PrintTicketCommand { get; set; }
        public CaptionCommand<string> PrintInvoiceCommand { get; set; }
        public CaptionCommand<string> ShowAllOpenTickets { get; set; }
        public CaptionCommand<string> MoveTicketItemsCommand { get; set; }

        public ICaptionCommand IncQuantityCommand { get; set; }
        public ICaptionCommand DecQuantityCommand { get; set; }
        public ICaptionCommand IncSelectionQuantityCommand { get; set; }
        public ICaptionCommand DecSelectionQuantityCommand { get; set; }
        public ICaptionCommand ShowVoidReasonsCommand { get; set; }
        public ICaptionCommand ShowGiftReasonsCommand { get; set; }
        public ICaptionCommand ShowTicketTagsCommand { get; set; }
        public ICaptionCommand CancelItemCommand { get; set; }
        public ICaptionCommand ShowExtraPropertyEditorCommand { get; set; }
        public ICaptionCommand EditTicketNoteCommand { get; set; }
        public ICaptionCommand RemoveTicketLockCommand { get; set; }
        public ICaptionCommand RemoveTicketTagCommand { get; set; }
        public ICaptionCommand ChangePriceCommand { get; set; }
        public ICaptionCommand PrintJobCommand { get; set; }
        public DelegateCommand<TicketTagFilterViewModel> FilterOpenTicketsCommand { get; set; }

        private TicketViewModel _selectedTicket;
        public TicketViewModel SelectedTicket
        {
            get
            {
                if (AppServices.MainDataContext.SelectedTicket == null) _selectedTicket = null;

                if (_selectedTicket == null && AppServices.MainDataContext.SelectedTicket != null)
                    _selectedTicket = new TicketViewModel(AppServices.MainDataContext.SelectedTicket,
                      AppServices.MainDataContext.SelectedDepartment != null && AppServices.MainDataContext.SelectedDepartment.IsFastFood);
                return _selectedTicket;
            }
        }

        private readonly ObservableCollection<TicketItemViewModel> _selectedTicketItems;
        public TicketItemViewModel SelectedTicketItem
        {
            get
            {
                return SelectedTicket != null && SelectedTicket.SelectedItems.Count == 1 ? SelectedTicket.SelectedItems[0] : null;
            }
        }

        public IEnumerable<PrintJobButton> PrintJobButtons
        {
            get
            {
                return SelectedTicket != null
                    ? SelectedTicket.PrintJobButtons.Where(x => x.Model.UseFromPos)
                    : null;
            }
        }

        public IEnumerable<Department> Departments { get { return AppServices.MainDataContext.Departments; } }
        public IEnumerable<Department> PermittedDepartments { get { return AppServices.MainDataContext.PermittedDepartments; } }

        public IEnumerable<OpenTicketViewModel> OpenTickets { get; set; }

        private IEnumerable<TicketTagFilterViewModel> _openTicketTags;
        public IEnumerable<TicketTagFilterViewModel> OpenTicketTags
        {
            get { return _openTicketTags; }
            set
            {
                _openTicketTags = value;
                RaisePropertyChanged("OpenTicketTags");
            }
        }

        private string _selectedTag;
        public string SelectedTag
        {
            get { return !string.IsNullOrEmpty(_selectedTag) ? _selectedTag : SelectedDepartment != null ? SelectedDepartment.DefaultTag : null; }
            set { _selectedTag = value; }
        }

        private int _selectedTicketView;
        public int SelectedTicketView
        {
            get { return _selectedTicketView; }
            set
            {
                StopTimer();
                if (value == OpenTicketListView)
                {
                    AppServices.ActiveAppScreen = AppScreens.TicketList;
                    StartTimer();
                }
                if (value == SingleTicketView)
                {
                    AppServices.ActiveAppScreen = AppScreens.SingleTicket;
                }
                _selectedTicketView = value;
                RaisePropertyChanged("SelectedTicketView");
            }
        }

        public Department SelectedDepartment
        {
            get { return AppServices.MainDataContext.SelectedDepartment; }
            set
            {
                if (value != AppServices.MainDataContext.SelectedDepartment)
                {
                    AppServices.MainDataContext.SelectedDepartment = value;
                    RaisePropertyChanged("SelectedDepartment");
                    RaisePropertyChanged("SelectedTicket");
                    SelectedDepartment.PublishEvent(EventTopicNames.SelectedDepartmentChanged);
                }
            }
        }

        public bool IsDepartmentSelectorVisible
        {
            get
            {
                return PermittedDepartments.Count() > 1 &&
                       AppServices.IsUserPermittedFor(PermissionNames.ChangeDepartment);
            }
        }

        public bool IsItemsSelected { get { return _selectedTicketItems.Count > 0; } }
        public bool IsItemsSelectedAndUnlocked { get { return _selectedTicketItems.Count > 0 && _selectedTicketItems.Where(x => x.IsLocked).Count() == 0; } }
        public bool IsItemsSelectedAndLocked { get { return _selectedTicketItems.Count > 0 && _selectedTicketItems.Where(x => !x.IsLocked).Count() == 0; } }
        public bool IsNothingSelected { get { return _selectedTicketItems.Count == 0; } }
        public bool IsNothingSelectedAndTicketLocked { get { return _selectedTicket != null && _selectedTicketItems.Count == 0 && _selectedTicket.IsLocked; } }
        public bool IsNothingSelectedAndTicketTagged { get { return _selectedTicket != null && _selectedTicketItems.Count == 0 && SelectedTicket.IsTagged; } }
        public bool IsTicketSelected { get { return SelectedTicket != null && _selectedTicketItems.Count == 0; } }
        public bool IsTicketTotalVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketTotalVisible; } }
        public bool IsTicketPaymentVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketPaymentVisible; } }
        public bool IsTicketRemainingVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketRemainingVisible; } }
        public bool IsTicketDiscountVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketDiscountVisible; } }
        public bool IsTicketRoundingVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketRoundingVisible; } }
        public bool IsTicketVatTotalVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketVatTotalVisible; } }
        public bool IsTicketTaxServiceVisible { get { return SelectedTicket != null && SelectedTicket.IsTicketTaxServiceVisible; } }
        public bool IsPlainTotalVisible { get { return IsTicketDiscountVisible || IsTicketVatTotalVisible || IsTicketRoundingVisible || IsTicketTaxServiceVisible; } }

        public bool IsTableButtonVisible
        {
            get
            {
                return ((AppServices.MainDataContext.TableCount > 0 ||
                        (AppServices.MainDataContext.SelectedDepartment != null
                        && AppServices.MainDataContext.SelectedDepartment.IsAlaCarte))
                        && IsNothingSelected) &&
                        ((AppServices.MainDataContext.SelectedDepartment != null &&
                        AppServices.MainDataContext.SelectedDepartment.TableScreenId > 0));
            }
        }

        public bool IsCustomerButtonVisible
        {
            get
            {
                return (AppServices.MainDataContext.CustomerCount > 0 ||
                    (AppServices.MainDataContext.SelectedDepartment != null
                    && AppServices.MainDataContext.SelectedDepartment.IsTakeAway))
                    && IsNothingSelected;
            }
        }

        public bool CanChangeDepartment
        {
            get { return SelectedTicket == null && AppServices.MainDataContext.IsCurrentWorkPeriodOpen; }
        }

        public Brush TicketBackground { get { return SelectedTicket != null && (SelectedTicket.IsLocked || SelectedTicket.IsPaid) ? SystemColors.ControlLightBrush : SystemColors.WindowBrush; } }

        public int OpenTicketListViewColumnCount { get { return SelectedDepartment != null ? SelectedDepartment.OpenTicketViewColumnCount : 5; } }
        public TicketItemViewModel LastSelectedTicketItem { get; set; }

        public IEnumerable<TicketTagButton> TicketTagButtons
        {
            get
            {
                return AppServices.MainDataContext.SelectedDepartment != null
                    ? AppServices.MainDataContext.SelectedDepartment.TicketTagGroups
                    .Where(x => x.ActiveOnPosClient)
                    .OrderBy(x => x.Order)
                    .Select(x => new TicketTagButton(x, SelectedTicket))
                    : null;
            }
        }

        public TicketListViewModel()
        {
            _timer = new Timer(OnTimer, null, Timeout.Infinite, 1000);
            _selectedTicketItems = new ObservableCollection<TicketItemViewModel>();

            PrintJobCommand = new CaptionCommand<PrintJob>(Resources.Print, OnPrintJobExecute, CanExecutePrintJob);

            AddMenuItemCommand = new DelegateCommand<ScreenMenuItemData>(OnAddMenuItemCommandExecute);
            CloseTicketCommand = new CaptionCommand<string>(Resources.CloseTicket_r, OnCloseTicketExecute, CanCloseTicket);
            OpenTicketCommand = new DelegateCommand<int?>(OnOpenTicketExecute);
            MakePaymentCommand = new CaptionCommand<string>(Resources.GetPayment, OnMakePaymentExecute, CanMakePayment);
            MakeCashPaymentCommand = new CaptionCommand<string>(Resources.CashPayment_r, OnMakeCashPaymentExecute, CanMakeFastPayment);
            MakeCreditCardPaymentCommand = new CaptionCommand<string>(Resources.CreditCard_r, OnMakeCreditCardPaymentExecute, CanMakeFastPayment);
            MakeTicketPaymentCommand = new CaptionCommand<string>(Resources.Voucher_r, OnMakeTicketPaymentExecute, CanMakeFastPayment);
            SelectTableCommand = new CaptionCommand<string>(Resources.SelectTable, OnSelectTableExecute, CanSelectTable);
            SelectCustomerCommand = new CaptionCommand<string>(Resources.SelectCustomer, OnSelectCustomerExecute, CanSelectCustomer);
            ShowAllOpenTickets = new CaptionCommand<string>(Resources.AllTickets_r, OnShowAllOpenTickets);
            FilterOpenTicketsCommand = new DelegateCommand<TicketTagFilterViewModel>(OnFilterOpenTicketsExecute);

            IncQuantityCommand = new CaptionCommand<string>("+", OnIncQuantityCommand, CanIncQuantity);
            DecQuantityCommand = new CaptionCommand<string>("-", OnDecQuantityCommand, CanDecQuantity);
            IncSelectionQuantityCommand = new CaptionCommand<string>("(+)", OnIncSelectionQuantityCommand, CanIncSelectionQuantity);
            DecSelectionQuantityCommand = new CaptionCommand<string>("(-)", OnDecSelectionQuantityCommand, CanDecSelectionQuantity);
            ShowVoidReasonsCommand = new CaptionCommand<string>(Resources.Void, OnShowVoidReasonsExecuted, CanVoidSelectedItems);
            ShowGiftReasonsCommand = new CaptionCommand<string>(Resources.Gift, OnShowGiftReasonsExecuted, CanGiftSelectedItems);
            ShowTicketTagsCommand = new CaptionCommand<TicketTagGroup>(Resources.Tag, OnShowTicketsTagExecute, CanExecuteShowTicketTags);
            CancelItemCommand = new CaptionCommand<string>(Resources.Cancel, OnCancelItemCommand, CanCancelSelectedItems);
            MoveTicketItemsCommand = new CaptionCommand<string>(Resources.MoveTicketLine, OnMoveTicketItems, CanMoveTicketItems);
            ShowExtraPropertyEditorCommand = new CaptionCommand<string>(Resources.ExtraModifier, OnShowExtraProperty, CanShowExtraProperty);
            EditTicketNoteCommand = new CaptionCommand<string>(Resources.TicketNote, OnEditTicketNote, CanEditTicketNote);
            RemoveTicketLockCommand = new CaptionCommand<string>(Resources.ReleaseLock, OnRemoveTicketLock, CanRemoveTicketLock);
            ChangePriceCommand = new CaptionCommand<string>(Resources.ChangePrice, OnChangePrice, CanChangePrice);

            EventServiceFactory.EventService.GetEvent<GenericEvent<LocationData>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.LocationSelectedForTicket)
                    {
                        if (SelectedTicket != null)
                        {
                            var oldLocationName = SelectedTicket.Location;
                            var ticketsMerged = x.Value.TicketId > 0 && x.Value.TicketId != SelectedTicket.Id;
                            TicketViewModel.AssignTableToSelectedTicket(x.Value.LocationId);

                            CloseTicket();

                            if (!AppServices.CurrentTerminal.AutoLogout)
                                EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateTicketView);

                            if (!string.IsNullOrEmpty(oldLocationName) || ticketsMerged)
                                if (ticketsMerged && !string.IsNullOrEmpty(oldLocationName))
                                    InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.TablesMerged_f, oldLocationName, x.Value.Caption));
                                else if (ticketsMerged)
                                    InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.TicketMergedToTable_f, x.Value.Caption));
                                else if (oldLocationName != x.Value.LocationName)
                                    InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.TicketMovedToTable_f, oldLocationName, x.Value.Caption));
                        }
                        else
                        {
                            if (x.Value.TicketId == 0)
                            {
                                TicketViewModel.CreateNewTicket();
                                TicketViewModel.AssignTableToSelectedTicket(x.Value.LocationId);
                            }
                            else
                            {
                                AppServices.MainDataContext.OpenTicket(x.Value.TicketId);
                                if (SelectedTicket != null)
                                {
                                    if (SelectedTicket.Location != x.Value.LocationName)
                                        AppServices.MainDataContext.ResetTableDataForSelectedTicket();
                                }
                            }
                            EventServiceFactory.EventService.PublishEvent(EventTopicNames.DisplayTicketView);
                        }
                    }

                }
                );

            EventServiceFactory.EventService.GetEvent<GenericEvent<WorkPeriod>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.WorkPeriodStatusChanged)
                    {
                        RaisePropertyChanged("CanChangeDepartment");
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketItemViewModel>>().Subscribe(
                x =>
                {
                    if (SelectedTicket != null && x.Topic == EventTopicNames.SelectedItemsChanged)
                    {
                        LastSelectedTicketItem = x.Value.Selected ? x.Value : null;
                        foreach (var item in SelectedTicket.SelectedItems)
                        { item.IsLastSelected = item == LastSelectedTicketItem; }

                        SelectedTicket.PublishEvent(EventTopicNames.SelectedItemsChanged);
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketTagData>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.TagSelectedForSelectedTicket)
                    {
                        if (x.Value.Action == 1 && CanCloseTicket(""))
                            CloseTicketCommand.Execute("");
                        if (x.Value.Action == 2 && CanMakePayment(""))
                            MakePaymentCommand.Execute("");
                        else
                        {
                            RefreshVisuals();
                        }
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketViewModel>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.SelectedItemsChanged)
                    {
                        _selectedTicketItems.Clear();
                        _selectedTicketItems.AddRange(x.Value.SelectedItems);
                        if (x.Value.SelectedItems.Count == 0) LastSelectedTicketItem = null;
                        RaisePropertyChanged("IsItemsSelected");
                        RaisePropertyChanged("IsNothingSelected");
                        RaisePropertyChanged("IsNothingSelectedAndTicketLocked");
                        RaisePropertyChanged("IsTableButtonVisible");
                        RaisePropertyChanged("IsCustomerButtonVisible");
                        RaisePropertyChanged("IsItemsSelectedAndUnlocked");
                        RaisePropertyChanged("IsItemsSelectedAndLocked");
                        RaisePropertyChanged("IsTicketSelected");
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<Customer>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.CustomerSelectedForTicket)
                    {
                        if (AppServices.MainDataContext.SelectedTicket == null)
                            TicketViewModel.CreateNewTicket();

                        AppServices.MainDataContext.AssignCustomerToSelectedTicket(x.Value);

                        var lastOrder = Dao.Last<Ticket>(y => y.CustomerId == x.Value.Id);

                        var lastOderDayCount = lastOrder != null
                            ? Convert.ToInt32(new TimeSpan(DateTime.Now.Ticks - lastOrder.Date.Ticks).TotalDays) : -1;
                        var lastOderTotal = lastOrder != null
                            ? lastOrder.TotalAmount : -1;

                        RuleExecutor.NotifyEvent(RuleEventNames.CustomerSelectedForTicket,
                            new
                            {
                                Ticket = AppServices.MainDataContext.SelectedTicket,
                                CustomerId = x.Value.Id,
                                CustomerName = x.Value.Name,
                                x.Value.PhoneNumber,
                                CustomerNote = x.Value.Note,
                                CustomerGroupCode = x.Value.GroupCode,
                                LastOrderTotal = lastOderTotal,
                                LastOrderDayCount = lastOderDayCount
                            });

                        if (!string.IsNullOrEmpty(SelectedTicket.CustomerName) && SelectedTicket.Items.Count > 0)
                            CloseTicket();
                        else
                        {
                            RefreshVisuals();
                            SelectedTicketView = SingleTicketView;
                        }
                    }

                    if (x.Topic == EventTopicNames.PaymentRequestedForTicket)
                    {
                        AppServices.MainDataContext.AssignCustomerToSelectedTicket(x.Value);
                        if (!string.IsNullOrEmpty(SelectedTicket.CustomerName) && SelectedTicket.Items.Count > 0)
                            MakePaymentCommand.Execute("");
                        else
                        {
                            RefreshVisuals();
                            SelectedTicketView = SingleTicketView;
                        }

                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<Ticket>>().Subscribe(
                x =>
                {
                    _selectedTicket = null;

                    if (x.Topic == EventTopicNames.PaymentSubmitted)
                    {
                        CloseTicket();
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                 x =>
                 {
                     if (SelectedDepartment == null)
                         UpdateSelectedDepartment(AppServices.CurrentLoggedInUser.UserRole.DepartmentId);

                     if (x.Topic == EventTopicNames.ActivateTicketView)
                     {
                         UpdateSelectedTicketView();
                         DisplayTickets();
                     }

                     if (x.Topic == EventTopicNames.DisplayTicketView)
                     {
                         UpdateSelectedTicketView();
                         RefreshVisuals();
                     }

                     if (x.Topic == EventTopicNames.RefreshSelectedTicket)
                     {
                         _selectedTicket = null;
                         RefreshVisuals();
                         SelectedTicketView = SingleTicketView;
                     }
                 });

            EventServiceFactory.EventService.GetEvent<GenericEvent<Message>>().Subscribe(
                x =>
                {
                    if (AppServices.ActiveAppScreen == AppScreens.TicketList
                        && x.Topic == EventTopicNames.MessageReceivedEvent
                        && x.Value.Command == Messages.TicketRefreshMessage)
                    {
                        RefreshOpenTickets();
                        RefreshVisuals();
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<PopupData>>().Subscribe(
                x =>
                {
                    if (x.Value.EventMessage == "SelectCustomer")
                    {
                        var dep = AppServices.MainDataContext.Departments.FirstOrDefault(y => y.IsTakeAway);
                        if (dep != null)
                        {
                            UpdateSelectedDepartment(dep.Id);
                            SelectedTicketView = OpenTicketListView;
                        }
                        if (SelectedDepartment == null)
                            SelectedDepartment = AppServices.MainDataContext.Departments.FirstOrDefault();
                        RefreshVisuals();
                    }
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<CreditCardProcessingResult>>().Subscribe(OnProcessCreditCard);
        }

        private void OnProcessCreditCard(EventParameters<CreditCardProcessingResult> obj)
        {
            if (obj.Topic == EventTopicNames.PaymentProcessed && AppServices.ActiveAppScreen != AppScreens.Payment)
            {
                if (obj.Value.ProcessType == ProcessType.Force)
                {
                    TicketViewModel.AddPaymentToSelectedTicket(AppServices.MainDataContext.SelectedTicket.GetRemainingAmount(), PaymentType.CreditCard);
                    if (AppServices.MainDataContext.SelectedTicket.GetRemainingAmount() == 0)
                        CloseTicket();
                }
            }
        }

        private void UpdateSelectedTicketView()
        {
            if (SelectedTicket != null || (SelectedDepartment != null && SelectedDepartment.IsFastFood))
                SelectedTicketView = SingleTicketView;
            else if (SelectedDepartment != null)
            {
                SelectedTicketView = OpenTicketListView;
                RefreshOpenTickets();
            }
        }

        private bool CanExecuteShowTicketTags(TicketTagGroup arg)
        {
            return SelectedTicket == null || (SelectedTicket.Model.CanSubmit);
        }

        private void OnShowTicketsTagExecute(TicketTagGroup tagGroup)
        {
            if (SelectedTicket != null)
            {
                _selectedTicket.LastSelectedTicketTag = tagGroup;
                _selectedTicket.PublishEvent(EventTopicNames.SelectTicketTag);
            }
            else if (ShowAllOpenTickets.CanExecute(""))
            {
                SelectedTag = tagGroup.Name;
                RefreshOpenTickets();
                if ((OpenTickets != null && OpenTickets.Count() > 0) || OpenTicketTags.Count() > 0)
                {
                    SelectedTicketView = OpenTicketListView;
                    RaisePropertyChanged("OpenTickets");
                }
                else InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.NoTicketsFoundForTag, tagGroup.Name));
            }
        }

        private bool CanChangePrice(string arg)
        {
            return SelectedTicket != null
                && !SelectedTicket.IsLocked
                && SelectedTicket.Model.CanSubmit
                && _selectedTicketItems.Count == 1
                && (_selectedTicketItems[0].Price == 0 || AppServices.IsUserPermittedFor(PermissionNames.ChangeItemPrice));
        }

        private void OnChangePrice(string obj)
        {
            decimal price;
            decimal.TryParse(AppServices.MainDataContext.NumeratorValue, out price);
            if (price <= 0)
            {
                InteractionService.UserIntraction.GiveFeedback(Resources.ForChangingPriceTypeAPrice);
            }
            else
            {
                _selectedTicketItems[0].UpdatePrice(price);
            }
            _selectedTicket.ClearSelectedItems();
            _selectedTicket.RefreshVisuals();
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ResetNumerator);
        }

        private bool CanExecutePrintJob(PrintJob arg)
        {
            return arg != null && SelectedTicket != null && (!SelectedTicket.IsLocked || SelectedTicket.Model.GetPrintCount(arg.Id) == 0);
        }

        private void OnPrintJobExecute(PrintJob printJob)
        {
            var message = SelectedTicket.GetPrintError();

            if (!string.IsNullOrEmpty(message))
            {
                InteractionService.UserIntraction.GiveFeedback(message);
                return;
            }

            SaveTicketIfNew();

            AppServices.PrintService.ManualPrintTicket(SelectedTicket.Model, printJob);

            if (printJob.WhenToPrint == (int)WhenToPrintTypes.Paid && !SelectedTicket.IsPaid)
                MakePaymentCommand.Execute("");
            else if (printJob.CloseTicket)
                CloseTicket();
        }

        private void SaveTicketIfNew()
        {
            if ((SelectedTicket.Id == 0 || SelectedTicket.Items.Any(x => x.Model.Id == 0)) && SelectedTicket.Items.Count > 0)
            {
                var result = AppServices.MainDataContext.CloseTicket();
                AppServices.MainDataContext.OpenTicket(result.TicketId);
                _selectedTicket = null;
            }
        }

        private bool CanRemoveTicketLock(string arg)
        {
            return SelectedTicket != null && (SelectedTicket.IsLocked) &&
                   AppServices.IsUserPermittedFor(PermissionNames.AddItemsToLockedTickets);
        }

        private void OnRemoveTicketLock(string obj)
        {
            SelectedTicket.IsLocked = false;
            SelectedTicket.RefreshVisuals();
        }

        private void OnMoveTicketItems(string obj)
        {
            SelectedTicket.FixSelectedItems();
            var newTicketId = AppServices.MainDataContext.MoveTicketItems(SelectedTicket.SelectedItems.Select(x => x.Model), 0).TicketId;
            OnOpenTicketExecute(newTicketId);
        }

        private bool CanMoveTicketItems(string arg)
        {
            return SelectedTicket != null && SelectedTicket.CanMoveSelectedItems();
        }

        private bool CanEditTicketNote(string arg)
        {
            return SelectedTicket != null && !SelectedTicket.IsPaid;
        }

        private void OnEditTicketNote(string obj)
        {
            SelectedTicket.PublishEvent(EventTopicNames.EditTicketNote);
        }

        private bool CanShowExtraProperty(string arg)
        {
            return SelectedTicketItem != null && !SelectedTicketItem.Model.Locked && AppServices.IsUserPermittedFor(PermissionNames.ChangeExtraProperty);
        }

        private void OnShowExtraProperty(string obj)
        {
            _selectedTicket.PublishEvent(EventTopicNames.SelectExtraProperty);
        }

        private void OnDecQuantityCommand(string obj)
        {
            LastSelectedTicketItem.Quantity--;
            _selectedTicket.RefreshVisuals();
        }

        private void OnIncQuantityCommand(string obj)
        {
            LastSelectedTicketItem.Quantity++;
            _selectedTicket.RefreshVisuals();
        }

        private bool CanDecQuantity(string arg)
        {
            return LastSelectedTicketItem != null &&
                LastSelectedTicketItem.Quantity > 1 &&
                !LastSelectedTicketItem.IsLocked &&
                !LastSelectedTicketItem.IsVoided;
        }

        private bool CanIncQuantity(string arg)
        {
            return LastSelectedTicketItem != null &&
                !LastSelectedTicketItem.IsLocked &&
                !LastSelectedTicketItem.IsVoided;
        }

        private bool CanDecSelectionQuantity(string arg)
        {
            return LastSelectedTicketItem != null &&
                   LastSelectedTicketItem.Quantity > 1 &&
                   LastSelectedTicketItem.IsLocked &&
                   !LastSelectedTicketItem.IsGifted &&
                   !LastSelectedTicketItem.IsVoided;
        }

        private void OnDecSelectionQuantityCommand(string obj)
        {
            LastSelectedTicketItem.DecSelectedQuantity();
            _selectedTicket.RefreshVisuals();
        }

        private bool CanIncSelectionQuantity(string arg)
        {
            return LastSelectedTicketItem != null &&
               LastSelectedTicketItem.Quantity > 1 &&
               LastSelectedTicketItem.IsLocked &&
               !LastSelectedTicketItem.IsGifted &&
               !LastSelectedTicketItem.IsVoided;
        }

        private void OnIncSelectionQuantityCommand(string obj)
        {
            LastSelectedTicketItem.IncSelectedQuantity();
            _selectedTicket.RefreshVisuals();
        }

        private bool CanVoidSelectedItems(string arg)
        {
            if (_selectedTicket != null && !_selectedTicket.IsLocked && AppServices.IsUserPermittedFor(PermissionNames.VoidItems))
                return _selectedTicket.CanVoidSelectedItems();
            return false;
        }

        private void OnShowVoidReasonsExecuted(string obj)
        {
            _selectedTicket.PublishEvent(EventTopicNames.SelectVoidReason);
        }

        private void OnShowGiftReasonsExecuted(string obj)
        {
            _selectedTicket.PublishEvent(EventTopicNames.SelectGiftReason);
        }

        private bool CanCancelSelectedItems(string arg)
        {
            if (_selectedTicket != null)
                return _selectedTicket.CanCancelSelectedItems();
            return false;
        }

        private void OnCancelItemCommand(string obj)
        {
            _selectedTicket.CancelSelectedItems();
            RefreshSelectedTicket();
        }

        private bool CanGiftSelectedItems(string arg)
        {
            if (_selectedTicket != null && !_selectedTicket.IsLocked && AppServices.IsUserPermittedFor(PermissionNames.GiftItems))
                return _selectedTicket.CanGiftSelectedItems();
            return false;
        }

        private void OnTimer(object state)
        {
            if (AppServices.ActiveAppScreen == AppScreens.TicketList && OpenTickets != null)
                foreach (var openTicketView in OpenTickets)
                {
                    openTicketView.Refresh();
                }
        }

        private void OnShowAllOpenTickets(string obj)
        {
            UpdateOpenTickets(null, "", "");
            SelectedTicketView = OpenTicketListView;
            RaisePropertyChanged("OpenTickets");
        }

        private void OnFilterOpenTicketsExecute(TicketTagFilterViewModel obj)
        {
            UpdateOpenTickets(SelectedDepartment, obj.TagGroup, obj.TagValue);
            RaisePropertyChanged("OpenTickets");
            SelectedTag = null;
        }

        private string _selectedTicketTitle;
        public string SelectedTicketTitle
        {
            get { return _selectedTicketTitle; }
            set { _selectedTicketTitle = value; RaisePropertyChanged("SelectedTicketTitle"); }
        }

        public void UpdateSelectedTicketTitle()
        {
            SelectedTicketTitle = SelectedTicket == null || SelectedTicket.Title.Trim() == "#" ? Resources.NewTicket : SelectedTicket.Title;
        }

        private void OnSelectCustomerExecute(string obj)
        {
            SelectedDepartment.PublishEvent(EventTopicNames.SelectCustomer);
        }

        private bool CanSelectCustomer(string arg)
        {
            return (SelectedTicket == null ||
                (SelectedTicket.Items.Count != 0
                && !SelectedTicket.IsLocked
                && SelectedTicket.Model.CanSubmit));
        }

        private bool CanSelectTable(string arg)
        {
            if (SelectedTicket != null && !SelectedTicket.IsLocked)
                return SelectedTicket.CanChangeTable();
            return SelectedTicket == null;
        }

        private void OnSelectTableExecute(string obj)
        {
            SelectedDepartment.PublishEvent(EventTopicNames.SelectTable);
        }

        public string SelectTableButtonCaption
        {
            get
            {
                if (SelectedTicket != null && !string.IsNullOrEmpty(SelectedTicket.Location))
                    return Resources.ChangeTable_r;
                return Resources.SelectTable_r;
            }
        }

        public string SelectCustomerButtonCaption
        {
            get
            {
                if (SelectedTicket != null && !string.IsNullOrEmpty(SelectedTicket.CustomerName))
                    return Resources.CustomerInfo_r;
                return Resources.SelectCustomer_r;
            }
        }

        private bool CanMakePayment(string arg)
        {
            if (AppServices.MainDataContext.SelectedDepartment != null && AppServices.CurrentTerminal.DepartmentId > 0
                && AppServices.MainDataContext.SelectedDepartment.Id != AppServices.CurrentTerminal.DepartmentId)
                return false;
            return SelectedTicket != null
                && (SelectedTicket.TicketPlainTotalValue > 0 || SelectedTicket.Items.Count > 0)
                && AppServices.IsUserPermittedFor(PermissionNames.MakePayment);
        }

        private void OnMakeCreditCardPaymentExecute(string obj)
        {
            if (CreditCardProcessingService.CanProcessCreditCards)
            {
                var ccpd = new CreditCardProcessingData
                {
                    TenderedAmount = AppServices.MainDataContext.SelectedTicket.GetRemainingAmount(),
                    Ticket = AppServices.MainDataContext.SelectedTicket
                };
                CreditCardProcessingService.Process(ccpd);
                return;
            }
            TicketViewModel.PaySelectedTicket(PaymentType.CreditCard);
            CloseTicket();
        }

        private void OnMakeTicketPaymentExecute(string obj)
        {
            TicketViewModel.PaySelectedTicket(PaymentType.Ticket);
            CloseTicket();
        }

        private void OnMakeCashPaymentExecute(string obj)
        {
            TicketViewModel.PaySelectedTicket(PaymentType.Cash);
            CloseTicket();
        }

        private bool CanMakeFastPayment(string arg)
        {
            if (AppServices.MainDataContext.SelectedDepartment != null && AppServices.CurrentTerminal.DepartmentId > 0
                && AppServices.MainDataContext.SelectedDepartment.Id != AppServices.CurrentTerminal.DepartmentId) return false;
            return SelectedTicket != null && SelectedTicket.TicketRemainingValue > 0 && AppServices.IsUserPermittedFor(PermissionNames.MakeFastPayment);
        }

        private bool CanCloseTicket(string arg)
        {
            return SelectedTicket == null || SelectedTicket.CanCloseTicket();
        }

        private void CloseTicket()
        {
            if (AppServices.MainDataContext.SelectedDepartment.IsFastFood && !CanCloseTicket(""))
            {
                SaveTicketIfNew();
                RefreshVisuals();
            }
            else if (CanCloseTicket(""))
                CloseTicketCommand.Execute("");
        }

        public void DisplayTickets()
        {
            if (SelectedDepartment != null)
            {
                if (SelectedDepartment.IsAlaCarte)
                {
                    SelectedDepartment.PublishEvent(EventTopicNames.SelectTable);
                    StopTimer();
                    RefreshVisuals();
                    return;
                }

                if (SelectedDepartment.IsTakeAway)
                {
                    SelectedDepartment.PublishEvent(EventTopicNames.SelectCustomer);
                    StopTimer();
                    RefreshVisuals();
                    return;
                }

                SelectedTicketView = SelectedDepartment.IsFastFood ? SingleTicketView : OpenTicketListView;

                if (SelectedTicket != null)
                {
                    if (!SelectedDepartment.IsFastFood || SelectedTicket.TicketRemainingValue == 0 || !string.IsNullOrEmpty(SelectedTicket.Location))
                    {
                        SelectedTicket.ClearSelectedItems();
                    }
                }
            }
            RefreshOpenTickets();
            RefreshVisuals();
        }

        public bool IsFastPaymentButtonsVisible
        {
            get
            {
                if (SelectedTicket != null && SelectedTicket.IsPaid) return false;
                if (SelectedTicket != null && !string.IsNullOrEmpty(SelectedTicket.Location)) return false;
                if (SelectedTicket != null && !string.IsNullOrEmpty(SelectedTicket.CustomerName)) return false;
                if (SelectedTicket != null && SelectedDepartment != null && SelectedDepartment.TicketTagGroups.Any(x => SelectedTicket.IsTaggedWith(x.Name))) return false;
                if (SelectedTicket != null && SelectedTicket.TicketRemainingValue == 0) return false;
                if (SelectedTicket != null && AppServices.CurrentTerminal.DepartmentId > 0 && SelectedDepartment != null && SelectedDepartment.Id != AppServices.CurrentTerminal.DepartmentId) return false;
                return SelectedDepartment != null && SelectedDepartment.IsFastFood;
            }
        }

        public bool IsCloseButtonVisible
        {
            get { return !IsFastPaymentButtonsVisible; }
        }

        public void RefreshOpenTickets()
        {
            UpdateOpenTickets(SelectedDepartment, SelectedTag, "");
            SelectedTag = string.Empty;
        }

        public void UpdateOpenTickets(Department department, string selectedTag, string tagFilter)
        {
            StopTimer();

            Expression<Func<Ticket, bool>> prediction;

            if (department != null && string.IsNullOrEmpty(selectedTag))
                prediction = x => !x.IsPaid && x.DepartmentId == department.Id;
            else
                prediction = x => !x.IsPaid;

            var shouldWrap = department != null && !department.IsTakeAway;

            OpenTickets = Dao.Select(x => new OpenTicketViewModel
            {
                Id = x.Id,
                LastOrderDate = x.LastOrderDate,
                TicketNumber = x.TicketNumber,
                LocationName = x.LocationName,
                CustomerName = x.CustomerName,
                RemainingAmount = x.RemainingAmount,
                Date = x.Date,
                WrapText = shouldWrap,
                TicketTag = x.Tag
            }, prediction).OrderBy(x => x.LastOrderDate);

            if (!string.IsNullOrEmpty(selectedTag))
            {
                var tagGroup = AppServices.MainDataContext.SelectedDepartment.TicketTagGroups.SingleOrDefault(
                        x => x.Name == selectedTag);

                if (tagGroup != null)
                {
                    var openTickets = GetOpenTickets(OpenTickets, tagGroup, tagFilter);

                    if (!string.IsNullOrEmpty(tagFilter.Trim()) && tagFilter != "*")
                    {
                        if (openTickets.Count() == 1)
                        {
                            OpenTicketCommand.Execute(openTickets.ElementAt(0).Id);
                        }
                        if (openTickets.Count() == 0)
                        {
                            TicketViewModel.CreateNewTicket();
                            AppServices.MainDataContext.SelectedTicket.SetTagValue(selectedTag, tagFilter);
                            RefreshSelectedTicket();
                            RefreshVisuals();
                        }
                    }

                    if (SelectedTicket == null)
                    {
                        OpenTicketTags = GetOpenTicketTags(OpenTickets, tagGroup, tagFilter);
                        OpenTickets = string.IsNullOrEmpty(tagFilter) && OpenTicketTags.Count() > 0 ? null : openTickets;
                    }
                }
            }
            else
            {
                OpenTicketTags = null;
            }

            SelectedTag = selectedTag;
            StartTimer();
        }

        private static IEnumerable<TicketTagFilterViewModel> GetOpenTicketTags(IEnumerable<OpenTicketViewModel> openTickets, TicketTagGroup tagGroup, string tagFilter)
        {
            var tag = tagGroup.Name.ToLower() + ":";
            var cnt = openTickets.Count(x => string.IsNullOrEmpty(x.TicketTag) || !x.TicketTag.ToLower().Contains(tag));

            var opt = new List<TicketTagFilterViewModel>();

            if (string.IsNullOrEmpty(tagFilter) && tagGroup.TicketTags.Count > 1)
            {
                opt = openTickets.Where(x => !string.IsNullOrEmpty(x.TicketTag))
                    .SelectMany(x => x.TicketTag.Split('\r'))
                    .Where(x => x.ToLower().Contains(tag))
                    .Distinct()
                    .Select(x => x.Split(':')).Select(x => new TicketTagFilterViewModel { TagGroup = x[0], TagValue = x[1] }).OrderBy(x => x.TagValue).ToList();

                var usedTags = opt.Select(x => x.TagValue);

                opt.AddRange(tagGroup.TicketTags.Select(x => x.Name).Where(x => !usedTags.Contains(x)).Select(x => new TicketTagFilterViewModel { TagGroup = tagGroup.Name, ButtonColor = "White", TagValue = x }));

                opt.Sort(new AlphanumComparator());
            }

            if (tagGroup.TicketTags.Count > 1)
            {
                if (string.IsNullOrEmpty(tagFilter))
                    opt.Insert(0, new TicketTagFilterViewModel { TagGroup = tagGroup.Name, TagValue = "*", ButtonColor = "Blue" });
                else
                    opt.Insert(0, new TicketTagFilterViewModel { TagGroup = tagGroup.Name, TagValue = "", ButtonColor = "Green" });
                if (cnt > 0)
                    opt.Insert(0, new TicketTagFilterViewModel { Count = cnt, TagGroup = tagGroup.Name, ButtonColor = "Red", TagValue = " " });
            }

            return opt;
        }

        private static IEnumerable<OpenTicketViewModel> GetOpenTickets(IEnumerable<OpenTicketViewModel> openTickets, TicketTagGroup tagGroup, string tagFilter)
        {
            var tag = tagGroup.Name.ToLower() + ":";
            IEnumerable<OpenTicketViewModel> result = openTickets.ToList();
            if (tagFilter == " ")
            {
                result = result.Where(x =>
                string.IsNullOrEmpty(x.TicketTag) ||
                  !x.TicketTag.ToLower().Contains(tag));
            }
            else
            {
                result = result.Where(x => !string.IsNullOrEmpty(x.TicketTag) && x.TicketTag.ToLower().Contains(tag));
            }

            if (!string.IsNullOrEmpty(tagFilter.Trim()))
            {
                if (tagFilter != "*")
                {
                    result = result.Where(x => x.TicketTag.ToLower().Contains((tag + tagFilter + "\r").ToLower()));
                }
                result.ForEach(x => x.Info = x.TicketTag.Split('\r').Where(y => y.ToLower().StartsWith(tag)).Single().Split(':')[1]);
            }

            return result;
        }

        private void StartTimer()
        {
            if (AppServices.ActiveAppScreen == AppScreens.TicketList)
                _timer.Change(60000, 60000);
        }

        private void StopTimer()
        {
            _timer.Change(Timeout.Infinite, 60000);
        }

        private static void OnMakePaymentExecute(string obj)
        {
            AppServices.MainDataContext.SelectedTicket.PublishEvent(EventTopicNames.MakePayment);
        }

        private void OnCloseTicketExecute(string obj)
        {
            if (SelectedTicketItem != null && !SelectedTicketItem.IsLocked)
            {
                var unselectedItem = AppServices.DataAccessService.GetUnselectedItem(SelectedTicketItem.Model);
                if (unselectedItem != null)
                {
                    InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.SelectionRequired_f, unselectedItem.Name));
                    return;
                }
            }

            if (SelectedTicket.Items.Count > 0 && SelectedTicket.Model.GetRemainingAmount() == 0)
            {
                var message = SelectedTicket.GetPrintError();
                if (!string.IsNullOrEmpty(message))
                {
                    SelectedTicket.ClearSelectedItems();
                    RefreshVisuals();
                    InteractionService.UserIntraction.GiveFeedback(message);
                    return;
                }
            }

            SelectedTicket.ClearSelectedItems();
            var result = AppServices.MainDataContext.CloseTicket();
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                InteractionService.UserIntraction.GiveFeedback(result.ErrorMessage);
            }
            else
            {
                RuleExecutor.NotifyEvent(RuleEventNames.TicketClosed, new { Ticket = _selectedTicket.Model });
            }

            _selectedTicket = null;
            _selectedTicketItems.Clear();

            if (AppServices.CurrentTerminal.AutoLogout)
            {
                AppServices.LogoutUser(false);
                AppServices.CurrentLoggedInUser.PublishEvent(EventTopicNames.UserLoggedOut);
            }
            else
            {
                DisplayTickets();
            }
            AppServices.MessagingService.SendMessage(Messages.TicketRefreshMessage, result.TicketId.ToString());
        }

        private void OnOpenTicketExecute(int? id)
        {
            _selectedTicket = null;
            _selectedTicketItems.Clear();
            AppServices.MainDataContext.OpenTicket(id.GetValueOrDefault(0));
            SelectedTicketView = SingleTicketView;
            RefreshVisuals();
            SelectedTicket.ClearSelectedItems();
            SelectedTicket.PublishEvent(EventTopicNames.SelectedTicketChanged);
        }

        private void RefreshVisuals()
        {
            UpdateSelectedTicketTitle();
            RaisePropertyChanged("SelectedTicket");
            RaisePropertyChanged("CanChangeDepartment");
            RaisePropertyChanged("IsTicketRemainingVisible");
            RaisePropertyChanged("IsTicketPaymentVisible");
            RaisePropertyChanged("IsTicketTotalVisible");
            RaisePropertyChanged("IsTicketDiscountVisible");
            RaisePropertyChanged("IsTicketVatTotalVisible");
            RaisePropertyChanged("IsTicketTaxServiceVisible");
            RaisePropertyChanged("IsTicketRoundingVisible");
            RaisePropertyChanged("IsPlainTotalVisible");
            RaisePropertyChanged("IsFastPaymentButtonsVisible");
            RaisePropertyChanged("IsCloseButtonVisible");
            RaisePropertyChanged("SelectTableButtonCaption");
            RaisePropertyChanged("SelectCustomerButtonCaption");
            RaisePropertyChanged("OpenTicketListViewColumnCount");
            RaisePropertyChanged("IsDepartmentSelectorVisible");
            RaisePropertyChanged("TicketBackground");
            RaisePropertyChanged("IsTableButtonVisible");
            RaisePropertyChanged("IsCustomerButtonVisible");
            RaisePropertyChanged("IsNothingSelectedAndTicketLocked");
            RaisePropertyChanged("IsNothingSelectedAndTicketTagged");
            RaisePropertyChanged("IsTicketSelected");
            RaisePropertyChanged("TicketTagButtons");
            RaisePropertyChanged("PrintJobButtons");

            if (SelectedTicketView == OpenTicketListView)
                RaisePropertyChanged("OpenTickets");
        }

        private void OnAddMenuItemCommandExecute(ScreenMenuItemData obj)
        {
            if (SelectedTicket == null)
            {
                TicketViewModel.CreateNewTicket();
                RefreshVisuals();
            }

            Debug.Assert(SelectedTicket != null);

            if (SelectedTicket.IsLocked && !AppServices.IsUserPermittedFor(PermissionNames.AddItemsToLockedTickets)) return;

            var ti = SelectedTicket.AddNewItem(obj.ScreenMenuItem.MenuItemId, obj.Quantity, obj.ScreenMenuItem.Gift, obj.ScreenMenuItem.DefaultProperties, obj.ScreenMenuItem.ItemPortion);

            if (obj.ScreenMenuItem.AutoSelect && ti != null)
            {
                ti.ItemSelectedCommand.Execute(ti);
            }

            RefreshSelectedTicket();
        }

        private void RefreshSelectedTicket()
        {
            SelectedTicketView = SingleTicketView;

            RaisePropertyChanged("SelectedTicket");
            RaisePropertyChanged("IsTicketRemainingVisible");
            RaisePropertyChanged("IsTicketPaymentVisible");
            RaisePropertyChanged("IsTicketTotalVisible");
            RaisePropertyChanged("IsTicketDiscountVisible");
            RaisePropertyChanged("IsTicketVatTotalVisible");
            RaisePropertyChanged("IsTicketTaxServiceVisible");
            RaisePropertyChanged("IsTicketRoundingVisible");
            RaisePropertyChanged("IsPlainTotalVisible");
            RaisePropertyChanged("CanChangeDepartment");
            RaisePropertyChanged("TicketBackground");
            RaisePropertyChanged("IsTicketSelected");
            RaisePropertyChanged("IsFastPaymentButtonsVisible");
            RaisePropertyChanged("IsCloseButtonVisible");
        }

        public void UpdateSelectedDepartment(int departmentId)
        {
            RaisePropertyChanged("Departments");
            RaisePropertyChanged("PermittedDepartments");
            SelectedDepartment = departmentId > 0
                ? Departments.SingleOrDefault(x => x.Id == departmentId)
                : null;
        }
    }
}
