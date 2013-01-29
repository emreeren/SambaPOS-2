using System;
using System.ComponentModel.Composition;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Modules.CreditCardModule.SimpleProcessor
{
    [Export(typeof(ICreditCardProcessor))]
    public class SimpleCreditCardProcessor : ICreditCardProcessor
    {
        private readonly SimpleCreditCardProcessorSettings _settings;
        private const string DmSettingName = "SCCP_DisplayMessage";

        public SimpleCreditCardProcessor()
        {
            _settings = new SimpleCreditCardProcessorSettings();
            _settings.DisplayMessage = AppServices.SettingService.ReadGlobalSetting(DmSettingName).StringValue;
        }

        public string Name { get { return "Simple Credit Card Processor"; } }

        public void Process(CreditCardProcessingData creditCardProcessingData)
        {
            // get operator response 
            var userEntry = InteractionService.UserIntraction.GetStringFromUser(Name, _settings.DisplayMessage);

            // publish processing result
            var result = new CreditCardProcessingResult()
                             {
                                 Amount = creditCardProcessingData.TenderedAmount,
                                 ProcessType = userEntry.Length > 0 ? ProcessType.Force : ProcessType.Cancel
                             };

            result.PublishEvent(EventTopicNames.PaymentProcessed);
        }

        public bool ForcePayment(int ticketId)
        {
            return false;
        }

        public void EditSettings()
        {
            // displays generic property editor
            InteractionService.UserIntraction.EditProperties(_settings);

            // saves values to global custom setting table
            AppServices.SettingService.ReadGlobalSetting(DmSettingName).StringValue = _settings.DisplayMessage;
            AppServices.SettingService.SaveChanges();
        }
    }
}
