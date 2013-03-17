﻿using System;
using System.Collections.Generic;
using Samba.Domain.Foundation;
using Samba.Infrastructure.Data;
using Samba.Infrastructure.Settings;

namespace Samba.Domain.Models.Menus
{
    public class MenuItem : IEntity
    {
        public MenuItem()
            : this(string.Empty)
        {

        }

        public MenuItem(string name)
        {
            Name = name;
            _portions = new List<MenuItemPortion>();
            _propertyGroups = new List<MenuItemPropertyGroup>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] LastUpdateTime { get; set; }
        public string GroupCode { get; set; }
        public string Barcode { get; set; }
        public string Tag { get; set; }

        public virtual VatTemplate VatTemplate { get; set; }

        private IList<MenuItemPortion> _portions;
        public virtual IList<MenuItemPortion> Portions
        {
            get { return _portions; }
            set { _portions = value; }
        }

        private IList<MenuItemPropertyGroup> _propertyGroups;
        public virtual IList<MenuItemPropertyGroup> PropertyGroups
        {
            get { return _propertyGroups; }
            set { _propertyGroups = value; }
        }

        private static MenuItem _all;
        public static MenuItem All { get { return _all ?? (_all = new MenuItem { Name = "*" }); } }

        public MenuItemPortion AddPortion(string portionName, decimal price, string currencyCode)
        {
            var mip = new MenuItemPortion
            {
                Name = portionName,
                Price = new Price(price, currencyCode),
                MenuItemId = Id
            };
            Portions.Add(mip);
            return mip;
        }

        internal MenuItemPortion GetPortion(string portionName)
        {
            foreach (var portion in Portions)
            {
                if (portion.Name == portionName)
                    return portion;
            }
            if (string.IsNullOrEmpty(portionName) && Portions.Count > 0) return Portions[0];
            throw new Exception("Porsiyon Tanımlı Değil.");
        }

        public string UserString
        {
            get { return string.Format("{0} [{1}]", Name, GroupCode); }
        }

        public static MenuItemPortion AddDefaultMenuPortion(MenuItem item)
        {
            return item.AddPortion(Localization.Properties.Resources.DefaultMenuPortion ?? "Normal", 0, LocalSettings.CurrencySymbol);
        }

        public static MenuItemProperty AddDefaultMenuItemProperty(MenuItemPropertyGroup item)
        {
            return item.AddProperty("", 0, LocalSettings.CurrencySymbol);
        }
        public static MenuItemProperty AddDefaultMenuItemProperty(MenuItemPropertyGroup item, string name, decimal price)
        {
            return item.AddProperty(name, price, LocalSettings.CurrencySymbol);
        }

        public static MenuItem Create()
        {
            var result = new MenuItem();
            AddDefaultMenuPortion(result);
            return result;
        }
    }
}
