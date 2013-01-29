using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.MenuModule
{
    [ModuleExport(typeof(MenuModule))]
    public class MenuModule : ModuleBase
    {
        private MenuItemListViewModel _menuItemListViewModel;
        private ScreenMenuListViewModel _screenMenuListViewModel;
        private DepartmentListViewModel _departmentListViewModel;
        private MenuItemPropertyGroupListViewModel _menuItemPropertyGroupListViewModel;
        private PriceListViewModel _priceListViewModel;
        private TicketTagGroupListViewModel _ticketTagGroupListViewModel;
        private MenuItemPriceDefinitionListViewModel _menuItemPriceDefinitionListViewModel;
        private VatTemplateListViewModel _vatTemplateListViewModel;
        private TaxServiceTemplateListViewModel _taxServiceTemplateListViewModel;

        public ICategoryCommand ListMenuItemsCommand { get; set; }
        public ICategoryCommand ListScreenMenusCommand { get; set; }
        public ICategoryCommand ListDepartmentsCommand { get; set; }
        public ICategoryCommand ListMenuItemPropertyGroupsCommand { get; set; }
        public ICategoryCommand ListPricesCommand { get; set; }
        public ICategoryCommand ListTicketTagGroupsCommand { get; set; }
        public ICategoryCommand ListMenuItemPriceDefinitionsCommand { get; set; }
        public ICategoryCommand ListVatTemplatesCommand { get; set; }
        public ICategoryCommand ListTaxServiceTemplates { get; set; }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishDashboardCommandEvent(ListDepartmentsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListMenuItemsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListScreenMenusCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListMenuItemPropertyGroupsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListPricesCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListTicketTagGroupsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListMenuItemPriceDefinitionsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListVatTemplatesCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListTaxServiceTemplates);
        }

        [ImportingConstructor]
        public MenuModule()
        {
            ListDepartmentsCommand = new CategoryCommand<string>(Resources.Departments, Resources.Settings, OnListDepartments);
            ListMenuItemsCommand = new CategoryCommand<string>(Resources.ProductList, Resources.Products, OnListMenuItems);
            ListScreenMenusCommand = new CategoryCommand<string>(Resources.MenuList, Resources.Products, OnListScreenMenus);
            ListMenuItemPropertyGroupsCommand = new CategoryCommand<string>(Resources.ModifierGroups, Resources.Products, OnListMenuItemPropertyGroupsCommand);
            ListPricesCommand = new CategoryCommand<string>(Resources.BatchPriceList, Resources.Products, OnListPrices);
            ListTicketTagGroupsCommand = new CategoryCommand<string>(Resources.TicketTags, Resources.Settings, OnListTicketTags) { Order = 10 };
            ListMenuItemPriceDefinitionsCommand = new CategoryCommand<string>(Resources.PriceDefinitions, Resources.Products, OnListMenuItemPriceDefinitions);
            ListVatTemplatesCommand = new CategoryCommand<string>(Resources.VatTemplates, Resources.Products, OnListVatTemplates);
            ListTaxServiceTemplates = new CategoryCommand<string>(Resources.TaxServiceTemplates, Resources.Products, OnListTaxServiceTemplates);

            PermissionRegistry.RegisterPermission(PermissionNames.ChangeDepartment, PermissionCategories.Department, Resources.CanChangeDepartment);
            foreach (var department in AppServices.MainDataContext.Departments)
            {
                PermissionRegistry.RegisterPermission(PermissionNames.UseDepartment + department.Id, PermissionCategories.Department, department.Name);
            }

            EventServiceFactory.EventService.GetEvent<GenericEvent<VisibleViewModelBase>>().Subscribe(s =>
            {
                if (s.Topic == EventTopicNames.ViewClosed)
                {
                    if (s.Value == _menuItemListViewModel)
                        _menuItemListViewModel = null;

                    if (s.Value == _screenMenuListViewModel)
                        _screenMenuListViewModel = null;

                    if (s.Value == _departmentListViewModel)
                        _departmentListViewModel = null;

                    if (s.Value == _menuItemPropertyGroupListViewModel)
                        _menuItemPropertyGroupListViewModel = null;

                    if (s.Value == _priceListViewModel)
                        _priceListViewModel = null;

                    if (s.Value == _ticketTagGroupListViewModel)
                        _ticketTagGroupListViewModel = null;

                    if (s.Value == _menuItemPriceDefinitionListViewModel)
                        _menuItemPriceDefinitionListViewModel = null;

                    if (s.Value == _vatTemplateListViewModel)
                        _vatTemplateListViewModel = null;

                    if (s.Value == _taxServiceTemplateListViewModel)
                        _taxServiceTemplateListViewModel = null;
                }
            });
        }

        private void OnListTaxServiceTemplates(string obj)
        {
            if (_taxServiceTemplateListViewModel == null)
                _taxServiceTemplateListViewModel = new TaxServiceTemplateListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_taxServiceTemplateListViewModel);
        }

        private void OnListVatTemplates(string obj)
        {
            if (_vatTemplateListViewModel == null)
                _vatTemplateListViewModel = new VatTemplateListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_vatTemplateListViewModel);
        }

        private void OnListMenuItemPriceDefinitions(string obj)
        {
            if (_menuItemPriceDefinitionListViewModel == null)
                _menuItemPriceDefinitionListViewModel = new MenuItemPriceDefinitionListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_menuItemPriceDefinitionListViewModel);
        }

        private void OnListTicketTags(string obj)
        {
            if (_ticketTagGroupListViewModel == null)
                _ticketTagGroupListViewModel = new TicketTagGroupListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_ticketTagGroupListViewModel);
        }

        private void OnListPrices(string obj)
        {
            if (_priceListViewModel == null)
                _priceListViewModel = new PriceListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_priceListViewModel);
        }

        private void OnListMenuItemPropertyGroupsCommand(string obj)
        {
            if (_menuItemPropertyGroupListViewModel == null)
                _menuItemPropertyGroupListViewModel = new MenuItemPropertyGroupListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_menuItemPropertyGroupListViewModel);
        }

        private void OnListDepartments(string obj)
        {
            if (_departmentListViewModel == null)
                _departmentListViewModel = new DepartmentListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_departmentListViewModel);
        }

        public void OnListMenuItems(string value)
        {
            if (_menuItemListViewModel == null)
                _menuItemListViewModel = new MenuItemListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_menuItemListViewModel);
        }

        public void OnListScreenMenus(string value)
        {
            if (_screenMenuListViewModel == null)
                _screenMenuListViewModel = new ScreenMenuListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_screenMenuListViewModel);
        }
    }
}
