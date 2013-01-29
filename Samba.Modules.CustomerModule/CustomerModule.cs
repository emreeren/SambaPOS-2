using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Interaction;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.CustomerModule
{
    [ModuleExport(typeof(CustomerModule))]
    public class CustomerModule : ModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly CustomerSelectorView _customerSelectorView;
        private CustomerListViewModel _customerListViewModel;
        public ICategoryCommand ListCustomersCommand { get; set; }

        [ImportingConstructor]
        public CustomerModule(IRegionManager regionManager, CustomerSelectorView customerSelectorView)
        {
            _regionManager = regionManager;
            _customerSelectorView = customerSelectorView;
            ListCustomersCommand = new CategoryCommand<string>(Resources.CustomerList, Resources.Customers, OnCustomerListExecute) { Order = 40 };
            PermissionRegistry.RegisterPermission(PermissionNames.MakeAccountTransaction, PermissionCategories.Cash, Resources.CanMakeAccountTransaction);
            PermissionRegistry.RegisterPermission(PermissionNames.CreditOrDeptAccount, PermissionCategories.Cash, Resources.CanMakeCreditOrDeptTransaction);
        }

        private void OnCustomerListExecute(string obj)
        {
            if (_customerListViewModel == null)
                _customerListViewModel = new CustomerListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_customerListViewModel);
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(CustomerSelectorView));

            EventServiceFactory.EventService.GetEvent<GenericEvent<VisibleViewModelBase>>().Subscribe(s =>
            {
                if (s.Topic == EventTopicNames.ViewClosed)
                {
                    if (s.Value == _customerListViewModel)
                        _customerListViewModel = null;
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<Department>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.SelectCustomer)
                {
                    ActivateCustomerView();
                    ((CustomerSelectorViewModel)_customerSelectorView.DataContext).RefreshSelectedCustomer();
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.ActivateCustomerView)
                {
                    ActivateCustomerView();
                    ((CustomerSelectorViewModel)_customerSelectorView.DataContext).RefreshSelectedCustomer();
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<Customer>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.ActivateCustomerAccount)
                {
                    ActivateCustomerView();
                    ((CustomerSelectorViewModel)_customerSelectorView.DataContext).DisplayCustomerAccount(x.Value);
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<PopupData>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.PopupClicked && x.Value.EventMessage == "SelectCustomer")
                    {
                        ActivateCustomerView();
                        ((CustomerSelectorViewModel)_customerSelectorView.DataContext).SearchCustomer(x.Value.DataObject as string);
                    }
                }
                );
        }

        private void ActivateCustomerView()
        {
            AppServices.ActiveAppScreen = AppScreens.CustomerList;
            _regionManager.Regions[RegionNames.MainRegion].Activate(_customerSelectorView);
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishDashboardCommandEvent(ListCustomersCommand);
        }
    }
}
