using System;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Settings
{
    public class WorkPeriod : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] LastUpdateTime { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StartDescription { get; set; }
        public string EndDescription { get; set; }
        public decimal CashAmount { get; set; }
        public decimal CreditCardAmount { get; set; }
        public decimal TicketAmount { get; set; }
        public string Description
        {
            get
            {
                var desc = (StartDescription + " - " + EndDescription).Trim(' ', '-');
                return desc;
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Name))
                return Name;

            var desc = !string.IsNullOrEmpty(Description) ? " # " + Description : "";

            if (StartDate == EndDate)
            {
                return StartDate.ToString("dd MMMMM yyyy HH:mm") + desc;
            }

            return string.Format("{0} - {1}{2}", StartDate.ToString("dd MMMMM yyyy HH:mm"), EndDate.ToString("dd MMMMM yyyy HH:mm"), desc).Trim();
        }
    }
}
