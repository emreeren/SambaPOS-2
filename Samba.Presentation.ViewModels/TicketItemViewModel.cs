using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Practices.Prism.Commands;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Tickets;
using Samba.Localization.Properties;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;

namespace Samba.Presentation.ViewModels
{
    public class TicketItemViewModel : ObservableObject
    {
        public bool IsSelectedQuantityModified { get; set; }

        public TicketItemViewModel(TicketItem model)
        {
            _model = model;
            ResetSelectedQuantity();
            ItemSelectedCommand = new DelegateCommand<TicketItemViewModel>(OnItemSelected);
            Properties = new ObservableCollection<TicketItemPropertyViewModel>(model.Properties.Select(x => new TicketItemPropertyViewModel(x)));
            UpdateItemColor();
        }

        public DelegateCommand<TicketItemViewModel> ItemSelectedCommand { get; set; }

        public string Description
        {
            get
            {
                string desc = _model.MenuItemName + _model.GetPortionDesc();

                if (IsGifted) desc = Resources.Gift_ab + desc;
                if (IsVoided) desc = Resources.Void_ab + desc;

                if (IsSelectedQuantityModified)
                    desc = string.Format("({0:#.##}) {1}", Model.SelectedQuantity, desc);
                return desc;
            }
        }

        private readonly TicketItem _model;
        public TicketItem Model { get { return _model; } }

        public decimal Quantity
        {
            get { return _model.Quantity; }
            set
            {
                _model.Quantity = value;
                RaisePropertyChanged("Quantity");
                RaisePropertyChanged("TotalPrice");
                ResetSelectedQuantity();
            }
        }

        public decimal SelectedQuantity { get { return Model.SelectedQuantity; } }

        public void IncSelectedQuantity()
        {
            Model.IncSelectedQuantity();
            IsSelectedQuantityModified = true;
            RefreshSelectedItem();
        }
        public void DecSelectedQuantity()
        {
            Model.DecSelectedQuantity();
            IsSelectedQuantityModified = true;
            RefreshSelectedItem();
        }

        public void ResetSelectedQuantity()
        {
            Model.ResetSelectedQuantity();
            IsLastSelected = false;
            IsSelectedQuantityModified = false;
            RefreshSelectedItem();
        }

        private void RefreshSelectedItem()
        {
            RaisePropertyChanged("SelectedQuantity");
            RaisePropertyChanged("Description");
            RaisePropertyChanged("Background");
            RaisePropertyChanged("Foreground");
            RaisePropertyChanged("BorderThickness");
        }

        public decimal Price
        {
            get { return Model.GetMenuItemPrice(); }
        }

        public decimal TotalPrice
        {
            get { return Price * Quantity; }
        }

        private bool _selected;
        public bool Selected { get { return _selected; } set { _selected = value; UpdateItemColor(); RaisePropertyChanged("Selected"); } }

        private Brush _background;
        public Brush Background { get { return _background; } set { _background = value; RaisePropertyChanged("Background"); } }

        private Brush _foreground;
        public Brush Foreground { get { return _foreground; } set { _foreground = value; RaisePropertyChanged("Foreground"); } }

        public int BorderThickness { get { return IsLastSelected ? 1 : 0; } }

        public string OrderNumber
        {
            get
            {
                return Model.OrderNumber > 0 ? string.Format(Resources.OrderNumber_f,
                    Model.OrderNumber, CreatingUserName) : Resources.NewOrder;
            }
        }

        public object GroupObject { get { return new { OrderNumber, Time = Model.Id > 0 ? Model.CreatedDateTime.ToShortTimeString() : "" }; } }

        public string CreatingUserName { get { return AppServices.MainDataContext.GetUserName(Model.CreatingUserId); } }

        public string CustomPropertyName
        {
            get { return Model.GetCustomProperty() != null ? Model.GetCustomProperty().Name : ""; }
            set
            {
                Model.UpdateCustomProperty(value, CustomPropertyPrice, CustomPropertyQuantity);
                RefreshProperties();
            }
        }

        public decimal CustomPropertyPrice
        {
            get
            {
                var prop = Model.GetCustomProperty();
                if (prop != null)
                {
                    return Model.VatIncluded ? prop.PropertyPrice.Amount + prop.VatAmount : prop.PropertyPrice.Amount;
                }
                return 0;
            }
            set
            {
                Model.UpdateCustomProperty(CustomPropertyName, value, CustomPropertyQuantity);
                RefreshProperties();
            }
        }

        public decimal CustomPropertyQuantity
        {
            get { return Model.GetCustomProperty() != null ? Model.GetCustomProperty().Quantity : 1; }
            set
            {
                Model.UpdateCustomProperty(CustomPropertyName, CustomPropertyPrice, value);
                RefreshProperties();
            }
        }

        public TextDecorationCollection TextDecoration
        {
            get
            {
                return Model.Voided ? TextDecorations.Strikethrough : null;
            }
        }

        public FontWeight FontWeight
        {
            get
            {
                return _model.Locked ? FontWeights.Bold : FontWeights.Normal;
            }
        }

        public string Reason { get { return Model.ReasonId > 0 ? AppServices.MainDataContext.GetReason(Model.ReasonId) : ""; } }

