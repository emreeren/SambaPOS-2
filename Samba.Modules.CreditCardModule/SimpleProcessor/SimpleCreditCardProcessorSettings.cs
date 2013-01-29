using System.ComponentModel;

namespace Samba.Modules.CreditCardModule.SimpleProcessor
{
    public class SimpleCreditCardProcessorSettings
    {
        [DisplayName("Display Message"), Category("Settings")]
        public string DisplayMessage { get; set; }
    }
}
