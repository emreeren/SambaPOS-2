namespace Samba.Modules.CreditCardModule.FirstData
{
    public class FdCreditCardResp
    {
        public string transaction_type { get; set; }
        public string logon_message { get; set; }
        public bool transaction_error { get; set; }
        public bool transaction_approved { get; set; }
        public string exact_resp_code { get; set; }
        public string exact_message { get; set; }
        public string bank_resp_code { get; set; }
        public string bank_message { get; set; }
        public string bank_resp_code_2 { get; set; }
        public int    transaction_tag { get; set; }
        public string authorization_num { get; set; }
        public string sequence_no { get; set; }
        public string avs { get; set; }
        public string cvv2 { get; set; }
        public string retrieval_ref_no { get; set; }
        public string merchant_name { get; set; }
        public string merchant_address { get; set; }
        public string merchant_city { get; set; }
        public string merchant_province { get; set; }
        public string merchant_country { get; set; }
        public string merchant_postal { get; set; }
        public string merchant_url { get; set; }
        public string ctr { get; set; }
        public string current_balance { get; set; }
        public string previous_balance { get; set; }
        public string cc_expiry { get; set; }
        public string cc_number { get; set; }
        public string credit_card_type { get; set; }
        public string amount { get; set; }
        public string cardholder_name { get; set; }
    }
}