using System;
using Samba.Domain.Foundation;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Presentation.Common;

namespace Samba.Presentation.ViewModels
{
    public class MenuItemPropertyViewModel : ObservableObject
    {
        public MenuItemProperty Model { get; set; }

        public MenuItemPropertyViewModel(MenuItemProperty model)
        {
            Model = model;
        }

        public string Name { get { return Model.Name; } set { Model.Name = value; } }
        public Price Price { get { return Model.Price; } set { Model.Price = value; } }
        public string Display { get { return TicketItemProperty != null ? GetTicketItemPropertyDisplayName(TicketItemProperty) : Name; } }

        private static string GetTicketItemPropertyDisplayName(TicketItemProperty ticketItemProperty)
        {
            return ticketItemProperty.Quantity > 1
                ? ticketItemProperty.Quantity + " " + ticketItemProperty.Name
                : ticketItemProperty.Name;
        }

        public TicketItemProperty TicketItemProperty { get; set; }

        public void Refresh()
        {
            RaisePropertyChanged("TicketItemProperty");
            RaisePropertyChanged("Display");
        }
    }
}
