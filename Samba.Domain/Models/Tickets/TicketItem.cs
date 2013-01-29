using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Diagnostics;
using Samba.Domain.Foundation;
using Samba.Domain.Models.Menus;
using Samba.Infrastructure.Settings;

namespace Samba.Domain.Models.Tickets
{
    public class TicketItem
    {
        public TicketItem()
        {
            _properties = new List<TicketItemProperty>();
            CreatedDateTime = DateTime.Now;
            ModifiedDateTime = DateTime.Now;
            _selectedQuantity = 0;
        }

        public int Id { get; set; }
        public int TicketId { get; set; }
        public int MenuItemId { get; set; }
        public string MenuItemName { get; set; }
        public string PortionName { get; set; }
        public decimal Price { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Quantity { get; set; }
        public int PortionCount { get; set; }
        public bool Locked { get; set; }
        public bool Voided { get; set; }
        public int ReasonId { get; set; }
        public bool Gifted { get; set; }
        public int OrderNumber { get; set; }
        public int CreatingUserId { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int ModifiedUserId { get; set; }
        public DateTime ModifiedDateTime { get; set; }
        [StringLength(10)]
        public string PriceTag { get; set; }
        public string Tag { get; set; }
        public int DepartmentId { get; set; }

        public decimal VatRate { get; set; }
        public decimal VatAmount { get; set; }
        public int VatTemplateId { get; set; }
        public bool VatIncluded { get; set; }

        private IList<TicketItemProperty> _properties;
        public virtual IList<TicketItemProperty> Properties
        {
            get { return _properties; }
            set { _properties = value; }
        }

        decimal _selectedQuantity;
        public decimal SelectedQuantity { get { return _selectedQuantity; } }

        private TicketItemProperty _lastSelectedProperty;
        public TicketItemProperty LastSelectedProperty
        {
            get { return _lastSelectedProperty; }
        }

        public void UpdateMenuItem(int userId, MenuItem menuItem, string portionName, string priceTag, int quantity, string defaultProperties)
        {
            MenuItemId = menuItem.Id;
            MenuItemName = menuItem.Name;
            var portion = menuItem.GetPortion(portionName);
            Debug.Assert(portion != null);
            UpdatePortion(portion, priceTag, menuItem.VatTemplate);
            Quantity = quantity;
            _selectedQuantity = quantity;
            PortionCount = menuItem.Portions.Count;
            CreatingUserId = userId;
            CreatedDateTime = DateTime.Now;

            if (!string.IsNullOrEmpty(defaultProperties))
            {
                foreach (var menuItemPropertyGroup in menuItem.PropertyGroups)
                {
                    var properties = defaultProperties.Split(',');
                    foreach (var defaultProperty in properties)
                    {
                        var property = defaultProperty.Trim();
                        var pQuantity = 1;
                        if (defaultProperty.Contains("*"))
                        {
                            var parts = defaultProperty.Split(new[] { '*' }, 1);
                            if (!string.IsNullOrEmpty(parts[0].Trim()))
                            {
                                property = parts[0];
                                int.TryParse(parts[1], out pQuantity);
                            }
                            else continue;
                        }
                        var defaultValue = menuItemPropertyGroup.Properties.FirstOrDefault(x => x.Name == property);
                        if (defaultValue != null)
                        {
                            for (int i = 0; i < pQuantity; i++)
                            {
                                ToggleProperty(menuItemPropertyGroup, defaultValue);
                            }
                        }
                    }
                }
            }
        }

        public void UpdatePortion(MenuItemPortion portion, string priceTag, VatTemplate vatTemplate)
        {
            PortionName = portion.Name;

            if (vatTemplate != null)
            {
                VatRate = vatTemplate.Rate;
                VatIncluded = vatTemplate.VatIncluded;
                VatTemplateId = vatTemplate.Id;
            }

            if (!string.IsNullOrEmpty(priceTag))
            {
                string tag = priceTag;
                var price = portion.Prices.SingleOrDefault(x => x.PriceTag == tag);
                if (price != null && price.Price > 0)
                {
                    UpdatePrice(price.Price, price.PriceTag);
                }
                else priceTag = "";
            }

            if (string.IsNullOrEmpty(priceTag))
            {
                UpdatePrice(portion.Price.Amount, "");
            }

            CurrencyCode = LocalSettings.CurrencySymbol;
            foreach (var ticketItemProperty in Properties)
            {
                ticketItemProperty.PortionName = portion.Name;
            }
        }

        public TicketItemProperty ToggleProperty(MenuItemPropertyGroup group, MenuItemProperty property)
        {
            if (property.Name.ToLower() == "x")
            {
                var groupItems = Properties.Where(x => x.PropertyGroupId == group.Id).ToList();
                foreach (var tip in groupItems) Properties.Remove(tip);
                if (group.MultipleSelection) Quantity = 1;
                return null;
            }

            var ti = FindProperty(property);
            if (ti == null)
            {
                ti = new TicketItemProperty
                        {
                            Name = property.Name,
                            PropertyPrice = new Price { Amount = property.Price.Amount, CurrencyCode = property.Price.CurrencyCode },
                            PropertyGroupId = group.Id,
                            MenuItemId = property.MenuItemId,
                            CalculateWithParentPrice = group.CalculateWithParentPrice,
                            PortionName = PortionName,
                            Quantity = group.MultipleSelection ? 0 : 1
                        };

                if (VatIncluded && VatRate > 0)
                {
                    ti.PropertyPrice.Amount = ti.PropertyPrice.Amount / ((100 + VatRate) / 100);
                    ti.PropertyPrice.Amount = decimal.Round(ti.PropertyPrice.Amount, 2);
                    ti.VatAmount = property.Price.Amount - ti.PropertyPrice.Amount;
                }
                else if (VatRate > 0) ti.VatAmount = (property.Price.Amount * VatRate) / 100;
                else ti.VatAmount = 0;
            }
            if (group.SingleSelection || !string.IsNullOrEmpty(group.GroupTag))
            {
                var tip = Properties.FirstOrDefault(x => x.PropertyGroupId == group.Id);
                if (tip != null)
                {
                    Properties.Insert(Properties.IndexOf(tip), ti);
                    Properties.Remove(tip);
                }
            }
            else if (group.MultipleSelection)
            {
                ti.Quantity++;
            }
            else if (!group.MultipleSelection && Properties.Contains(ti))
            {
                Properties.Remove(ti);
                _lastSelectedProperty = ti;
                return ti;
            }

            if (!Properties.Contains(ti)) Properties.Add(ti);

            _lastSelectedProperty = ti;
            return ti;
        }

        public TicketItemProperty GetCustomProperty()
        {
            return Properties.FirstOrDefault(x => x.PropertyGroupId == 0);
        }

        public TicketItemProperty GetOrCreateCustomProperty()
        {
            var tip = GetCustomProperty();
            if (tip == null)
            {
                tip = new TicketItemProperty
                          {
                              Name = "",
                              PropertyPrice = new Price(0, LocalSettings.CurrencySymbol),
                              PropertyGroupId = 0,
                              MenuItemId = 0,
                              Quantity = 0
                          };
                Properties.Add(tip);
            }
            return tip;
        }

        public void UpdateCustomProperty(string text, decimal price, decimal quantity)
        {
            var tip = GetOrCreateCustomProperty();
            if (string.IsNullOrEmpty(text))
            {
                Properties.Remove(tip);
            }
            else
            {
                tip.Name = text;
                tip.PropertyPrice = new Price(price, LocalSettings.CurrencySymbol);
                if (VatIncluded && VatRate > 0)
                {
                    tip.PropertyPrice.Amount = tip.PropertyPrice.Amount / ((100 + VatRate) / 100);
                    tip.PropertyPrice.Amount = decimal.Round(tip.PropertyPrice.Amount, 2);
                    tip.VatAmount = price - tip.PropertyPrice.Amount;
                }
                else if (VatRate > 0) tip.VatAmount = (price * VatRate) / 100;
                else VatAmount = 0;

                tip.Quantity = quantity;
            }
        }

        private TicketItemProperty FindProperty(MenuItemProperty property)
        {
            return Properties.FirstOrDefault(x => x.Name == property.Name && (x.PropertyPrice.Amount + x.VatAmount) == property.Price.Amount);
        }

        public decimal GetTotal()
        {
            return Voided || Gifted ? 0 : GetItemValue();
        }

        public decimal GetItemValue()
        {
            return Quantity * GetItemPrice();
        }

        public decimal GetSelectedValue()
        {
            return SelectedQuantity > 0 ? SelectedQuantity * GetItemPrice() : GetItemValue();
        }

        public decimal GetItemPrice()
        {
            var result = Price + GetTotalPropertyPrice();
            if (VatIncluded) result += VatAmount;
            return result;
        }

        public decimal GetMenuItemPrice()
        {
            var result = Price + GetMenuItemPropertyPrice();
            if (VatIncluded) result += VatAmount;
            return result;
        }

        public decimal GetTotalPropertyPrice()
        {
            return GetPropertySum(Properties, VatIncluded);
        }

        public decimal GetPropertyPrice()
        {
            return GetPropertySum(Properties.Where(x => !x.CalculateWithParentPrice), VatIncluded);
        }

        public decimal GetMenuItemPropertyPrice()
        {
            return GetPropertySum(Properties.Where(x => x.CalculateWithParentPrice), VatIncluded);
        }

        private static decimal GetPropertySum(IEnumerable<TicketItemProperty> properties, bool vatIncluded)
        {
            return properties.Sum(property => (property.PropertyPrice.Amount + (vatIncluded ? property.VatAmount : 0)) * property.Quantity);
        }

        public void IncSelectedQuantity()
        {
            _selectedQuantity++;
            if (_selectedQuantity > Quantity) _selectedQuantity = 1;
        }

        public void DecSelectedQuantity()
        {
            _selectedQuantity--;
            if (_selectedQuantity < 1) _selectedQuantity = 1;
        }

        public void ResetSelectedQuantity()
        {
            _selectedQuantity = Quantity;
        }

        public void UpdateSelectedQuantity(decimal value)
        {
            _selectedQuantity = value;
            if (_selectedQuantity > Quantity) _selectedQuantity = 1;
            if (_selectedQuantity < 1) _selectedQuantity = 1;
        }

        public string GetPortionDesc()
        {
            if (PortionCount > 1
                && !string.IsNullOrEmpty(PortionName)
                && !string.IsNullOrEmpty(PortionName.Trim('\b', ' ', '\t'))
                && PortionName.ToLower() != "normal")
                return "." + PortionName;
            return "";
        }

        public void UpdatePrice(decimal value, string priceTag)
        {
            Price = value;
            PriceTag = priceTag;
            if (VatIncluded && VatRate > 0)
            {
                Price = Price / ((100 + VatRate) / 100);
                Price = decimal.Round(Price, 2);
                VatAmount = value - Price;
            }
            else if (VatRate > 0) VatAmount = (Price * VatRate) / 100;
            else VatAmount = 0;
            VatAmount = decimal.Round(VatAmount, 2);
        }

        public decimal GetTotalVatAmount()
        {
            return !Voided && !Gifted ? (VatAmount + Properties.Sum(x => x.VatAmount)) * Quantity : 0;
        }
    }
}
