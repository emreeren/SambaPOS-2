using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.DashboardModule
{
    [ModuleExport(typeof(DashboardModule))]
    public class DashboardModule : ModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly DashboardView _dashboardView;
        private readonly ICategoryCommand _navigateDashboardCommand;

        [ImportingConstructor]
        public DashboardModule(IRegionManager regionManager, DashboardView dashboardView)
        {
            _regionManager = regionManager;
            _dashboardView = dashboardView;
            _navigateDashboardCommand = new CategoryCommand<string>(Resources.Management, Resources.Common, "Images/Tools.png", OnNavigateDashboard, CanNavigateDashboard) { Order = 90 };
            PermissionRegistry.RegisterPermission(PermissionNames.OpenDashboard, PermissionCategories.Navigation, Resources.CanOpenDashboard);
        }

        private static bool CanNavigateDashboard(string arg)
        {
            return AppServices.IsUserPermittedFor(PermissionNames.OpenDashboard);
        }

        private void OnNavigateDashboard(string obj)
        {
            AppServices.ActiveAppScreen = AppScreens.Dashboard;
            _regionManager.Regions[RegionNames.MainRegion].Activate(_dashboardView);
            ((DashboardViewModel) _dashboardView.DataContext).Refresh();
        }

        protected override void OnPreInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(DashboardView));
            _regionManager.RegisterViewWithRegion(RegionNames.UserRegion, typeof(KeyboardButtonView));
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishNavigationCommandEvent(_navigateDashboardCommand);
        }
    }
}
