using System.ComponentModel;
using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.FirstData
{
    public class FdProcessorSettings
    {
        [DisplayName("Gratuity"), Category("Settings"), DefaultValue(0.0)]
        public decimal GratuityRate { get; set; }
        [DisplayName("Gratuity Template Name"), Category("Settings")]
        public string GratuityTemplateName { get; set; }
        //First Data
        [DisplayName("GatewayId"), Category("Settings")]
        public string GatewayId { get; set; }
        [DisplayName("Password"), Category("Settings")]
        public string Password { get; set; }
        [DisplayName("GatewayUri Key"), Category("Settings")]
        public string GatewayUri { get; set; }
        [DisplayName("Transaction Key"), Category("Settings"), PasswordPropertyText(true)]
        public string HmacKey { get; set; }
        [DisplayName("x_Login"), Category("Settings")]
        public string LoginId { get; set; }
        [DisplayName("Currency Code"), Category("Settings"), DefaultValue("USD")]
        public string CurrencyCode { get; set; }
        


        [Browsable(false)]
        public TaxServiceTemplate GratuityService { get; set; }

        public void Load()
        {
            GratuityRate = AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue;
            GratuityTemplateName = AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue;
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);

            GatewayId = AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_ID").StringValue;
            Password = AppServices.SettingService.ReadGlobalSetting("FD_PASSWORD").StringValue;
            GatewayUri = AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_URI").StringValue;
        }

        public void Save()
        {
            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue = GratuityRate;
            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue = GratuityTemplateName;

            //First Data
            AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_ID").StringValue = GatewayId;
            AppServices.SettingService.ReadGlobalSetting("FD_PASSWORD").StringValue = Password;
            AppServices.SettingService.ReadGlobalSetting("FD_GATEWAY_URI").StringValue = GatewayUri;

            AppServices.SettingService.SaveChanges();
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);
        }
    }
}
