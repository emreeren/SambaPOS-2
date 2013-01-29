using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Domain.Models.Tables;
using Samba.Presentation.Common;

namespace Samba.Presentation.ViewModels
{
    public class TerminalOpenTicketView : ObservableObject
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string CustomerName { get; set; }
        public string TicketNumber { get; set; }
        public bool IsLocked { get; set; }
        public string TicketTag { get; set; }
        public string Info { get; set; }
        public string ButtonColor { get { return IsLocked ? "Silver" : "White"; } }

        public string Title
        {
            get
            {
                var result = TicketNumber;
                if (!string.IsNullOrEmpty(Info)) return Info + "-" + result;
                if (!string.IsNullOrEmpty(LocationName)) return LocationName;
                return result;
            }
        }
        public string TitleTextColor { get { return !string.IsNullOrEmpty(Info) || !string.IsNullOrEmpty(LocationName) || !string.IsNullOrEmpty(CustomerName) ? "DarkBlue" : "Maroon"; } }

        public void Refresh()
        {
            RaisePropertyChanged("Title");
        }
    }
}
