using System;
using System.ComponentModel.Composition;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Users;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Modules.NavigationModule
{
    [ModuleExport(typeof(NavigationModule))]
    public class NavigationModule : ModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly NavigationView _navigationView;


        [ImportingConstructor]
        public NavigationModule(IRegionManager regionManager, NavigationView navigationView)
        {
            _regionManager = regionManager;
            _navigationView = navigationView;

            PermissionRegistry.RegisterPermission(PermissionNames.OpenNavigation, PermissionCategories.Navigation, Resources.CanOpenNavigation);

            EventServiceFactory.EventService.GetEvent<GenericEvent<User>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.UserLoggedIn)
                        ActivateNavigation();
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.ActivateNavigation)
                        ActivateNavigation();
                });
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(NavigationView));
        }

        private void ActivateNavigation()
        {
            InteractionService.ClearMouseClickQueue();
            
            if (AppServices.IsUserPermittedFor(PermissionNames.OpenNavigation))
            {
                AppServices.ActiveAppScreen = AppScreens.Navigation;
                _regionManager.Regions[RegionNames.MainRegion].Activate(_navigationView);
                (_navigationView.DataContext as NavigationViewModel).Refresh();
            }
            else if (AppServices.MainDataContext.IsCurrentWorkPeriodOpen)
            {
                EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateTicketView);
            }
            else
            {
                AppServices.CurrentLoggedInUser.PublishEvent(EventTopicNames.UserLoggedOut);
                AppServices.LogoutUser();
            }
        }
    }
}
