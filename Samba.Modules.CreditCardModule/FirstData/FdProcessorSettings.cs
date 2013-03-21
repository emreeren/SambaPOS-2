using System.ComponentModel;
using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.FirstData
{
    public class FdProcessorSettings
    {
        [DisplayName("Port"), Category("SerialPort"), DefaultValue("COM3")]
        public string ComPort { get; set; }
        [DisplayName("ReadTimeout"), Category("SerialPort"), DefaultValue(5000)]
        public int ComReadTimeout { get; set; }
        [DisplayName("BaudRate"), Category("SerialPort"), DefaultValue(19200)]
        public int ComBaudRate { get; set; }
       

        [DisplayName("Gratuity"), Category("Settings"), DefaultValue(0.0)]
        public decimal GratuityRate { get; set; }
        [DisplayName("Gratuity Template Name"), Category("Settings")]
        public string GratuityTemplateName { get; set; }
        [DisplayName("Merge Credit Card Receipt"), Category("Settings"), DefaultValue(true)]
        public bool MergeCreditCardReceipt { get; set; }
        [DisplayName("Sign Required Amount"), Category("Settings"), DefaultValue(20.00)]
        public decimal SignRequiredAmount { get; set; }
        //First Data
        [DisplayName("GatewayUri"), Category("FD")]
        public string GatewayUri { get; set; }
        [DisplayName("Currency Code"), Category("FD"), DefaultValue("USD")]
        public string CurrencyCode { get; set; }
        //FD API V11
        [DisplayName("GatewayId"), Category("FDV11")]
        public string GatewayId { get; set; }
        [DisplayName("Password"), Category("FDV11")]
        public string Password { get; set; }
        /*
        //API V12
        [DisplayName("Transaction Key"), Category("FDV12"), PasswordPropertyText(true)]
        public string HmacKey { get; set; }
        [DisplayName("x_Login"), Category("FDV12")]
        public string LoginId { get; set; }
        */
        


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
            SignRequiredAmount = AppServices.SettingService.ReadGlobalSetting("CC_SIGNREQUIRED)AMOUNT").DecimalValue;
            MergeCreditCardReceipt =
                AppServices.SettingService.ReadGlobalSetting("CC_MERGE_CREDITCARD_RECEIPT").BoolValue;

            GatewayId = AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_ID").StringValue;
            Password = AppServices.SettingService.ReadGlobalSetting("FD_PASSWORD").StringValue;
            GatewayUri = AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_URI").StringValue;



        }

        public void Save()
        {
            
            AppServices.SettingService.ReadGlobalSetting("COM_PORT").StringValue = ComPort;
            AppServices.SettingService.ReadGlobalSetting("COM_READ_TIMEOUT").IntegerValue = ComReadTimeout;
            AppServices.SettingService.ReadGlobalSetting("COM_BAUD_RATE").IntegerValue = ComBaudRate;

            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue = GratuityRate;
            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue = GratuityTemplateName;

            AppServices.SettingService.ReadGlobalSetting("CC_SIGNREQUIRED)AMOUNT").DecimalValue = SignRequiredAmount;
            AppServices.SettingService.ReadGlobalSetting("CC_MERGE_CREDITCARD_RECEIPT").BoolValue =
                MergeCreditCardReceipt;

            //First Data
            AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_ID").StringValue = GatewayId;
            AppServices.SettingService.ReadGlobalSetting("FD_PASSWORD").StringValue = Password;
            AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_URI").StringValue = GatewayUri;

            AppServices.SettingService.SaveChanges();
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);
        }
    }
}
