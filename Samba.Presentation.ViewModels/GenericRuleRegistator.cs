using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Domain.Models.Users;
using Samba.Localization.Properties;
using Samba.Persistance.Data;
using Samba.Presentation.Common;
using Samba.Presentation.Common.Services;
using Samba.Services;
using Samba.Infrastructure.Data.Serializer;

namespace Samba.Presentation.ViewModels
{
    public static class GenericRuleRegistator
    {
        private static bool _registered;
        public static void RegisterOnce()
        {
            Debug.Assert(_registered == false);
            RegisterActions();
            RegisterRules();
            RegisterParameterSources();
            HandleEvents();
            RegisterNotifiers();
            _registered = true;
        }

        private static void RegisterActions()
        {
            RuleActionTypeRegistry.RegisterActionType("SendEmail", Resources.SendEmail, new { SMTPServer = "", SMTPUser = "", SMTPPassword = "", SMTPPort = 0, ToEMailAddress = "", Subject = "", FromEMailAddress = "", EMailMessage = "", FileName = "", DeleteFile = false, BypassSslErrors = false });
            RuleActionTypeRegistry.RegisterActionType("AddTicketDiscount", Resources.AddTicketDiscount, new { DiscountPercentage = 0m });
            RuleActionTypeRegistry.RegisterActionType("AddTicketItem", Resources.AddTicketItem, new { MenuItemName = "", PortionName = "", Quantity = 0, Gift = false, GiftReason = "", Tag = "" });
            RuleActionTypeRegistry.RegisterActionType("GiftLastTicketItem", Resources.GiftLastTicketItem, new { GiftReason = "", Quantity = 0 });
            RuleActionTypeRegistry.RegisterActionType("UpdateLastTicketItemPriceTag", Resources.UpdateLastTicketItemPriceTag, new { PriceTag = "" });
            RuleActionTypeRegistry.RegisterActionType("UpdateTicketItemPriceTag", Resources.UpdateTicketItemPriceTag, new { PriceTag = "" });
            RuleActionTypeRegistry.RegisterActionType("UpdateTicketItemTag", Resources.UpdateTicketItemTag, new { Tag = "" });
            RuleActionTypeRegistry.RegisterActionType("VoidTicketItems", Resources.VoidTicketItems, new { MenuItemName = "", Tag = "" });
            RuleActionTypeRegistry.RegisterActionType("RemoveLastModifier", Resources.RemoveLastModifier);
            RuleActionTypeRegistry.RegisterActionType("UpdateTicketTag", Resources.UpdateTicketTag, new { TagName = "", TagValue = "" });
            RuleActionTypeRegistry.RegisterActionType("UpdatePriceTag", Resources.UpdatePriceTag, new { DepartmentName = "", PriceTag = "" });
            RuleActionTypeRegistry.RegisterActionType("RefreshCache", Resources.RefreshCache);
            RuleActionTypeRegistry.RegisterActionType("SendMessage", Resources.BroadcastMessage, new { Command = "" });
            RuleActionTypeRegistry.RegisterActionType("UpdateProgramSetting", Resources.UpdateProgramSetting, new { SettingName = "", SettingValue = "", UpdateType = Resources.Update, IsLocal = true });
            RuleActionTypeRegistry.RegisterActionType("ExecuteTicketEvent", Resources.ExecuteTicketOperation, new { TicketOperationType = "" });
            RuleActionTypeRegistry.RegisterActionType("UpdateTicketVat", Resources.UpdateTicketVat, new { VatTemplate = "" });
            RuleActionTypeRegistry.RegisterActionType("RegenerateTicketVat", Resources.RegenerateTicketVat);
            RuleActionTypeRegistry.RegisterActionType("UpdateTicketTaxService", Resources.UpdateTicketTaxService, new { TaxServiceTemplate = "", Amount = 0m });
            RuleActionTypeRegistry.RegisterActionType("UpdateTicketAccount", Resources.UpdateTicketAccount, new { AccountPhone = "", AccountName = "", Note = "" });
            RuleActionTypeRegistry.RegisterActionType("ExecutePrintJob", Resources.ExecutePrintJob, new { PrintJobName = "", TicketItemTag = "" });
            RuleActionTypeRegistry.RegisterActionType("UpdateApplicationSubTitle", "Update Application Subtitle", new { Title = "", Color = "White", FontSize = 12 });
        }

