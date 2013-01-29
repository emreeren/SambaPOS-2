using System;
using System.Collections.Generic;
using System.Linq;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Services;

namespace Samba.Presentation.ViewModels
{
    public class CustomerViewModel : ObservableObject
    {
        public Customer Model { get; set; }

        public CustomerViewModel(Customer model)
        {
            Model = model;
        }

        public int Id { get { return Model.Id; } }
        public string Name { get { return Model.Name; } set { Model.Name = value.Trim(); RaisePropertyChanged("Name"); } }
        public string PhoneNumber { get { return Model.PhoneNumber; } set { Model.PhoneNumber = !string.IsNullOrEmpty(value) ? value.Trim() : ""; RaisePropertyChanged("PhoneNumber"); } }
        public string GroupCode { get { return Model.GroupCode; } set { Model.GroupCode = value; RaisePropertyChanged("GroupCode"); } }
        public string Address { get { return Model.Address; } set { Model.Address = value; RaisePropertyChanged("Address"); } }
        public string Note { get { return Model.Note; } set { Model.Note = value; RaisePropertyChanged("Note"); } }
        public string PhoneNumberText { get { return PhoneNumber != null ? FormatAsPhoneNumber(PhoneNumber) : PhoneNumber; } }
        public DateTime AccountOpeningDate { get { return Model.AccountOpeningDate; } set { Model.AccountOpeningDate = value; } }
        
        private IEnumerable<string> _groupCodes;
        public IEnumerable<string> GroupCodes { get { return _groupCodes ?? (_groupCodes = Dao.Distinct<Customer>(x => x.GroupCode)); } }

        public decimal AccountBalance { get; private set; }
        public Ticket LastTicket { get; private set; }
        public bool IsNotNew { get { return Model.Id > 0; } }

        private static string FormatAsPhoneNumber(string phoneNumber)
        {
            var phoneNumberInputMask = AppServices.SettingService.PhoneNumberInputMask;
            if (phoneNumber.Length == phoneNumberInputMask.Count(x => x == '#'))
            {
                decimal d;
                decimal.TryParse(phoneNumber, out d);
                return d.ToString(phoneNumberInputMask);
            }
            return phoneNumber;
        }

        public void UpdateDetailedInfo()
        {
            LastTicket = Dao.Last<Ticket>(x => x.CustomerId == Model.Id, x => x.TicketItems);
            TotalTicketAmount = Dao.Sum<Ticket>(x => x.TotalAmount, x => x.CustomerId == Model.Id);
            AccountBalance = CashService.GetAccountBalance(Model.Id);
        }

        public IEnumerable<TicketItemViewModel> LastTicketLines { get { return LastTicket != null ? LastTicket.TicketItems.Where(x => !x.Gifted || !x.Voided).Select(x => new TicketItemViewModel(x)) : null; } }
        public decimal TicketTotal { get { return LastTicket != null ? LastTicket.GetSum() : 0; } }
        public string LastTicketStateString { get { return LastTicket != null ? (LastTicket.IsPaid ? Resources.Paid : Resources.Open) : ""; } }
        public decimal TotalTicketAmount { get; private set; }
    }
}
