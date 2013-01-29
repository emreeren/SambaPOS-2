using System;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Customers
{
    public class Customer : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string GroupCode { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Note { get; set; }
        public DateTime AccountOpeningDate { get; set; }
        public bool InternalAccount { get; set; }

        private static readonly Customer _null = new Customer { Name = "*" };
        public static Customer Null { get { return _null; } }

        public Customer()
        {
            AccountOpeningDate = DateTime.Now;
        }
    }
}
