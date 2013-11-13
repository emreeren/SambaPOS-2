using System;
using System.ComponentModel;
using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.Verifone
{
    public class VfProcessorSettings
    {
        [DisplayName("Port"), Category("SerialPort"), DefaultValue("COM4")]
        public string ComPort { get; set; }
        [DisplayName("ReadTimeout"), Category("SerialPort"), DefaultValue(3000)]
        public int ComReadTimeout { get; set; }
        [DisplayName("BaudRate"), Category("SerialPort"), DefaultValue(9600)]
        public int ComBaudRate { get; set; }
        [DisplayName("DataBit"), Category("SerialPort"), DefaultValue(7)]
        public int DataBit { get; set; }
        [DisplayName("Stopbit"), Category("SerialPort"), DefaultValue(1)]
        public int StopBit { get; set; }
        

        [DisplayName("Gratuity"), Category("Settings"), DefaultValue(0)]
        public decimal GratuityRate { get; set; }
        [DisplayName("Gratuity Template Name"), Category("Settings")]
        public string GratuityTemplateName { get; set; }

       
        [DisplayName("TerminalId"), Category("Settings")]
        public int TerminalId { get; set; }

        [DisplayName("LocalCurrency"), Category("Settings"), DefaultValue("USD")]
        public String LocalCurrency { get; set; }

        [DisplayName("Retry"), Category("Settings"), DefaultValue(3)]
        public int Retry { get; set; }

        
      


        [Browsable(false)]
        public TaxServiceTemplate GratuityService { get; set; }

        public void Load()
        {
            ComPort = AppServices.SettingService.ReadGlobalSetting("COM_PORT").StringValue;
            ComReadTimeout = AppServices.SettingService.ReadGlobalSetting("COM_READ_TIMEOUT").IntegerValue;
            ComBaudRate = AppServices.SettingService.ReadGlobalSetting("COM_BAUD_RATE").IntegerValue;
           

            GratuityRate = AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue;
            GratuityTemplateName = AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue;
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);
            TerminalId = AppServices.SettingService.ReadGlobalSetting("VF_TERMINAL_ID").IntegerValue;
           

            Retry = AppServices.SettingService.ReadGlobalSetting("VF_RETRY").IntegerValue;
            LocalCurrency = AppServices.SettingService.ReadGlobalSetting("CURRENCY").StringValue;

        }

        public void Save()
        {
            
            AppServices.SettingService.ReadGlobalSetting("COM_PORT").StringValue = ComPort;
            AppServices.SettingService.ReadGlobalSetting("COM_READ_TIMEOUT").IntegerValue = ComReadTimeout;
            AppServices.SettingService.ReadGlobalSetting("COM_BAUD_RATE").IntegerValue = ComBaudRate;

            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue = GratuityRate;
            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue = GratuityTemplateName;

            AppServices.SettingService.ReadGlobalSetting("VF_TERMINAL_ID").IntegerValue = TerminalId;
            AppServices.SettingService.ReadGlobalSetting("VF_RETRY").IntegerValue = Retry;
            AppServices.SettingService.ReadGlobalSetting("CURRENCY").StringValue = LocalCurrency;
          



            AppServices.SettingService.SaveChanges();
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);
        }
    }
}