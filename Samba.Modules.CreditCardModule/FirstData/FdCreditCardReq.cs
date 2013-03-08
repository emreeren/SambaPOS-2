namespace Samba.Modules.CreditCardModule.FirstData
{
    class FdCreditCardReq
    {
        public string gateway_id { get; set; }
        public string password { get; set; }
        public string transaction_type { get; set; }
        public string amount { get; set; }
        public string cardholder_name { get; set; }
        public string cc_number { get; set; }
        public string cc_expiry { get; set; }
       
    }
}