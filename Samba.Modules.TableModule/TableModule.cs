using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.TableModule
{
    [ModuleExport(typeof(TableModule))]
    public class TableModule : ModuleBase
    {
        private readonly IRegionManager _regionManager;

        public ICategoryCommand ListTablesCommand { get; set; }
        public ICategoryCommand ListTableScreensCommand { get; set; }
        public ICategoryCommand NavigateTablesCommand { get; set; }

        private TableListViewModel _tableListViewModel;
        private readonly TableSelectorView _tableSelectorView;
        private TableScreenListViewModel _tableScreenListViewModel;

        [ImportingConstructor]
        public TableModule(IRegionManager regionManager, TableSelectorView tableSelectorView)
        {
            _regionManager = regionManager;
            _tableSelectorView = tableSelectorView;
            ListTablesCommand = new CategoryCommand<string>(Resources.TableList, Resources.Tables, OnListTablesExecute) { Order = 30 };
            ListTableScreensCommand = new CategoryCommand<string>(Resources.TableViews, Resources.Tables, OnListTableScreensExecute);
            NavigateTablesCommand = new CategoryCommand<string>(Resources.Tables, Resources.Common, "images/Png.png", OnNavigateTables, CanNavigateTables);
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishDashboardCommandEvent(ListTablesCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListTableScreensCommand);
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(TableSelectorView));
            PermissionRegistry.RegisterPermission(PermissionNames.OpenTables, PermissionCategories.Navigation, Resources.CanOpenTableList);
            PermissionRegistry.RegisterPermission(PermissionNames.ChangeTable, PermissionCategories.Ticket, Resources.CanChangeTable);

            EventServiceFactory.EventService.GetEvent<GenericEvent<Department>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.SelectTable)
                {
                    ActivateTableView();
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<VisibleViewModelBase>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.ViewClosed)
                    {
                        if (x.Value == _tableListViewModel)
                            _tableListViewModel = null;
                        if (x.Value == _tableScreenListViewModel)
                            _tableScreenListViewModel = null;
                    }
                }
                );
        }

        private void OnNavigateTables(string obj)
        {
            ActivateTableView();
            ((TableSelectorViewModel)_tableSelectorView.DataContext).IsNavigated = true;
            ((TableSelectorViewModel)_tableSelectorView.DataContext).RefreshTables();
        }

        private static bool CanNavigateTables(string arg)
        {
            return
                AppServices.IsUserPermittedFor(PermissionNames.OpenTables) &&
                AppServices.MainDataContext.IsCurrentWorkPeriodOpen;
        }

        private void ActivateTableView()
        {
            AppServices.ActiveAppScreen = AppScreens.TableList;
            _regionManager.Regions[RegionNames.MainRegion].Activate(_tableSelectorView);
            ((TableSelectorViewModel)_tableSelectorView.DataContext).IsNavigated = false;
        }

        private void OnListTableScreensExecute(string obj)
        {
            if (_tableScreenListViewModel == null)
                _tableScreenListViewModel = new TableScreenListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_tableScreenListViewModel);
        }

        private void OnListTablesExecute(string obj)
        {
            if (_tableListViewModel == null)
                _tableListViewModel = new TableListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_tableListViewModel);
        }

    }
}
