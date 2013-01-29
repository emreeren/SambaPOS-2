using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Modularity;
using Samba.Domain.Models.Inventory;
using Samba.Domain.Models.Settings;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.InventoryModule
{
    [ModuleExport(typeof(InventoryModule))]
    public class InventoryModule :ModuleBase
    {
        private InventoryItemListViewModel _inventoryItemListViewModel;
        private RecipeListViewModel _recipeListViewModel;
        private TransactionListViewModel _transactionListViewModel;
        private PeriodicConsumptionListViewModel _periodicConsumptionListViewModel;

        public ICategoryCommand ListInventoryItemsCommand { get; set; }
        public ICategoryCommand ListRecipesCommand { get; set; }
        public ICategoryCommand ListTransactionsCommand { get; set; }
        public ICategoryCommand ListPeriodicConsumptionsCommand { get; set; }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishDashboardCommandEvent(ListInventoryItemsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListRecipesCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListTransactionsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListPeriodicConsumptionsCommand);
        }

        [ImportingConstructor]
        public InventoryModule()
        {
            ListInventoryItemsCommand = new CategoryCommand<string>(Resources.InventoryItems, Resources.Products, OnListInventoryItems) { Order = 26 };
            ListRecipesCommand = new CategoryCommand<string>(Resources.Recipes, Resources.Products, OnListRecipes) { Order = 27 };
            ListTransactionsCommand = new CategoryCommand<string>(Resources.Transactions, Resources.Products, OnListTransactions) { Order = 28 };
            ListPeriodicConsumptionsCommand = new CategoryCommand<string>(Resources.EndOfDayRecords, Resources.Products, OnListPeriodicConsumptions) { Order = 29 };

            EventServiceFactory.EventService.GetEvent<GenericEvent<VisibleViewModelBase>>().Subscribe(s =>
            {
                if (s.Topic == EventTopicNames.ViewClosed)
                {
                    if (s.Value == _inventoryItemListViewModel)
                        _inventoryItemListViewModel = null;
                    if (s.Value == _recipeListViewModel)
                        _recipeListViewModel = null;
                    if (s.Value == _transactionListViewModel)
                        _transactionListViewModel = null;
                    if (s.Value == _periodicConsumptionListViewModel)
                        _periodicConsumptionListViewModel = null;
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<WorkPeriod>>().Subscribe(OnWorkperiodStatusChanged);
        }

        private static void OnWorkperiodStatusChanged(EventParameters<WorkPeriod> obj)
        {
            if (obj.Topic == EventTopicNames.WorkPeriodStatusChanged)
            {
                using (var ws = WorkspaceFactory.Create())
                {
                    if (ws.Count<Recipe>() > 0)
                    {
                        if (!AppServices.MainDataContext.IsCurrentWorkPeriodOpen)
                        {
                            var pc = InventoryService.GetCurrentPeriodicConsumption(ws);
                            if (pc.Id == 0) ws.Add(pc);
                            ws.CommitChanges();
                        }
                        else
                        {
                            if (AppServices.MainDataContext.PreviousWorkPeriod != null)
                            {
                                var pc = InventoryService.GetPreviousPeriodicConsumption(ws);
                                if (pc != null)
                                {
                                    InventoryService.CalculateCost(pc, AppServices.MainDataContext.PreviousWorkPeriod);
                                    ws.CommitChanges();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnListPeriodicConsumptions(string obj)
        {
            if (_periodicConsumptionListViewModel == null)
                _periodicConsumptionListViewModel = new PeriodicConsumptionListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_periodicConsumptionListViewModel);
        }

        private void OnListTransactions(string obj)
        {
            if (_transactionListViewModel == null)
                _transactionListViewModel = new TransactionListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_transactionListViewModel);
        }

        private void OnListRecipes(string obj)
        {
            if (_recipeListViewModel == null)
                _recipeListViewModel = new RecipeListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_recipeListViewModel);
        }

        private void OnListInventoryItems(string obj)
        {
            if (_inventoryItemListViewModel == null)
                _inventoryItemListViewModel = new InventoryItemListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_inventoryItemListViewModel);
        }
    }
}
