using System.Collections.Generic;
using System.Linq;
using Samba.Domain.Models.Menus;
using Samba.Presentation.Common;

namespace Samba.Presentation.ViewModels
{
    public class MenuItemGroupedPropertyViewModel : ObservableObject
    {
        public string Name { get; set; }
        public IEnumerable<MenuItemGroupedPropertyItemViewModel> Properties { get; set; }
        public int ColumnCount { get; set; }
        public int ButtonHeight { get; set; }
        public int TerminalColumnCount { get; set; }
        public int TerminalButtonHeight { get; set; }

        public MenuItemGroupedPropertyViewModel(TicketItemViewModel selectedItem, IGrouping<string, MenuItemPropertyGroup> menuItemPropertyGroups)
        {
            Name = menuItemPropertyGroups.Key;
            Properties = menuItemPropertyGroups.Select(x => new MenuItemGroupedPropertyItemViewModel(selectedItem, x)).ToList();
            ColumnCount = menuItemPropertyGroups.First().ColumnCount;
            ButtonHeight = menuItemPropertyGroups.First().ButtonHeight;
            TerminalButtonHeight = menuItemPropertyGroups.First().TerminalButtonHeight;
            TerminalColumnCount = menuItemPropertyGroups.First().TerminalColumnCount;
        }
    }
}