        private static void RegisterRules()
        {
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.UserLoggedIn, Resources.UserLogin, new { UserName = "", RoleName = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.UserLoggedOut, Resources.UserLogout, new { UserName = "", RoleName = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.WorkPeriodStarts, Resources.WorkPeriodStarted, new { UserName = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.WorkPeriodEnds, Resources.WorkPeriodEnded, new { UserName = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TriggerExecuted, Resources.TriggerExecuted, new { TriggerName = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketCreated, Resources.TicketCreated);
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketLocationChanged, Resources.TicketLocationChanged, new { OldLocation = "", NewLocation = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketTagSelected, Resources.TicketTagSelected, new { TagName = "", TagValue = "", NumericValue = 0, TicketTag = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.CustomerSelectedForTicket, Resources.CustomerSelectedForTicket, new { CustomerId = 0, CustomerName = "", CustomerGroupCode = "", PhoneNumber = "", CustomerNote = "", LastOrderTotal = 0m, LastOrderDayCount = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketTotalChanged, Resources.TicketTotalChanged, new { TicketTotal = 0m, PreviousTotal = 0m, DiscountTotal = 0m, GiftTotal = 0m, DiscountAmount = 0m, TipAmount = 0m, CustomerName = "", CustomerGroupCode = "", CustomerId = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.MessageReceived, Resources.MessageReceived, new { Command = "" });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.PaymentReceived, Resources.PaymentReceived, new { PaymentType = "", Amount = 0, TicketTag = "", CustomerId = 0, CustomerName = "", CustomerGroupCode = "", SelectedLinesCount = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketLineAdded, Resources.LineAddedToTicket, new { TicketId = 0m, TicketTag = "", MenuItemName = "", Quantity = 0m, MenuItemGroupCode = "", CustomerName = "", CustomerGroupCode = "", CustomerId = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketLineCancelled, Resources.TicketLineCancelled, new { TicketTag = "", MenuItemName = "", Quantity = 0m, MenuItemGroupCode = "", CustomerName = "", CustomerGroupCode = "", CustomerId = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.PortionSelected, Resources.PortionSelected, new { TicketTag = "", MenuItemName = "", PortionName = "", PortionPrice = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.ModifierSelected, Resources.ModifierSelected, new { TicketTag = "", MenuItemName = "", ModifierGroupName = "", ModifierName = "", ModifierPrice = 0, ModifierQuantity = 0, IsRemoved = false, IsPriceAddedToParentPrice = false, TotalPropertyCount = 0, TotalModifierQuantity = 0m, TotalModifierPrice = 0m });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.ChangeAmountChanged, Resources.ChangeAmountUpdated, new { TicketAmount = 0, ChangeAmount = 0, TenderedAmount = 0 });
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.TicketClosed, Resources.TicketClosed);
            RuleActionTypeRegistry.RegisterEvent(RuleEventNames.ApplicationStarted, Resources.ApplicationStarted, new { CommandLineArguments = "" });
        }

        private static void RegisterParameterSources()
        {
            RuleActionTypeRegistry.RegisterParameterSoruce("UserName", () => AppServices.MainDataContext.Users.Select(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("DepartmentName", () => AppServices.MainDataContext.Departments.Select(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("TerminalName", () => AppServices.Terminals.Select(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("TriggerName", () => Dao.Select<Trigger, string>(yz => yz.Name, y => !string.IsNullOrEmpty(y.Expression)));
            RuleActionTypeRegistry.RegisterParameterSoruce("MenuItemName", () => Dao.Select<MenuItem, string>(yz => yz.Name, y => y.Id > 0));
            RuleActionTypeRegistry.RegisterParameterSoruce("PriceTag", () => Dao.Select<MenuItemPriceDefinition, string>(x => x.PriceTag, x => x.Id > 0));
            RuleActionTypeRegistry.RegisterParameterSoruce("Color", () => typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static).Select(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("VatTemplate", () => Dao.Select<VatTemplate, string>(x => x.Name, x => x.Id > 0));
            RuleActionTypeRegistry.RegisterParameterSoruce("TaxServiceTemplate", () => Dao.Select<TaxServiceTemplate, string>(x => x.Name, x => x.Id > 0));
            RuleActionTypeRegistry.RegisterParameterSoruce("TagName", () => Dao.Select<TicketTagGroup, string>(x => x.Name, x => x.Id > 0));
            RuleActionTypeRegistry.RegisterParameterSoruce("PaymentType", () => new[] { Resources.Cash, Resources.CreditCard, Resources.Ticket });
            RuleActionTypeRegistry.RegisterParameterSoruce("PrintJobName", () => Dao.Distinct<PrintJob>(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("CustomerGroupCode", () => Dao.Distinct<Customer>(x => x.GroupCode));
            RuleActionTypeRegistry.RegisterParameterSoruce("MenuItemGroupCode", () => Dao.Distinct<MenuItem>(x => x.GroupCode));
            RuleActionTypeRegistry.RegisterParameterSoruce("UpdateType", () => new[] { Resources.Update, Resources.Increase, Resources.Decrease, "Toggle", "Multiply" });
            RuleActionTypeRegistry.RegisterParameterSoruce("GiftReason", () => Dao.Select<Reason, string>(x => x.Name, x => x.ReasonType == 1).Distinct());
            RuleActionTypeRegistry.RegisterParameterSoruce("PortionName", () => Dao.Distinct<MenuItemPortion>(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("ModifierGroupName", () => Dao.Distinct<MenuItemPropertyGroup>(x => x.Name));
            RuleActionTypeRegistry.RegisterParameterSoruce("TicketOperationType", () => new[] { Resources.Refresh, Resources.Close });
        }

        private static void ResetCache()
        {
            TriggerService.UpdateCronObjects();
            AppServices.ResetCache();
            AppServices.MainDataContext.SelectedDepartment.PublishEvent(EventTopicNames.SelectedDepartmentChanged);
            CommandManager.InvalidateRequerySuggested();
        }

        private static void HandleEvents()
        {
            EventServiceFactory.EventService.GetEvent<GenericEvent<ActionData>>().Subscribe(x =>
            {
                if (x.Value.Action.ActionType == "UpdateApplicationSubTitle")
                {
                    var title = x.Value.GetAsString("Title");
                    PresentationServices.SubTitle.ApplicationTitle = title;
                    var fontSize = x.Value.GetAsInteger("FontSize");
                    if (fontSize > 0) PresentationServices.SubTitle.ApplicationTitleFontSize = fontSize;
                    var fontColor = x.Value.GetAsString("Color");
                    if (!string.IsNullOrEmpty(fontColor))
                        PresentationServices.SubTitle.ApplicationTitleColor = fontColor;
                }
                if (x.Value.Action.ActionType == "RemoveLastModifier")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket == null) return;
                    var ti = x.Value.GetDataValue<TicketItem>("TicketItem");
                    if (ti == null) return;
                    if (ti.Properties.Count > 0)
                    {
                        var prop = ti.LastSelectedProperty ?? ti.Properties.Last();
                        prop.Quantity--;
                        if (prop.Quantity < 1)
                            ti.Properties.Remove(prop);
                    }
                    TicketViewModel.RecalculateTicket(ticket);
                }

                if (x.Value.Action.ActionType == "UpdateTicketItemPriceTag")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket == null) return;

                    var ti = x.Value.GetDataValue<TicketItem>("TicketItem");
                    if (ti == null) return;

                    var priceTag = x.Value.GetAsString("PriceTag");
                    var mi = AppServices.DataAccessService.GetMenuItem(ti.MenuItemId);
                    if (mi == null) return;

                    var portion = mi.Portions.SingleOrDefault(y => y.Name == ti.PortionName);
                    if (portion == null) return;

                    ti.UpdatePortion(portion, priceTag, null);

                    TicketViewModel.RecalculateTicket(ticket);
                    EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                }

                if (x.Value.Action.ActionType == "UpdateTicketItemTag")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket == null) return;

                    var ti = x.Value.GetDataValue<TicketItem>("TicketItem");
                    if (ti == null) return;

                    var tag = x.Value.GetAsString("Tag");
                    ti.Tag = tag;
                    decimal val;
                    decimal.TryParse(tag, out val);
                }

                if (x.Value.Action.ActionType == "UpdateLastTicketItemPriceTag")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket == null) return;

                    var ti = ticket.TicketItems.LastOrDefault();
                    if (ti == null) return;

                    var priceTag = x.Value.GetAsString("PriceTag");
                    var mi = AppServices.DataAccessService.GetMenuItem(ti.MenuItemId);
                    if (mi == null) return;

                    var portion = mi.Portions.SingleOrDefault(y => y.Name == ti.PortionName);
                    if (portion == null) return;

                    ti.UpdatePortion(portion, priceTag, null);

                    TicketViewModel.RecalculateTicket(ticket);
                    EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                }

                if (x.Value.Action.ActionType == "GiftLastTicketItem")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var ti = ticket.TicketItems.LastOrDefault();
                        if (ti != null)
                        {
                            decimal quantity;
                            decimal.TryParse(x.Value.Action.GetParameter("Quantity"), out quantity);
                            if (quantity > 0 && ti.Quantity > quantity)
                            {
                                ti.UpdateSelectedQuantity(quantity);
                                ti = ticket.ExtractSelectedTicketItems(new List<TicketItem> { ti }).FirstOrDefault();
                                if (ti == null) return;
                                AppServices.MainDataContext.AddItemToSelectedTicket(ti);
                            }
                            var reasonId = 0;
                            var giftReason = x.Value.Action.GetParameter("GiftReason");
                            if (!string.IsNullOrEmpty(giftReason))
                            {
                                var reason = Dao.SingleWithCache<Reason>(u => u.Name == giftReason);
                                if (reason != null) reasonId = reason.Id;
                            }
                            ticket.GiftItem(ti, reasonId, AppServices.CurrentLoggedInUser.Id);
                            TicketViewModel.RecalculateTicket(ticket);
                            EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                        }
                    }
                }

                if (x.Value.Action.ActionType == "UpdateTicketAccount")
                {
                    Expression<Func<Customer, bool>> qFilter = null;

                    var phoneNumber = x.Value.GetAsString("AccountPhone");
                    var accountName = x.Value.GetAsString("AccountName");
                    var note = x.Value.GetAsString("Note");

                    if (!string.IsNullOrEmpty(phoneNumber))
                    {
                        qFilter = y => y.PhoneNumber == phoneNumber;
                    }

                    if (!string.IsNullOrEmpty(accountName))
                    {
                        if (qFilter == null) qFilter = y => y.Name == accountName;
                        else qFilter = qFilter.And(y => y.Name == accountName);
                    }

                    if (!string.IsNullOrEmpty(note))
                    {
                        if (qFilter == null) qFilter = y => y.Note == note;
                        else qFilter = qFilter.And(y => y.Note == note);
                    }

                    if (qFilter != null)
                    {
                        var customer = Dao.Query(qFilter).FirstOrDefault();
                        if (customer != null)
                            AppServices.MainDataContext.AssignCustomerToSelectedTicket(customer);
                    }
                    else AppServices.MainDataContext.AssignCustomerToSelectedTicket(Customer.Null);
                }

                if (x.Value.Action.ActionType == "UpdateProgramSetting")
                {
                    SettingAccessor.ResetCache();

                    var settingName = x.Value.GetAsString("SettingName");
                    var updateType = x.Value.GetAsString("UpdateType");
                    if (!string.IsNullOrEmpty(settingName))
                    {
                        var isLocal = x.Value.GetAsBoolean("IsLocal");
                        var setting = isLocal
                            ? AppServices.SettingService.ReadLocalSetting(settingName)
                            : AppServices.SettingService.ReadGlobalSetting(settingName);

                        if (updateType == Resources.Increase)
                        {
                            var settingValue = x.Value.GetAsInteger("SettingValue");
                            if (string.IsNullOrEmpty(setting.StringValue))
                                setting.IntegerValue = settingValue;
                            else
                                setting.IntegerValue = setting.IntegerValue + settingValue;
                        }
                        else if (updateType == Resources.Decrease)
                        {
                            var settingValue = x.Value.GetAsInteger("SettingValue");
                            if (string.IsNullOrEmpty(setting.StringValue))
                                setting.IntegerValue = settingValue;
                            else
                                setting.IntegerValue = setting.IntegerValue - settingValue;
                        }
                        else if (updateType == "Multiply")
                        {
                            if (string.IsNullOrEmpty(setting.StringValue))
                                setting.DecimalValue = 0;
                            else
                                setting.DecimalValue = setting.DecimalValue * x.Value.GetAsDecimal("SettingValue");
                        }
                        else if (updateType == "Toggle")
                        {
                            var settingValue = x.Value.GetAsString("SettingValue");
                            var parts = settingValue.Split(',');
                            if (string.IsNullOrEmpty(setting.StringValue))
                            {
                                setting.StringValue = parts[0];
                            }
                            else
                            {
                                for (var i = 0; i < parts.Length; i++)
                                {
                                    if (parts[i] == setting.StringValue)
                                    {
                                        setting.StringValue = (i + 1) < parts.Length ? parts[i + 1] : parts[0];
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var settingValue = x.Value.GetAsString("SettingValue");
                            setting.StringValue = settingValue;
                        }
                        if (!isLocal) AppServices.SettingService.SaveChanges();
                    }
                }

                if (x.Value.Action.ActionType == "RefreshCache")
                {
                    MethodQueue.Queue("ResetCache", ResetCache);
                }

                if (x.Value.Action.ActionType == "SendMessage")
                {
                    AppServices.MessagingService.SendMessage("ActionMessage", x.Value.GetAsString("Command"));
                }

                if (x.Value.Action.ActionType == "SendEmail")
                {
                    EMailService.SendEMailAsync(x.Value.GetAsString("SMTPServer"),
                        x.Value.GetAsString("SMTPUser"),
                        x.Value.GetAsString("SMTPPassword"),
                        x.Value.GetAsInteger("SMTPPort"),
                        x.Value.GetAsString("ToEMailAddress"),
                        x.Value.GetAsString("FromEMailAddress"),
                        x.Value.GetAsString("Subject"),
                        x.Value.GetAsString("EMailMessage"),
                        x.Value.GetAsString("FileName"),
                        x.Value.GetAsBoolean("DeleteFile"),
                        x.Value.GetAsBoolean("BypassSslErrors"));
                }

                if (x.Value.Action.ActionType == "ExecuteTicketEvent")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var operationName = x.Value.GetAsString("TicketOperation");
                        if (operationName == Resources.Refresh)
                            EventServiceFactory.EventService.PublishEvent(EventTopicNames.DisplayTicketView);
                        if (operationName == Resources.Close)
                            ticket.PublishEvent(EventTopicNames.PaymentSubmitted);
                    }
                }


                if (x.Value.Action.ActionType == "UpdateTicketVat")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var vatTemplateName = x.Value.GetAsString("VatTemplate");
                        var vatTemplate = AppServices.MainDataContext.VatTemplates.FirstOrDefault(y => y.Name == vatTemplateName);
                        if (vatTemplate != null)
                        {
                            ticket.UpdateVat(vatTemplate);
                            TicketViewModel.RecalculateTicket(ticket);
                            EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                        }
                    }
                }

                if (x.Value.Action.ActionType == "UpdateTicketTaxService")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var taxServiceTemplateName = x.Value.GetAsString("TaxServiceTemplate");
                        var taxServiceTemplate =
                            AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(
                                y => y.Name == taxServiceTemplateName);
                        if (taxServiceTemplate != null)
                        {
                            var amount = x.Value.GetAsDecimal("Amount");
                            ticket.AddTaxService(taxServiceTemplate.Id, taxServiceTemplate.CalculationMethod, amount);
                            TicketViewModel.RecalculateTicket(ticket);
                        }
                    }
                }

                if (x.Value.Action.ActionType == "RegenerateTicketVat")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        TicketViewModel.RegenerateVatRates(ticket);
                        TicketViewModel.RecalculateTicket(ticket);
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                    }
                }

                if (x.Value.Action.ActionType == "AddTicketDiscount")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var percentValue = x.Value.GetAsDecimal("DiscountPercentage");
                        ticket.AddTicketDiscount(DiscountType.Percent, percentValue, AppServices.CurrentLoggedInUser.Id);
                        TicketViewModel.RecalculateTicket(ticket);
                    }
                }

                if (x.Value.Action.ActionType == "AddTicketItem")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");

                    if (ticket != null)
                    {
                        var menuItemName = x.Value.GetAsString("MenuItemName");
                        var menuItem = AppServices.DataAccessService.GetMenuItemByName(menuItemName);
                        var portionName = x.Value.GetAsString("PortionName");
                        var quantity = x.Value.GetAsDecimal("Quantity");
                        var gifted = x.Value.GetAsBoolean("Gift");
                        var giftReason = x.Value.GetAsString("GiftReason");
                        var tag = x.Value.GetAsString("Tag");

                        var departmentId = AppServices.CurrentTerminal.DepartmentId > 0
                               ? AppServices.MainDataContext.SelectedDepartment.Id
                               : ticket.DepartmentId;

                        var ti = ticket.AddTicketItem(AppServices.CurrentLoggedInUser.Id,
                            departmentId, menuItem, portionName,
                            AppServices.MainDataContext.SelectedDepartment.PriceTag, "");

                        ti.Quantity = quantity;
                        ti.Gifted = gifted;
                        if (gifted && !string.IsNullOrEmpty(giftReason))
                        {
                            var reason = Dao.SingleWithCache<Reason>(u => u.Name == giftReason);
                            if (reason != null) ti.ReasonId = reason.Id;
                        }
                        else
                        {
                            ti.ReasonId = 0;
                        }
                        ti.Tag = tag;

                        TicketViewModel.RecalculateTicket(ticket);
                        EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                    }
                }

                if (x.Value.Action.ActionType == "VoidTicketItems")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var menuItemName = x.Value.GetAsString("MenuItemName");
                        var tag = x.Value.GetAsString("Tag");
                        if (!string.IsNullOrEmpty(menuItemName) && !string.IsNullOrEmpty(tag))
                        {
                            var lines = ticket.TicketItems.Where(y => !y.Voided &&
                                (string.IsNullOrEmpty(menuItemName) || y.MenuItemName.Contains(menuItemName)) &&
                                (y.Tag.Contains(tag) || string.IsNullOrEmpty(tag))).ToList();
                            lines.ForEach(y => ticket.VoidItem(y, 0, AppServices.CurrentLoggedInUser.Id));
                            TicketViewModel.RecalculateTicket(ticket);
                            EventServiceFactory.EventService.PublishEvent(EventTopicNames.RefreshSelectedTicket);
                        }
                    }
                }

                if (x.Value.Action.ActionType == "UpdateTicketTag")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    if (ticket != null)
                    {
                        var tagName = x.Value.GetAsString("TagName");
                        var tagValue = x.Value.GetAsString("TagValue");
                        if (tagValue.Contains(","))
                        {
                            var ctag = ticket.GetTagValue(tagName);
                            if (!string.IsNullOrEmpty(ctag))
                            {
                                var nextTag = tagValue.Split(',').SkipWhile(y => y != ctag).Skip(1).FirstOrDefault();
                                if (string.IsNullOrEmpty(nextTag)) nextTag = tagValue.Split(',')[0];
                                tagValue = nextTag;
                            }
                        }
                        ticket.SetTagValue(tagName, tagValue);
                        var tagData = new TicketTagData { TagName = tagName, TagValue = tagValue };
                        tagData.PublishEvent(EventTopicNames.TagSelectedForSelectedTicket);
                    }
                }

                if (x.Value.Action.ActionType == "UpdatePriceTag")
                {
                    using (var workspace = WorkspaceFactory.Create())
                    {
                        var priceTag = x.Value.GetAsString("PriceTag");
                        var departmentName = x.Value.GetAsString("DepartmentName");
                        var department = workspace.Single<Department>(y => y.Name == departmentName);
                        if (department != null)
                        {
                            department.PriceTag = priceTag;
                            workspace.CommitChanges();
                            MethodQueue.Queue("ResetCache", ResetCache);
                        }
                    }
                }

                if (x.Value.Action.ActionType == "ExecutePrintJob")
                {
                    var ticket = x.Value.GetDataValue<Ticket>("Ticket");
                    var pjName = x.Value.GetAsString("PrintJobName");
                    var ticketItemTag = x.Value.GetAsString("TicketItemTag");

                    if (!string.IsNullOrEmpty(pjName))
                    {
                        var j = AppServices.CurrentTerminal.PrintJobs.SingleOrDefault(y => y.Name == pjName);

                        if (j != null)
                        {
                            if (ticket != null)
                            {
                                AppServices.MainDataContext.UpdateTicketNumber(ticket);
                                if (j.LocksTicket) ticket.RequestLock();

                                var clonedTicket = ObjectCloner.Clone(ticket);
                                clonedTicket.CopyPaidItemsCache(ticket);
                                if (!string.IsNullOrEmpty(ticketItemTag))
                                    clonedTicket.TicketItems =
                                        clonedTicket.TicketItems.Where(y => !string.IsNullOrEmpty(y.Tag) &&
                                                y.Tag.ToLower().Contains(ticketItemTag.Trim().ToLower())).ToList();
                                AppServices.PrintService.ManualPrintTicket(clonedTicket, j);
                            }
                            else
                                AppServices.PrintService.ExecutePrintJob(j);
                        }
                    }
                }
            });
        }

        private static void RegisterNotifiers()
        {
            EventServiceFactory.EventService.GetEvent<GenericEvent<Message>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.MessageReceivedEvent && x.Value.Command == "ActionMessage")
                {
                    RuleExecutor.NotifyEvent(RuleEventNames.MessageReceived, new { Command = x.Value.Data });
                }

                if (x.Topic == EventTopicNames.MessageReceivedEvent && x.Value.Command == "SHUTDOWN")
                {
                    RuleExecutor.NotifyEvent(RuleEventNames.MessageReceived, new { Command = "SHUTDOWN" });
                }
            });

            EventServiceFactory.EventService.GetEvent<GenericEvent<User>>().Subscribe(x =>
            {
                if (x.Topic == EventTopicNames.UserLoggedIn)
                {
                    RuleExecutor.NotifyEvent(RuleEventNames.UserLoggedIn, new { User = x.Value, UserName = x.Value.Name, RoleName = x.Value.UserRole.Name });
                }

                if (x.Topic == EventTopicNames.UserLoggedOut)
                {
                    RuleExecutor.NotifyEvent(RuleEventNames.UserLoggedOut, new { User = x.Value, UserName = x.Value.Name, RoleName = x.Value.UserRole.Name });
                }
            });
        }
    }
}