        public string PriceTag { get { return Model.PriceTag + (!string.IsNullOrEmpty(Model.Tag) ? string.Format(" [{0}]", Model.Tag) : ""); } }

        public ObservableCollection<TicketItemPropertyViewModel> Properties { get; private set; }

        public bool IsGifted { get { return Model.Gifted; } }
        public bool IsVoided { get { return Model.Voided; } }
        public bool IsLocked { get { return Model.Locked; } }
        private bool _isLastSelected;
        public bool IsLastSelected
        {
            get { return _isLastSelected; }
            set
            {
                _isLastSelected = value;
                RaisePropertyChanged("BorderThickness");
            }
        }

        private void OnItemSelected(TicketItemViewModel obj)
        {
            if (Selected && !IsLocked)
            {
                var unselectedItem = AppServices.DataAccessService.GetUnselectedItem(obj.Model);
                if (unselectedItem != null)
                {
                    InteractionService.UserIntraction.GiveFeedback(string.Format(Resources.SelectionRequired_f, unselectedItem.Name));
                    return;
                }
            }
            Selected = !Selected;
            if (!Selected) ResetSelectedQuantity();
            this.PublishEvent(EventTopicNames.SelectedItemsChanged);
        }

        private void UpdateItemColor()
        {
            if (Selected)
            {
                Background = SystemColors.HighlightBrush;
                Foreground = SystemColors.HighlightTextBrush;
            }
            else
            {
                Background = null;
                Foreground = SystemColors.WindowTextBrush;

                if (IsLocked)
                    Foreground = Brushes.DarkRed;
                if (IsVoided)
                    Foreground = Brushes.Gray;
                if (IsGifted)
                    Foreground = Brushes.DarkBlue;
            }
        }

        public void NotSelected()
        {
            if (_selected)
            {
                _selected = false;
                IsLastSelected = false;
                UpdateItemColor();
            }
        }

        public void UpdatePortion(MenuItemPortion portion, string priceTag)
        {
            _model.UpdatePortion(portion, priceTag, AppServices.MainDataContext.GetVatTemplate(portion.MenuItemId));
            RuleExecutor.NotifyEvent(RuleEventNames.PortionSelected,
                   new
                   {
                       Ticket = AppServices.MainDataContext.SelectedTicket,
                       TicketItem = Model,
                       TicketTag = AppServices.MainDataContext.SelectedTicket.Tag,
                       MenuItemName = _model.MenuItemName,
                       PortionName = portion.Name,
                       PortionPrice = _model.Price
                   });
            RaisePropertyChanged("Description");
            RaisePropertyChanged("TotalPrice");
        }

        public TicketItemProperty ToggleProperty(MenuItemPropertyGroup group, MenuItemProperty menuItemProperty)
        {
            var ti = _model.ToggleProperty(group, menuItemProperty);
            if (ti != null)
            {
                RuleExecutor.NotifyEvent(RuleEventNames.ModifierSelected,
                                         new
                                             {
                                                 Ticket = AppServices.MainDataContext.SelectedTicket,
                                                 TicketItem = Model,
                                                 TicketTag = AppServices.MainDataContext.SelectedTicket.Tag,
                                                 MenuItemName = _model.MenuItemName,
                                                 ModifierGroupName = group.Name,
                                                 ModifierName = menuItemProperty.Name,
                                                 ModifierPrice = ti.PropertyPrice.Amount + ti.VatAmount,
                                                 ModifierQuantity = ti.Quantity,
                                                 IsRemoved = !_model.Properties.Contains(ti),
                                                 IsPriceAddedToParentPrice = ti.CalculateWithParentPrice,
                                                 TotalPropertyCount = Model.Properties.Count,
                                                 TotalModifierQuantity = Model.Properties.Where(x => x.PropertyGroupId == group.Id).Sum(x => x.Quantity),
                                                 TotalModifierPrice = Model.Properties.Where(x => x.PropertyGroupId == group.Id).Sum(x => x.PropertyPrice.Amount + x.VatAmount)
                                             });
            }
            RefreshProperties();
            RaisePropertyChanged("Properties");
            RaisePropertyChanged("TotalPrice");
            RaisePropertyChanged("Quantity");
            return ti;
        }

        private void RefreshProperties()
        {
            Properties.Clear();
            Properties.AddRange(Model.Properties.Select(x => new TicketItemPropertyViewModel(x)));
        }

        public void UpdatePrice(decimal value)
        {
            Model.UpdatePrice(value, AppServices.MainDataContext.SelectedDepartment.PriceTag);
            RaisePropertyChanged("Price");
            RaisePropertyChanged("TotalPrice");
        }

        public void RemoveProperty(MenuItemPropertyGroup mig, MenuItemProperty menuItemProperty)
        {
            var p = Model.Properties.Where(x => x.PropertyGroupId == mig.Id && x.Name == menuItemProperty.Name && (x.VatAmount + x.PropertyPrice.Amount) == menuItemProperty.Price.Amount).FirstOrDefault();
            if (p != null)
            {
                Model.Properties.Remove(p);
            }
            RefreshProperties();
            RaisePropertyChanged("Properties");
            RaisePropertyChanged("TotalPrice");
            RaisePropertyChanged("Quantity");
        }
    }
}
