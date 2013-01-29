using System.ComponentModel;
using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.ExternalProcessor
{
    public class ExternalProcessorSettings
    {
        [DisplayName("Gratuity"), Category("Settings")]
        public decimal GratuityRate { get; set; }
        [DisplayName("Gratuity Template Name"), Category("Settings")]
        public string GratuityTemplateName { get; set; }

        [Browsable(false)]
        public TaxServiceTemplate GratuityService { get; set; }

        public void Load()
        {
            GratuityRate = AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue;
            GratuityTemplateName = AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue;
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);
        }

        public void Save()
        {
            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYRATE").DecimalValue = GratuityRate;
            AppServices.SettingService.ReadGlobalSetting("EXCCS_GRATUITYTEMPLATE").StringValue = GratuityTemplateName;
            AppServices.SettingService.SaveChanges();
            GratuityService = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Name == GratuityTemplateName);
        }
    }
}
