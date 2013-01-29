using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.Practices.EnterpriseLibrary.Logging.Configuration;

namespace Samba.Infrastructure.Settings
{
    public class SettingsObject
    {
        public string MajorCurrencyName { get; set; }
        public string MinorCurrencyName { get; set; }
        public string PluralCurrencySuffix { get; set; }
        public int MessagingServerPort { get; set; }
        public string MessagingServerName { get; set; }
        public string TerminalName { get; set; }
        public string ConnectionString { get; set; }
        public bool StartMessagingClient { get; set; }
        public string LogoPath { get; set; }
        public string DefaultHtmlReportHeader { get; set; }
        public string CurrentLanguage { get; set; }
        public bool OverrideLanguage { get; set; }
        public bool OverrideWindowsRegionalSettings { get; set; }
        public string DefaultCreditCardProcessorName { get; set; }
        public SerializableDictionary<string, string> CustomSettings { get; set; }

        public SettingsObject()
        {
            CustomSettings = new SerializableDictionary<string, string>();
            MessagingServerPort = 8080;
            ConnectionString = "";
            DefaultHtmlReportHeader =
                @"
<style type='text/css'> 
html
{
  font-family: 'Courier New', monospace;
} 
</style>";
        }

        public void SetCustomValue(string settingName, string settingValue)
        {
            if (!CustomSettings.ContainsKey(settingName))
                CustomSettings.Add(settingName, settingValue);
            else
                CustomSettings[settingName] = settingValue;
            if (string.IsNullOrEmpty(settingValue))
                CustomSettings.Remove(settingName);
        }

        public string GetCustomValue(string settingName)
        {
            return CustomSettings.ContainsKey(settingName) ? CustomSettings[settingName] : "";
        }
    }

    public static class LocalSettings
    {
        private static SettingsObject _settingsObject;

        public static int Decimals { get { return 2; } }

        public static int MessagingServerPort
        {
            get { return _settingsObject.MessagingServerPort; }
            set { _settingsObject.MessagingServerPort = value; }
        }

        public static string MessagingServerName
        {
            get { return _settingsObject.MessagingServerName; }
            set { _settingsObject.MessagingServerName = value; }
        }

        public static string TerminalName
        {
            get { return _settingsObject.TerminalName; }
            set { _settingsObject.TerminalName = value; }
        }

        public static string ConnectionString
        {
            get { return _settingsObject.ConnectionString; }
            set { _settingsObject.ConnectionString = value; }
        }

        public static bool StartMessagingClient
        {
            get { return _settingsObject.StartMessagingClient; }
            set { _settingsObject.StartMessagingClient = value; }
        }

        public static string LogoPath
        {
            get { return _settingsObject.LogoPath; }
            set { _settingsObject.LogoPath = value; }
        }

        public static string DefaultHtmlReportHeader
        {
            get { return _settingsObject.DefaultHtmlReportHeader; }
            set { _settingsObject.DefaultHtmlReportHeader = value; }
        }

        public static string MajorCurrencyName
        {
            get { return _settingsObject.MajorCurrencyName; }
            set { _settingsObject.MajorCurrencyName = value; }
        }

        public static string MinorCurrencyName
        {
            get { return _settingsObject.MinorCurrencyName; }
            set { _settingsObject.MinorCurrencyName = value; }
        }

        public static string PluralCurrencySuffix
        {
            get { return _settingsObject.PluralCurrencySuffix; }
            set { _settingsObject.PluralCurrencySuffix = value; }
        }

        public static string DefaultCreditCardProcessorName
        {
            get { return _settingsObject.DefaultCreditCardProcessorName; }
            set { _settingsObject.DefaultCreditCardProcessorName = value; }
        }

        private static CultureInfo _cultureInfo;
        public static string CurrentLanguage
        {
            get { return _settingsObject.CurrentLanguage; }
            set
            {
                _cultureInfo = CultureInfo.GetCultureInfo(value);
                if (_settingsObject.CurrentLanguage != value)
                {
                    _settingsObject.CurrentLanguage = value;
                    SaveSettings();
                }
                UpdateThreadLanguage();
            }
        }

        public static bool OverrideWindowsRegionalSettings
        {
            get { return _settingsObject.OverrideWindowsRegionalSettings; }
            set { _settingsObject.OverrideWindowsRegionalSettings = value; }
        }

        public static string AppPath { get; set; }
        public static string DocumentPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\SambaPOS2"; } }

        public static string DataPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\Ozgu Tech\\SambaPOS2"; } }
        public static string UserPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Ozgu Tech\\SambaPOS2"; } }

