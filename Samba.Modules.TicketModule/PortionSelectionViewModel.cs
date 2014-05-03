using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Presentation.ViewModels;
using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.Prism.Events;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Users;
using Samba.Presentation.Common;
using Samba.Presentation.ViewModels;
using Samba.Services;


namespace Samba.Modules.TicketModule
{
    [Export]
   public class PortionSelectionViewModel : ObservableObject
    {
         [ImportingConstructor]
        public PortionSelectionViewModel()
        {
          //  SelectedTicketViewModel = ticketViewModel;
            
            PortionSelectedCommand = new DelegateCommand<MenuItemPortion>(OnPortionSelected);
            SelectedItemPortions = new ObservableCollection<MenuItemPortion>() ; // SelectedTicketViewModel.SelectedItemPortions;
           
           
        }
         private void OnPortionSelected(MenuItemPortion obj)
         {
             PortionSelectionView.Topmost = false;
             PortionSelectionView.Hide();
             SelectedMenuItemPortion = obj;
             SelectedItemPortions.Clear();
            // SelectedTicketViewModel.OnPortionSelected(obj);
           
         }

         public MenuItemPortion SelectedMenuItemPortion { get; set; }

         public DelegateCommand<MenuItemPortion> PortionSelectedCommand { get; set; }

      //   public SelectedTicketItemsViewModel SelectedTicketViewModel { get; private set; }
        public ObservableCollection<MenuItemPortion> SelectedItemPortions { get; set; }
        public PortionSelectionView PortionSelectionView { get; set; }
      
       
    }
}
