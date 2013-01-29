using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.ViewModels;
using Samba.Services;

namespace Samba.Presentation.Terminal
{
    public delegate void TicketSelectionEventHandler(int selectedTicketId);

    public class TicketScreenViewModel : ObservableObject
    {
        public event TicketSelectionEventHandler TicketSelectedEvent;

        public DelegateCommand<TerminalOpenTicketView> SelectTicketCommand { get; set; }
        public ICaptionCommand CreateNewTicketCommand { get; set; }

        public IEnumerable<TerminalOpenTicketView> OpenTickets { get; set; }

        private IEnumerable<TicketTagFilterViewModel> _openTicketTags;
        public IEnumerable<TicketTagFilterViewModel> OpenTicketTags
        {
            get { return _openTicketTags; }
            set
            {
                _openTicketTags = value;
                RaisePropertyChanged("OpenTicketTags");
            }
        }

        public TicketScreenViewModel()
        {
            SelectTicketCommand = new DelegateCommand<TerminalOpenTicketView>(OnSelectTicket);
            CreateNewTicketCommand = new CaptionCommand<string>(Resources.NewTicket, OnCreateNewTicket);
        }

        private void OnCreateNewTicket(string obj)
        {
            InvokeTicketSelectedEvent(0);
        }

        public void InvokeTicketSelectedEvent(int ticketId)
        {
            TicketSelectionEventHandler handler = TicketSelectedEvent;
            if (handler != null) handler(ticketId);
        }

        private void OnSelectTicket(TerminalOpenTicketView obj)
        {
            InvokeTicketSelectedEvent(obj.Id);
        }

        public void Refresh()
        {
            UpdateOpenTickets(AppServices.MainDataContext.SelectedDepartment, AppServices.MainDataContext.SelectedDepartment.TerminalDefaultTag);
            RaisePropertyChanged("OpenTickets");
        }

        public void UpdateOpenTickets(Department department, string selectedTag)
        {
            Expression<Func<Ticket, bool>> prediction;

            if (department != null)
                prediction = x => !x.IsPaid && x.DepartmentId == department.Id;
            else
                prediction = x => !x.IsPaid;

            OpenTickets = Dao.Select(x => new TerminalOpenTicketView
            {
                Id = x.Id,
                TicketNumber = x.TicketNumber,
                LocationName = x.LocationName,
                CustomerName = x.CustomerName,
                IsLocked = x.Locked,
                TicketTag = x.Tag
            }, prediction).OrderBy(x => x.Title);

            if (!string.IsNullOrEmpty(selectedTag))
            {
                var tag = selectedTag.ToLower() + ":";
                var cnt = OpenTickets.Count(x => string.IsNullOrEmpty(x.TicketTag) || !x.TicketTag.ToLower().Contains(tag));

                OpenTickets = OpenTickets.Where(x => !string.IsNullOrEmpty(x.TicketTag) && x.TicketTag.ToLower().Contains(tag));

                var opt = OpenTickets.SelectMany(x => x.TicketTag.Split('\r'))
                    .Where(x => x.ToLower().Contains(tag))
                    .Distinct()
                    .Select(x => x.Split(':')).Select(x => new TicketTagFilterViewModel { TagGroup = x[0], TagValue = x[1] }).OrderBy(x => x.TagValue).ToList();

                opt.Insert(0, new TicketTagFilterViewModel { TagGroup = selectedTag, TagValue = "*", ButtonColor = "Blue" });

                if (cnt > 0)
                    opt.Insert(0, new TicketTagFilterViewModel { Count = cnt, TagGroup = selectedTag, ButtonColor = "Red" });

                OpenTicketTags = opt.Count() > 1 ? opt : null;

                OpenTickets.ForEach(x => x.Info = x.TicketTag.Split('\r').Where(y => y.ToLower().StartsWith(tag)).Single().Split(':')[1]);
            }
            else
            {
                OpenTicketTags = null;
            }
        }
    }
}
