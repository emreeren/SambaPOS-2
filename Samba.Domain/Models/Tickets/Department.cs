using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Tickets
{
    public class Department : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] LastUpdateTime { get; set; }
        public string UserString { get { return Name; } }
        public int ScreenMenuId { get; set; }
        public int TerminalScreenMenuId { get; set; }
        public virtual Numerator TicketNumerator { get; set; }
        public virtual Numerator OrderNumerator { get; set; }
        public bool IsFastFood { get; set; }
        public bool IsAlaCarte { get; set; }
        public bool IsTakeAway { get; set; }
        public int TableScreenId { get; set; }
        public int TerminalTableScreenId { get; set; }
        public int OpenTicketViewColumnCount { get; set; }
        public string DefaultTag { get; set; }
        public string TerminalDefaultTag { get; set; }
        [StringLength(10)]
        public string PriceTag { get; set; }

        private IList<TicketTagGroup> _ticketTagGroups;
        public virtual IList<TicketTagGroup> TicketTagGroups
        {
            get { return _ticketTagGroups; }
            set { _ticketTagGroups = value; }
        }

        private IList<TaxServiceTemplate> _taxServiceTemplates;
        public virtual IList<TaxServiceTemplate> TaxServiceTemplates
        {
            get { return _taxServiceTemplates; }
            set { _taxServiceTemplates = value; }
        }

        private static readonly Department _all = new Department { Name = "*" };
        public static Department All { get { return _all; } }

        public Department()
        {
            OpenTicketViewColumnCount = 5;
            _ticketTagGroups = new List<TicketTagGroup>();
            _taxServiceTemplates = new List<TaxServiceTemplate>();
        }
    }
}
