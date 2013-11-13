using System.Security;

namespace Samba.Modules.CreditCardModule.Verifone
{
    internal class PreauthData
    {
        public SecureString SwipeData { get; set; }
        public string CardHolderName { get; set; }
        public string CardExpireDate { get; set; }
        public decimal TenderedAmount { get; set; }
        public decimal Gratuity { get; set; }
        public string MerchantAuthCode { get; set; }
      
    }
}