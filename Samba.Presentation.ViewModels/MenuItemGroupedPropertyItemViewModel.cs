using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Presentation.Common;

namespace Samba.Presentation.ViewModels
{
    public class MenuItemGroupedPropertyItemViewModel : ObservableObject
    {
        public MenuItemGroupedPropertyItemViewModel(TicketItemViewModel selectedItem, MenuItemPropertyGroup menuItemPropertyGroup)
        {
            _selectedItem = selectedItem;
            MenuItemPropertyGroup = menuItemPropertyGroup;
            UpdateNextProperty(null);
        }

        public void UpdateNextProperty(MenuItemProperty current)
        {
            CurrentProperty = current;
            if (CurrentProperty != null && CurrentProperty.Name.ToLower() == "x")
                CurrentProperty = null;
            NextProperty = MenuItemPropertyGroup.Properties.First();
            var selected = _selectedItem.Properties.FirstOrDefault(x => x.Model.PropertyGroupId == MenuItemPropertyGroup.Id);
            var selectedPropertyName = selected != null ? selected.Model.Name : "";

            if (!string.IsNullOrEmpty(selectedPropertyName))
            {
                CurrentProperty = MenuItemPropertyGroup.Properties.FirstOrDefault(x => x.Name == selectedPropertyName);
                var nProp = MenuItemPropertyGroup.Properties.SkipWhile(x => x.Name != selectedPropertyName).Skip(1).FirstOrDefault();
                if (nProp != null) NextProperty = nProp;
            }
            Name = CurrentProperty != null ? CurrentProperty.Name : MenuItemPropertyGroup.Name;
        }

        public MenuItemPropertyGroup MenuItemPropertyGroup { get; set; }
        public MenuItemProperty NextProperty { get; set; }
        public MenuItemProperty CurrentProperty { get; set; }

        private TicketItemProperty _ticketItemProperty;
        public TicketItemProperty TicketItemProperty
        {
            get { return _ticketItemProperty; }
            set
            {
                _ticketItemProperty = value;
                RaisePropertyChanged("TicketItemProperty");
            }
        }

        private readonly TicketItemViewModel _selectedItem;

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                RaisePropertyChanged("Name");
            }
        }

    }
}