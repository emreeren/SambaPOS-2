using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Samba.Modules.BasicReports.Reports
{
    class PaymentTypeCount
    {
        public int CashPaymentCount { get; set; }
        public int CreditPaymentCount { get; set; }
        public int TicketPaymentCount { get; set; }
        public int AccountPaymentCount { get; set; }

        public PaymentTypeCount(int cashPaymentCount, int creditPaymentCount, int ticketPaymentCount, int accountPaymentCount)
        {
            CashPaymentCount = cashPaymentCount;
            CreditPaymentCount = creditPaymentCount;
            TicketPaymentCount = ticketPaymentCount;
            AccountPaymentCount = accountPaymentCount;
            
        }
    }
}
