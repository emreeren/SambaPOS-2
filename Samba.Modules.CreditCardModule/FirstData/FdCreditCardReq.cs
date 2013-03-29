namespace Samba.Modules.CreditCardModule.FirstData
{
    /// <summary>
    /// Fd Credit Card Request
    /// </summary>
    public class FdCreditCardReq
    {
        public FdCreditCardReq(FdTransactionType txType)
        {
            transaction_type = ((int)txType).ToString("00");
            partial_redemption = true;
        }
        public FdCreditCardReq(string txType)
        {
            transaction_type = txType;
            partial_redemption = true;
        }
        public string gateway_id { get; set; }
        public string password { get; set; }
        public string transaction_type { get; set; }
        public string amount { get; set; }
        public string cc_number { get; set; }
        public string cc_expiry { get; set; }
        public string cardholder_name { get; set; }
        public string reference_no { get; set; }
        public bool partial_redemption { get; set; }
        public string credit_card_type { get; set; }

        
            public enum FdTransactionType
            {
                Purchase = 0,
                PreAuthorization = 1,
                PreAuthorizationCompletion = 2,
                ForcedPost = 3,
                Refund = 4,
                PreAuthorizationOnly = 5,
                PayPalOrder = 7,
                Void = 13,
                TaggedPreAuthorizationCompletion = 32,
                TaggedVoid = 33,
                CashOut = 83,
                Activation = 85,
                BalanceInquiry = 86,
                Reload = 88,
                Deactivation = 89
           
            }


        }


    }