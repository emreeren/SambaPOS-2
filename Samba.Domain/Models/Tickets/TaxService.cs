using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Samba.Domain.Models.Tickets
{
    public class TaxService
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int TaxServiceId { get; set; }
        public int TaxServiceType { get; set; }
        public int CalculationType { get; set; }
        public decimal Amount { get; set; }
        public decimal CalculationAmount { get; set; }
    }
}
