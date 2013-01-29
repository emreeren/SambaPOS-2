using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Settings;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Tickets
{
    public class TicketTagGroup : IEntity, IOrderable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }

        public virtual Numerator Numerator { get; set; }
        private IList<TicketTag> _ticketTags;

        public int Action { get; set; }
        public bool FreeTagging { get; set; }
        public bool SaveFreeTags { get; set; }
        public bool ExcludeInReports { get; set; }
        public string ButtonColorWhenTagSelected { get; set; }
        public string ButtonColorWhenNoTagSelected { get; set; }
        public bool ActiveOnPosClient { get; set; }
        public bool ActiveOnTerminalClient { get; set; }
        public bool ForceValue { get; set; }
        public bool NumericTags { get; set; }
        public bool PriceTags { get; set; }

        public string UserString
        {
            get { return Name; }
        }

        public virtual IList<TicketTag> TicketTags
        {
            get { return _ticketTags; }
            set { _ticketTags = value; }
        }

        public TicketTagGroup()
        {
            _ticketTags = new List<TicketTag>();
            ButtonColorWhenNoTagSelected = "Gainsboro";
            ButtonColorWhenTagSelected = "Gainsboro";
            ActiveOnPosClient = true;
            SaveFreeTags = true;
        }
    }
}
