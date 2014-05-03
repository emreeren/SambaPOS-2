﻿using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Users;
using Samba.Presentation.Common;
using Samba.Presentation.ViewModels;
using Samba.Services;

namespace Samba.Modules.TicketModule
{
    [Export]
    public class TicketEditorViewModel : ObservableObject
    {
        [ImportingConstructor]
        public TicketEditorViewModel()
        {
            TicketListViewModel = new TicketListViewModel();
            MenuItemSelectorViewModel = new MenuItemSelectorViewModel(TicketListViewModel.AddMenuItemCommand);
            PaymentViewModel = new PaymentEditorViewModel();
            SelectedTicketItemsViewModel = new SelectedTicketItemsViewModel();
            //PortionSelectionViewModel = new PortionSelectionViewModel(SelectedTicketItemsViewModel);
            //PortionSelectionView = new PortionSelectionView(PortionSelectionViewModel);
           // PortionSelectionView.Hide();
            TicketExplorerViewModel = new TicketExplorerViewModel();
            DisplayCategoriesScreen();

            EventServiceFactory.EventService.GetEvent<GenericEvent<TicketViewModel>>().Subscribe(OnTicketViewModelEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<Ticket>>().Subscribe(OnTicketEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<User>>().Subscribe(OnUserLoginEvent);
            EventServiceFactory.EventService.GetEvent<GenericEvent<WorkPeriod>>().Subscribe(OnWorkPeriodEvent);

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                 x =>
                 {
                     if (x.Topic == EventTopicNames.ActivateTicketView || x.Topic == EventTopicNames.DisplayTicketView)
                     {
                         DisplayCategoriesScreen();
                     }
                 });
        }

        public CaptionCommand<Ticket> DisplayPaymentScreenCommand { get; set; }

        public MenuItemSelectorViewModel MenuItemSelectorViewModel { get; set; }
        public TicketListViewModel TicketListViewModel { get; set; }
        public PaymentEditorViewModel PaymentViewModel { get; set; }
        public SelectedTicketItemsViewModel SelectedTicketItemsViewModel { get; set; }
        //public PortionSelectionViewModel PortionSelectionViewModel { get; set; }
       // private PortionSelectionView PortionSelectionView { get; set; }
        public TicketExplorerViewModel TicketExplorerViewModel { get; set; }

        private int _selectedView;
        public int SelectedView
        {
            get { return _selectedView; }
            set { _selectedView = value; RaisePropertyChanged("SelectedView"); }
        }

        private int _selectedSubView;
        public int SelectedSubView
        {
            get { return _selectedSubView; }
            set { _selectedSubView = value; RaisePropertyChanged("SelectedSubView"); }
        }

        private void OnUserLoginEvent(EventParameters<User> obj)
        {
            if (obj.Topic == EventTopicNames.UserLoggedOut)
            {
                CloseTicket();
            }
        }

        private void CloseTicket()
        {
            if (AppServices.MainDataContext.SelectedTicket != null)
                AppServices.MainDataContext.CloseTicket();
            TicketListViewModel.SelectedDepartment = null;
        }

        private void OnWorkPeriodEvent(EventParameters<WorkPeriod> obj)
        {
            if (obj.Topic == EventTopicNames.DisplayTicketExplorer)
            {
                DisplayTicketExplorerScreen();
            }
        }

        private void OnTicketViewModelEvent(EventParameters<TicketViewModel> obj)
        {
            if (obj.Topic == EventTopicNames.SelectedItemsChanged)
            {
                if (SelectedTicketItemsViewModel.ShouldDisplay(obj.Value))
                {

                    if(SelectedTicketItemsViewModel.SelectedItemPortions.Count > 0) {
                   
                   // PortionSelectionViewModel.PortionSelectionView = PortionSelectionView;
                   // PortionSelectionView.Owner = Application.Current.MainWindow;
                  //  PortionSelectionView.Topmost = true;
                   // InteractionService.UserIntraction.BlurMainWindow();
                   // PortionSelectionView.ShowDialog();
                    }
                    DisplayTicketDetailsScreen();

                }
                else DisplayCategoriesScreen();
            }
             
            if (obj.Topic == EventTopicNames.SelectVoidReason
                || obj.Topic == EventTopicNames.SelectGiftReason
                || obj.Topic == EventTopicNames.SelectExtraProperty
                || obj.Topic == EventTopicNames.SelectTicketTag
                || obj.Topic == EventTopicNames.EditTicketNote)
            {
                DisplayTicketDetailsScreen();
            }
        }

        private void OnTicketEvent(EventParameters<Ticket> obj)
        {
            switch (obj.Topic)
            {
                case EventTopicNames.MakePayment:
                    AppServices.ActiveAppScreen = AppScreens.Payment;
                    PaymentViewModel.Prepare();
                    DisplayPaymentScreen();
                    break;

                case EventTopicNames.PaymentSubmitted:
                    DisplayCategoriesScreen();
                    break;
            }
        }

        private void DisplayCategoriesScreen()
        {
            DisplayTicketItemsScreen();
        }

        private void DisplayPaymentScreen()
        {
            SelectedView = 1;
        }

        public void DisplayTicketItemsScreen()
        {
            SelectedView = 0;
            SelectedSubView = 0;
            this.PublishEvent(EventTopicNames.FocusTicketScreen);
        }

        public void DisplayTicketDetailsScreen()
        {
            SelectedView = 0;
            SelectedSubView = 1;
            this.PublishEvent(EventTopicNames.FocusTicketScreen);
        }

        public void DisplayTicketExplorerScreen()
        {
            SelectedView = 0;
            SelectedSubView = 2;
            TicketExplorerViewModel.StartDate = AppServices.MainDataContext.CurrentWorkPeriod.StartDate.Date;
            if (!AppServices.IsUserPermittedFor(PermissionNames.DisplayOldTickets))
            {
                TicketExplorerViewModel.StartDate = AppServices.MainDataContext.CurrentWorkPeriod.StartDate;
            }
            TicketExplorerViewModel.EndDate = DateTime.Now;
            TicketExplorerViewModel.Refresh();
        }

        public bool HandleTextInput(string text)
        {
            if ((AppServices.ActiveAppScreen == AppScreens.TicketList || AppServices.ActiveAppScreen == AppScreens.SingleTicket)
                && SelectedView == 0 && SelectedSubView == 0)
                return MenuItemSelectorViewModel.HandleTextInput(text);
            return false;
        }
    }
}
