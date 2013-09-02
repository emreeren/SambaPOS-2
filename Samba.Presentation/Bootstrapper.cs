using System;
using System.ComponentModel.Composition.Hosting;
using System.Windows;
using Microsoft.Practices.Prism.Logging;
using Microsoft.Practices.Prism.MefExtensions;
using Microsoft.Practices.ServiceLocation;
using Samba.Infrastructure.Settings;
using Samba.Localization.Engine;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Presentation.ViewModels;
using Samba.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Samba.Presentation
{
    public class Bootstrapper : MefBootstrapper
    {
        private readonly EntLibLoggerAdapter _logger = new EntLibLoggerAdapter();

        protected override DependencyObject CreateShell()
        {
            return Container.GetExportedValue<Shell>();
        }

        protected override void ConfigureAggregateCatalog()
        {
            base.ConfigureAggregateCatalog();
            var path = System.IO.Path.GetDirectoryName(Application.ResourceAssembly.Location);
            if (path != null)
            {
                AggregateCatalog.Catalogs.Add(new DirectoryCatalog(path, "Samba.Login*"));
                AggregateCatalog.Catalogs.Add(new DirectoryCatalog(path, "Samba.Modules*"));
                AggregateCatalog.Catalogs.Add(new DirectoryCatalog(path, "Samba.Presentation*"));
                AggregateCatalog.Catalogs.Add(new DirectoryCatalog(path, "Samba.Services.dll"));
            }
            LocalSettings.AppPath = path;
        }

        protected override ILoggerFacade CreateLogger()
        {
            return _logger;
        }

        protected override void InitializeModules()
        {
            base.InitializeModules();
            var moduleInitializationService = ServiceLocator.Current.GetInstance<IModuleInitializationService>();
            moduleInitializationService.Initialize();
        }

        protected override void InitializeShell()
        {
            LocalizeDictionary.ChangeLanguage(LocalSettings.CurrentLanguage);

            LocalSettings.SetTraceLogPath("app");
            InteractionService.UserIntraction = ServiceLocator.Current.GetInstance<IUserInteraction>();
            InteractionService.UserIntraction.ToggleSplashScreen();

            AppServices.MainDispatcher = Application.Current.Dispatcher;

            AppServices.MessagingService.RegisterMessageListener(new MessageListener());

            if (LocalSettings.StartMessagingClient)
                AppServices.MessagingService.StartMessagingClient();

            GenericRuleRegistator.RegisterOnce();

            PresentationServices.Initialize();

            base.InitializeShell();
            
            try
            {
                var creationService = new DataCreationService();
                creationService.CreateData();
            }
            catch (Exception e)
            {
                bool terminate = true;
                InteractionService.UserIntraction.GiveFeedback("Failed to connect to the database.");
                if (!string.IsNullOrEmpty(LocalSettings.ConnectionString))
                {
                    var currentConnectString = LocalSettings.ConnectionString;
                    var connectionString =
                        InteractionService.UserIntraction.GetStringFromUser(
                        "Connection String",
                        Resources.DatabaseErrorMessage + e.Message,
                        LocalSettings.ConnectionString);

                    var cs = String.Join(" ", connectionString);

                    if (!string.IsNullOrEmpty(cs))
                        LocalSettings.ConnectionString = cs.Trim();

                   
                    LocalSettings.SaveSettings();
                    if (LocalSettings.ConnectionString != currentConnectString)
                    {
                        bool answer = InteractionService.UserIntraction.AskQuestion(
                                       "Do you want to Retry with modified connectstring?");
                       
                        if (answer)
                        {
                            try
                            {
                                var creationService = new DataCreationService();
                                creationService.CreateData();
                                terminate = false;
                            }
                            catch (Exception ex)
                            {
                                InteractionService.UserIntraction.GiveFeedback("Failed to connect to the database again.");
                                AppServices.LogError(e);
                                
                            }
                        }
                    }

                }               
                else
                {

                    AppServices.LogError(e);
                    LocalSettings.ConnectionString = "";
                   
                }
               
                 if ( terminate  && !string.IsNullOrEmpty(LocalSettings.FailoverConnectString))
                {
                    
                        bool answer = InteractionService.UserIntraction.AskQuestion(
                                       "Do you want to connect using failover connect string?");

                        if (answer)
                        {
                            try
                            {
                                LocalSettings.UseFailoverConnectionString = true;
                                var creationService = new DataCreationService();
                                creationService.CreateData();
                                terminate = false;
                            }
                            catch (Exception ex)
                            {
                                InteractionService.UserIntraction.GiveFeedback("Failed to connect to the database using failover connect string.");
                                AppServices.LogError(e);
                            }
                        }
                    
                }

                if (terminate)
                {
                    AppServices.LogError(e, Resources.CurrentErrorLoggedMessage);
                    Environment.Exit(1);
                }
            }

            if (string.IsNullOrEmpty(LocalSettings.MajorCurrencyName))
            {
                LocalSettings.MajorCurrencyName = Resources.Dollar;
                LocalSettings.MinorCurrencyName = Resources.Cent;
                LocalSettings.PluralCurrencySuffix = Resources.PluralCurrencySuffix;
            }

            Application.Current.MainWindow = (Shell)Shell;
            Application.Current.MainWindow.Show();
            InteractionService.UserIntraction.ToggleSplashScreen();
            TriggerService.UpdateCronObjects();

            RuleExecutor.NotifyEvent(RuleEventNames.ApplicationStarted, new { CommandLineArguments = LocalSettings.StartupArguments });
        }
    }
}
