using System;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Practices.Prism.MefExtensions.Modularity;
using Microsoft.Practices.Prism.Regions;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Modules.BasicReports
{
    [ModuleExport(typeof(BasicReportModule))]
    public class BasicReportModule : ModuleBase
    {
        private readonly IRegionManager _regionManager;
        private readonly ICategoryCommand _navigateReportsCommand;
        private readonly BasicReportView _basicReportView;

        [ImportingConstructor]
        public BasicReportModule(IRegionManager regionManager, BasicReportView basicReportView)
        {
            _regionManager = regionManager;
            _basicReportView = basicReportView;
            _navigateReportsCommand = new CategoryCommand<string>(Resources.Reports, Resources.Common, "Images/Ppt.png", OnNavigateReportModule, CanNavigateReportModule) { Order = 80 };

            PermissionRegistry.RegisterPermission(PermissionNames.OpenReports, PermissionCategories.Navigation, Resources.CanDisplayReports);
            PermissionRegistry.RegisterPermission(PermissionNames.ChangeReportDate, PermissionCategories.Report, Resources.CanChangeReportFilter);

            RuleActionTypeRegistry.RegisterActionType("SaveReportToFile", Resources.SaveReportToFile, new { ReportName = "", FileName = "" });
            RuleActionTypeRegistry.RegisterParameterSoruce("ReportName", () => ReportContext.Reports.Select(x => x.Header));

            EventServiceFactory.EventService.GetEvent<GenericEvent<ActionData>>().Subscribe(x =>
            {
                if (x.Value.Action.ActionType == "SaveReportToFile")
                {
                    var reportName = x.Value.GetAsString("ReportName");
                    var fileName = x.Value.GetAsString("FileName");
                    if (!string.IsNullOrEmpty(reportName))
                    {
                        var report = ReportContext.Reports.Where(y => y.Header == reportName).FirstOrDefault();
                        if (report != null)
                        {
                            ReportContext.CurrentWorkPeriod = AppServices.MainDataContext.CurrentWorkPeriod;
                            var document = report.GetReportDocument();
                            try
                            {
                                ReportViewModelBase.SaveAsXps(fileName, document);
                            }
                            catch (Exception e)
                            {
                                AppServices.LogError(e);
                            }
                        }
                    }
                }
            });
        }

        private static bool CanNavigateReportModule(string arg)
        {
            return (AppServices.IsUserPermittedFor(PermissionNames.OpenReports) && AppServices.MainDataContext.CurrentWorkPeriod != null);
        }

        private void OnNavigateReportModule(string obj)
        {
            _regionManager.Regions[RegionNames.MainRegion].Activate(_basicReportView);
            ReportContext.ResetCache();
            ReportContext.CurrentWorkPeriod = AppServices.MainDataContext.CurrentWorkPeriod;
        }

        protected override void OnInitialization()
        {
            _regionManager.RegisterViewWithRegion(RegionNames.MainRegion, typeof(BasicReportView));
        }

        protected override void OnPostInitialization()
        {
            CommonEventPublisher.PublishNavigationCommandEvent(_navigateReportsCommand);
        }
    }
}
