using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Mail;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Modules.SettingsModule.WorkPeriods;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.SettingsModule
{
    [ModuleExport(typeof(SettingsModule))]
    public class SettingsModule : ModuleBase
    {
        public ICategoryCommand ListProgramSettingsCommand { get; set; }
        public ICategoryCommand ListTerminalsCommand { get; set; }
        public ICategoryCommand ListPrintJobsCommand { get; set; }
        public ICategoryCommand ListPrintersCommand { get; set; }
        public ICategoryCommand ListPrinterTemplatesCommand { get; set; }
        public ICategoryCommand ListNumeratorsCommand { get; set; }
        public ICategoryCommand ListVoidReasonsCommand { get; set; }
        public ICategoryCommand ListGiftReasonsCommand { get; set; }
        public ICategoryCommand ListMenuItemSettingsCommand { get; set; }
        public ICategoryCommand ListRuleActionsCommand { get; set; }
        public ICategoryCommand ListRulesCommand { get; set; }
        public ICategoryCommand ListTriggersCommand { get; set; }

        public ICategoryCommand ShowBrowser { get; set; }

        private BrowserViewModel _browserViewModel;
        private SettingsViewModel _settingsViewModel;
        private TerminalListViewModel _terminalListViewModel;
        private PrintJobListViewModel _printJobsViewModel;
        private PrinterListViewModel _printerListViewModel;
        private PrinterTemplateCollectionViewModel _printerTemplateCollectionViewModel;
        private NumeratorListViewModel _numeratorListViewModel;
        private VoidReasonListViewModel _voidReasonListViewModel;
        private GiftReasonListViewModel _giftReasonListViewModel;
        private ProgramSettingsViewModel _menuItemSettingsViewModel;
        private RuleActionListViewModel _ruleActionListViewModel;
        private TriggerListViewModel _triggerListViewModel;
        private RuleListViewModel _ruleListViewModel;


        public ICategoryCommand NavigateWorkPeriodsCommand { get; set; }

        private readonly IRegionManager _regionManager;
        private readonly WorkPeriodsView _workPeriodsView;

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(WorkPeriodsView));
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishDashboardCommandEvent(ShowBrowser);
            CommonEventPublisher.PublishDashboardCommandEvent(ListProgramSettingsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListTerminalsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListPrintersCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListPrintJobsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListPrinterTemplatesCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListNumeratorsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListVoidReasonsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListGiftReasonsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListMenuItemSettingsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListRuleActionsCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListRulesCommand);
            CommonEventPublisher.PublishDashboardCommandEvent(ListTriggersCommand);

            CommonEventPublisher.PublishNavigationCommandEvent(NavigateWorkPeriodsCommand);
        }

        [ImportingConstructor]
        public SettingsModule(IRegionManager regionManager, WorkPeriodsView workPeriodsView)
        {
            _regionManager = regionManager;
            _workPeriodsView = workPeriodsView;

            NavigateWorkPeriodsCommand = new CategoryCommand<string>(Resources.DayOperations, Resources.Common, "Images/Run.png", OnNavigateWorkPeriods, CanNavigateWorkPeriods);

            ListProgramSettingsCommand = new CategoryCommand<string>(Resources.LocalSettings, Resources.Settings, OnListProgramSettings);
            ListTerminalsCommand = new CategoryCommand<string>(Resources.Terminals, Resources.Settings, OnListTerminals);
            ListPrintersCommand = new CategoryCommand<string>(Resources.Printers, Resources.Settings, OnListPrinters);
            ListPrintJobsCommand = new CategoryCommand<string>(Resources.PrintJobs, Resources.Settings, OnListPrintJobs);
            ListPrinterTemplatesCommand = new CategoryCommand<string>(Resources.PrinterTemplates, Resources.Settings, OnListPrinterTemplates);
            ListNumeratorsCommand = new CategoryCommand<string>(Resources.Numerators, Resources.Settings, OnListNumerators);
            ListVoidReasonsCommand = new CategoryCommand<string>(Resources.VoidReasons, Resources.Products, OnListVoidReasons);
            ListGiftReasonsCommand = new CategoryCommand<string>(Resources.GiftReasons, Resources.Products, OnListGiftReasons);
            ListMenuItemSettingsCommand = new CategoryCommand<string>(Resources.ProgramSettings, Resources.Settings, OnListMenuItemSettings) { Order = 10 };
            ListRuleActionsCommand = new CategoryCommand<string>(Resources.RuleActions, Resources.Settings, OnListRuleActions);
            ListRulesCommand = new CategoryCommand<string>(Resources.Rules, Resources.Settings, OnListRules);
            ListTriggersCommand = new CategoryCommand<string>(Resources.Triggers, Resources.Settings, OnListTriggers);

            ShowBrowser = new CategoryCommand<string>(Resources.SambaPosWebsite, Resources.SambaNetwork, OnShowBrowser) { Order = 99 };

            PermissionRegistry.RegisterPermission(PermissionNames.OpenWorkPeriods, PermissionCategories.Navigation, Resources.CanStartEndOfDay);
            PermissionRegistry.RegisterPermission(PermissionNames.CloseActiveWorkPeriods, PermissionCategories.Navigation, Resources.ForceClosingActiveWorkPeriod);

            EventServiceFactory.EventService.GetEvent<GenericEvent<VisibleViewModelBase>>().Subscribe(s =>
            {
                if (s.Topic == EventTopicNames.ViewClosed)
                {
                    if (s.Value == _settingsViewModel)
                        _settingsViewModel = null;

                    if (s.Value == _terminalListViewModel)
                        _terminalListViewModel = null;

                    if (s.Value == _printerListViewModel)
                        _printerListViewModel = null;

                    if (s.Value == _printerTemplateCollectionViewModel)
                        _printerTemplateCollectionViewModel = null;

                    if (s.Value == _printJobsViewModel)
                        _printJobsViewModel = null;

                    if (s.Value == _numeratorListViewModel)
                        _numeratorListViewModel = null;

                    if (s.Value == _voidReasonListViewModel)
                        _voidReasonListViewModel = null;

                    if (s.Value == _giftReasonListViewModel)
                        _giftReasonListViewModel = null;

                    if (s.Value == _ruleActionListViewModel)
                        _ruleActionListViewModel = null;

                    if (s.Value == _ruleListViewModel)
                        _ruleListViewModel = null;

                    if (s.Value == _triggerListViewModel)
                        _triggerListViewModel = null;
                }
            });
        }

        private void OnListTriggers(string obj)
        {
            if (_triggerListViewModel == null)
                _triggerListViewModel = new TriggerListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_triggerListViewModel);
        }

        private void OnListRules(string obj)
        {
            if (_ruleListViewModel == null)
                _ruleListViewModel = new RuleListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_ruleListViewModel);
        }

        private void OnListRuleActions(string obj)
        {
            if (_ruleActionListViewModel == null)
                _ruleActionListViewModel = new RuleActionListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_ruleActionListViewModel);
        }

        private static bool CanNavigateWorkPeriods(string arg)
        {
            return AppServices.IsUserPermittedFor(PermissionNames.OpenWorkPeriods);
        }

        private void OnNavigateWorkPeriods(string obj)
        {
            AppServices.ActiveAppScreen = AppScreens.WorkPeriods;
            _regionManager.Regions[RegionNames.MainRegion].Activate(_workPeriodsView);
            ((WorkPeriodsViewModel)_workPeriodsView.DataContext).Refresh();
        }

        private void OnListGiftReasons(string obj)
        {
            if (_giftReasonListViewModel == null)
                _giftReasonListViewModel = new GiftReasonListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_giftReasonListViewModel);
        }

        private void OnListVoidReasons(string obj)
        {
            if (_voidReasonListViewModel == null)
                _voidReasonListViewModel = new VoidReasonListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_voidReasonListViewModel);
        }

        private void OnListNumerators(string obj)
        {
            if (_numeratorListViewModel == null)
                _numeratorListViewModel = new NumeratorListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_numeratorListViewModel);
        }

        private void OnListPrinterTemplates(string obj)
        {
            if (_printerTemplateCollectionViewModel == null)
                _printerTemplateCollectionViewModel = new PrinterTemplateCollectionViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_printerTemplateCollectionViewModel);
        }

        private void OnListPrinters(string obj)
        {
            if (_printerListViewModel == null)
                _printerListViewModel = new PrinterListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_printerListViewModel);
        }

        private void OnListPrintJobs(string obj)
        {
            if (_printJobsViewModel == null)
                _printJobsViewModel = new PrintJobListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_printJobsViewModel);
        }

        private void OnListTerminals(string obj)
        {
            if (_terminalListViewModel == null)
                _terminalListViewModel = new TerminalListViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_terminalListViewModel);
        }

        private void OnListMenuItemSettings(string obj)
        {
            if (_menuItemSettingsViewModel == null)
                _menuItemSettingsViewModel = new ProgramSettingsViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_menuItemSettingsViewModel);
        }

        private void OnListProgramSettings(string obj)
        {
            if (_settingsViewModel == null)
                _settingsViewModel = new SettingsViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_settingsViewModel);
        }

        private void OnShowBrowser(string obj)
        {
            if (_browserViewModel == null)
                _browserViewModel = new BrowserViewModel();
            CommonEventPublisher.PublishViewAddedEvent(_browserViewModel);
            new Uri("http://network.sambapos.com").PublishEvent(EventTopicNames.BrowseUrl);
        }
    }
}
