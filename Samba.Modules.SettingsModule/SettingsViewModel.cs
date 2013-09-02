﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.ModelBase;
using Samba.Services;

namespace Samba.Modules.SettingsModule
{
    public class SettingsViewModel : VisibleViewModelBase
    {
        public SettingsViewModel()
        {
            SaveSettingsCommand = new CaptionCommand<string>(Resources.Save, OnSaveSettings);
            StartMessagingServerCommand = new CaptionCommand<string>(Resources.StartClientNow, OnStartMessagingServer, CanStartMessagingServer);
            DisplayCommonAppPathCommand = new CaptionCommand<string>(Resources.DisplayAppPath, OnDisplayAppPath);
            DisplayUserAppPathCommand = new CaptionCommand<string>(Resources.DisplayUserPath, OnDisplayUserPath);
            EditCreditCardProcessorSettings = new CaptionCommand<string>("Credit Card Processor Settings", OnEditCreditCardProcessorSettings, CanEditCreditCardProcessorSettings);
        }

        private bool CanEditCreditCardProcessorSettings(string arg)
        {
            return CreditCardProcessingService.GetDefaultProcessor() != null;
        }

        public bool IsCreditCardProcessorEditorVisible { get { return CreditCardProcessorNames.Count() > 0; } }

        private static void OnEditCreditCardProcessorSettings(string obj)
        {
            var defaultProcessor = CreditCardProcessingService.GetDefaultProcessor();
            if (defaultProcessor != null) defaultProcessor.EditSettings();
        }

        public void OnDisplayUserPath(string obj)
        {
            var prc = new System.Diagnostics.Process { StartInfo = { FileName = LocalSettings.UserPath } };
            prc.Start();
        }

        public void OnDisplayAppPath(string obj)
        {
            var prc = new System.Diagnostics.Process { StartInfo = { FileName = LocalSettings.DataPath } };
            prc.Start();
        }

        private static bool CanStartMessagingServer(string arg)
        {
            return AppServices.MessagingService.CanStartMessagingClient();
        }

        private static void OnStartMessagingServer(string obj)
        {
            AppServices.MessagingService.StartMessagingClient();
        }

        private void OnSaveSettings(string obj)
        {
            LocalSettings.SaveSettings();
            ((VisibleViewModelBase)this).PublishEvent(EventTopicNames.ViewClosed);
        }

        public ICaptionCommand SaveSettingsCommand { get; set; }
        public ICaptionCommand StartMessagingServerCommand { get; set; }
        public ICaptionCommand DisplayCommonAppPathCommand { get; set; }
        public ICaptionCommand DisplayUserAppPathCommand { get; set; }
        public ICaptionCommand EditCreditCardProcessorSettings { get; set; }

        public string TerminalName
        {
            get { return LocalSettings.TerminalName; }
            set { LocalSettings.TerminalName = value; }
        }

        public string ConnectionString
        {
            get { return LocalSettings.ConnectionString; }
            set { LocalSettings.ConnectionString = value; }
        }
        public string FailoverConnectString
        {
            get { return LocalSettings.FailoverConnectString; }
            set { LocalSettings.FailoverConnectString = value; }
        }

        public string MessagingServerName
        {
            get { return LocalSettings.MessagingServerName; }
            set { LocalSettings.MessagingServerName = value; }
        }

        public int MessagingServerPort
        {
            get { return LocalSettings.MessagingServerPort; }
            set { LocalSettings.MessagingServerPort = value; }
        }

        public bool StartMessagingClient
        {
            get { return LocalSettings.StartMessagingClient; }
            set { LocalSettings.StartMessagingClient = value; }
        }

        public string Language
        {
            get { return LocalSettings.CurrentLanguage; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    LocalSettings.CurrentLanguage = "";
                }
                else if (LocalSettings.SupportedLanguages.Contains(value))
                {
                    LocalSettings.CurrentLanguage = value;
                }
                else
                {
                    var ci = CultureInfo.GetCultureInfo(value);
                    if (LocalSettings.SupportedLanguages.Contains(ci.TwoLetterISOLanguageName))
                    {
                        LocalSettings.CurrentLanguage = ci.TwoLetterISOLanguageName;
                    }
                }
            }
        }

        public bool OverrideWindowsRegionalSettings
        {
            get { return LocalSettings.OverrideWindowsRegionalSettings; }
            set
            {
                LocalSettings.OverrideWindowsRegionalSettings = value;
                RaisePropertyChanged("OverrideWindowsRegionalSettings");
            }
        }

        private IEnumerable<string> _terminalNames;
        public IEnumerable<string> TerminalNames
        {
            get { return _terminalNames ?? (_terminalNames = Dao.Distinct<Terminal>(x => x.Name)); }
        }

        private IEnumerable<CultureInfo> _supportedLanguages;
        public IEnumerable<CultureInfo> SupportedLanguages
        {
            get
            {
                return _supportedLanguages ?? (_supportedLanguages =
                    LocalSettings.SupportedLanguages.Select(CultureInfo.GetCultureInfo).ToList().OrderBy(x => x.DisplayName));
            }
        }

        public string MajorCurrencyName
        {
            get { return LocalSettings.MajorCurrencyName; }
            set { LocalSettings.MajorCurrencyName = value; }
        }

        public string MinorCurrencyName
        {
            get { return LocalSettings.MinorCurrencyName; }
            set { LocalSettings.MinorCurrencyName = value; }
        }

        public string PluralCurrencySuffix
        {
            get { return LocalSettings.PluralCurrencySuffix; }
            set { LocalSettings.PluralCurrencySuffix = value; }
        }

        public IEnumerable<string> CreditCardProcessorNames { get { return CreditCardProcessingService.GetProcessors().Select(x => x.Name); } }

        public string DefaultCreditCardProcessorName
        {
            get { return LocalSettings.DefaultCreditCardProcessorName; }
            set
            {
                LocalSettings.DefaultCreditCardProcessorName =
                    CreditCardProcessorNames.Contains(value) ? value : "";
                RaisePropertyChanged("DefaultCreditCardProcessorName");
            }
        }

        protected override string GetHeaderInfo()
        {
            return Resources.ProgramSettings;
        }

        public override Type GetViewType()
        {
            return typeof(SettingsView);
        }
    }
}