        public static string CommonSettingsFileName { get { return DataPath + "\\SambaSettings.txt"; } }
        public static string UserSettingsFileName { get { return UserPath + "\\SambaSettings.txt"; } }

        public static string SettingsFileName { get { return File.Exists(UserSettingsFileName) ? UserSettingsFileName : CommonSettingsFileName; } }

        public static string DefaultCurrencyFormat { get; set; }
        public static string CurrencySymbol { get { return CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol; } }

        public static int DbVersion { get { return 20; } }
        public static string AppVersion { get { return "2.99"; } }
        public static IList<string> SupportedLanguages { get { return new[] { "en", "de", "fr", "es", "cs", "ru", "hr", "tr", "pt-BR", "it", "ro", "sq", "zh-CN", "nl-NL", "id", "el" }; } }

        public static long CurrentDbVersion { get; set; }

        public static string DatabaseLabel
        {
            get
            {
                if (ConnectionString.ToLower().Contains(".sdf")) return "CE";
                if (ConnectionString.ToLower().Contains("data source")) return "SQ";
                if (ConnectionString.ToLower().StartsWith("mongodb://")) return "MG";
                return "TX";
            }
        }

        public static string StartupArguments { get; set; }

        public static void SaveSettings()
        {
            try
            {
                var serializer = new XmlSerializer(_settingsObject.GetType());
                var writer = new XmlTextWriter(SettingsFileName, null);
                try
                {
                    serializer.Serialize(writer, _settingsObject);
                }
                finally
                {
                    writer.Close();
                }
            }
            catch (UnauthorizedAccessException)
            {
                if (!File.Exists(UserSettingsFileName))
                {
                    File.Create(UserSettingsFileName).Close();
                    SaveSettings();
                }
            }
        }

        public static void LoadSettings()
        {
            _settingsObject = new SettingsObject();
            string fileName = SettingsFileName;
            if (File.Exists(fileName))
            {
                var serializer = new XmlSerializer(_settingsObject.GetType());
                var reader = new XmlTextReader(fileName);
                try
                {
                    _settingsObject = serializer.Deserialize(reader) as SettingsObject;
                }
                finally
                {
                    reader.Close();
                }
            }
        }

        static LocalSettings()
        {
            if (!Directory.Exists(DocumentPath))
                Directory.CreateDirectory(DocumentPath);
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
            if (!Directory.Exists(UserPath))
                Directory.CreateDirectory(UserPath);
            LoadSettings();
        }

        public static void UpdateThreadLanguage()
        {
            if (_cultureInfo != null)
            {
                if (OverrideWindowsRegionalSettings)
                    Thread.CurrentThread.CurrentCulture = _cultureInfo;
                Thread.CurrentThread.CurrentUICulture = _cultureInfo;
            }
        }

        public static void UpdateSetting(string settingName, string settingValue)
        {
            _settingsObject.SetCustomValue(settingName, settingValue);
            SaveSettings();
        }

        public static string ReadSetting(string settingName)
        {
            return _settingsObject.GetCustomValue(settingName);
        }

        public static void SetTraceLogPath(string prefix)
        {
            var logFilePath = DocumentPath + "\\" + prefix + "_trace.log";
            var objConfigPath = new ConfigurationFileMap();
            var appPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;

            try
            {
                objConfigPath.MachineConfigFilename = appPath;
                var entLibConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var loggingSettings = (LoggingSettings)entLibConfig.GetSection(LoggingSettings.SectionName);
                var traceListenerData = loggingSettings.TraceListeners.Get("Flat File Trace Listener");
                var objFlatFileTraceListenerData = traceListenerData as FlatFileTraceListenerData;
                if (objFlatFileTraceListenerData != null) objFlatFileTraceListenerData.FileName = logFilePath;
                entLibConfig.Save();
            }
            catch (Exception)
            {

            }
        }

        public static string GetSqlServerConnectionString()
        {
            var cs = ConnectionString;
            if (!cs.Trim().EndsWith(";"))
                cs += ";";
            if (!cs.ToLower().Contains("multipleactiveresultsets"))
                cs += " MultipleActiveResultSets=True;";
            if (!cs.ToLower(CultureInfo.InvariantCulture).Contains("user id") && (!cs.ToLower(CultureInfo.InvariantCulture).Contains("integrated security")))
                cs += " Integrated Security=True;";
            if (cs.ToLower(CultureInfo.InvariantCulture).Contains("user id") && !cs.ToLower().Contains("persist security info"))
                cs += " Persist Security Info=True;";
            return cs;
        }
    }
}
