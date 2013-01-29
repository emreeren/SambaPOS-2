using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Samba.Domain.Models.Inventory
{
    public class TransactionItem
    {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public virtual InventoryItem InventoryItem { get; set; }
        public string Unit { get; set; }
        public int Multiplier { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
