using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Modules.TicketModule
{
    [ModuleExport(typeof(TicketModule))]
    public class TicketModule : ModuleBase
    {
        readonly IRegionManager _regionManager;
        private readonly TicketEditorView _ticketEditorView;
        private readonly ICategoryCommand _navigateTicketCommand;

        [ImportingConstructor]
        public TicketModule(IRegionManager regionManager, TicketEditorView ticketEditorView)
        {
            _navigateTicketCommand = new CategoryCommand<string>("POS", Resources.Common, "Images/Network.png", OnNavigateTicketCommand, CanNavigateTicket);
            _regionManager = regionManager;
            _ticketEditorView = ticketEditorView;

            PermissionRegistry.RegisterPermission(PermissionNames.AddItemsToLockedTickets, PermissionCategories.Ticket, Resources.CanReleaseTicketLock);
            PermissionRegistry.RegisterPermission(PermissionNames.RemoveTicketTag, PermissionCategories.Ticket, Resources.CanRemoveTicketTag);
            PermissionRegistry.RegisterPermission(PermissionNames.GiftItems, PermissionCategories.Ticket, Resources.CanGiftItems);
            PermissionRegistry.RegisterPermission(PermissionNames.VoidItems, PermissionCategories.Ticket, Resources.CanVoidItems);
            PermissionRegistry.RegisterPermission(PermissionNames.MoveTicketItems, PermissionCategories.Ticket, Resources.CanMoveTicketLines);
            PermissionRegistry.RegisterPermission(PermissionNames.MergeTickets, PermissionCategories.Ticket, Resources.CanMergeTickets);
            PermissionRegistry.RegisterPermission(PermissionNames.DisplayOldTickets, PermissionCategories.Ticket, Resources.CanDisplayOldTickets);
            PermissionRegistry.RegisterPermission(PermissionNames.MoveUnlockedTicketItems, PermissionCategories.Ticket, Resources.CanMoveUnlockedTicketLines);
            PermissionRegistry.RegisterPermission(PermissionNames.ChangeExtraProperty, PermissionCategories.Ticket, Resources.CanUpdateExtraModifiers);

            PermissionRegistry.RegisterPermission(PermissionNames.MakePayment, PermissionCategories.Payment, Resources.CanGetPayment);
            PermissionRegistry.RegisterPermission(PermissionNames.MakeFastPayment, PermissionCategories.Payment, Resources.CanGetFastPayment);
            PermissionRegistry.RegisterPermission(PermissionNames.MakeDiscount, PermissionCategories.Payment, Resources.CanMakeDiscount);
            PermissionRegistry.RegisterPermission(PermissionNames.RoundPayment, PermissionCategories.Payment, Resources.CanRoundTicketTotal);
            PermissionRegistry.RegisterPermission(PermissionNames.FixPayment, PermissionCategories.Payment, Resources.CanFlattenTicketTotal);

            EventServiceFactory.EventService.GetEvent<GenericEvent<Customer>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.CustomerSelectedForTicket || x.Topic == EventTopicNames.PaymentRequestedForTicket)
                        ActivateTicketEditorView();
                }
                );

            EventServiceFactory.EventService.GetEvent<GenericEvent<EventAggregator>>().Subscribe(
                x =>
                {
                    if (x.Topic == EventTopicNames.ActivateTicketView || x.Topic == EventTopicNames.DisplayTicketView)
                        ActivateTicketEditorView();
                });

            EventServiceFactory.EventService.GetEvent<GenericEvent<WorkPeriod>>().Subscribe(
            x =>
            {
                if (x.Topic == EventTopicNames.WorkPeriodStatusChanged)
                {
                    if (x.Value.StartDate < x.Value.EndDate)
                    {
                        using (var v = WorkspaceFactory.Create())
                        {
                            var items = v.All<ScreenMenuItem>().ToList();
                            using (var vr = WorkspaceFactory.CreateReadOnly())
                            {
                                AppServices.ResetCache();
                                var endDate = AppServices.MainDataContext.LastTwoWorkPeriods.Last().EndDate;
                                var startDate = endDate.AddDays(-7);
                                vr.Queryable<TicketItem>()
                                    .Where(y => y.CreatedDateTime >= startDate && y.CreatedDateTime < endDate)
                                    .GroupBy(y => y.MenuItemId)
                                    .ToList().ForEach(
                                        y => items.Where(z => z.MenuItemId == y.Key).ToList().ForEach(z => z.UsageCount = y.Count()));
                            }
                            v.CommitChanges();
                        }
                    }
                }
            });

        }

        private static bool CanNavigateTicket(string arg)
        {
            return AppServices.MainDataContext.IsCurrentWorkPeriodOpen;
        }

        private static void OnNavigateTicketCommand(string obj)
        {
            EventServiceFactory.EventService.PublishEvent(EventTopicNames.ActivateTicketView);
        }

        private void ActivateTicketEditorView()
        {
            InteractionService.ClearMouseClickQueue();
            _regionManager.Regions[RegionNames.MainRegion].Activate(_ticketEditorView);
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(TicketEditorView));
            _regionManager.RegisterViewWithRegion(RegionNames.UserRegion, typeof(DepartmentButtonView));
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishNavigationCommandEvent(_navigateTicketCommand);
        }
    }
}
