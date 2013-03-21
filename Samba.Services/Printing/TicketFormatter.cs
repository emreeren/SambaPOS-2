﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NCalc;
using Samba.Domain.Models.Customers;
using Samba.Domain.Models.Menus;
using Samba.Domain.Models.Settings;
using Samba.Domain.Models.Tickets;
using Samba.Infrastructure.Settings;
using Samba.Localization.Properties;
using Samba.Persistance.Data;

namespace Samba.Services.Printing
{
    public class TagData
    {
        public TagData(string data, string tag)
        {
            data = ReplaceInBracketValues(data, "\r\n", "<newline>", '[', ']');

            data = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Contains(tag)).FirstOrDefault();

            Tag = tag;
            DataString = tag;
            if (string.IsNullOrEmpty(data)) return;

            StartPos = data.IndexOf(tag);
            EndPos = StartPos + 1;

            while (data[EndPos] != '}') { EndPos++; }
            EndPos++;
            Length = EndPos - StartPos;

            DataString = BracketContains(data, '[', ']', Tag) ? GetBracketValue(data, '[', ']') : data.Substring(StartPos, Length);
            DataString = DataString.Replace("<newline>", "\r\n");
            Title = !DataString.StartsWith("[=") ? DataString.Trim('[', ']') : DataString;
            Title = Title.Replace(Tag, "<value>");
            Length = DataString.Length;
            StartPos = data.IndexOf(DataString);
            EndPos = StartPos + Length;
        }

        public string DataString { get; set; }
        public string Tag { get; set; }
        public string Title { get; set; }
        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public int Length { get; set; }

        public static string ReplaceInBracketValues(string content, string find, string replace, char open, char close)
        {
            var result = content;
            var v1 = GetBracketValue(result, open, close);
            while (!string.IsNullOrEmpty(v1))
            {
                var value = v1.Replace(find, replace);
                value = value.Replace(open.ToString(), "<op>");
                value = value.Replace(close.ToString(), "<cl>");
                result = result.Replace(v1, value);
                v1 = GetBracketValue(result, open, close);
            }
            result = result.Replace("<op>", open.ToString());
            result = result.Replace("<cl>", close.ToString());
            return result;
        }

        public static bool BracketContains(string content, char open, char close, string testValue)
        {
            if (!content.Contains(open)) return false;
            var br = GetBracketValue(content, open, close);
            return (br.Contains(testValue)) && !br.StartsWith("[=");
        }

        public static string GetBracketValue(string content, char open, char close)
        {
            var closePass = 1;
            var start = content.IndexOf(open);
            var end = start;
            if (start > -1)
            {
                while (end < content.Length - 1 && closePass > 0)
                {
                    end++;
                    if (content[end] == open && close != open) closePass++;
                    if (content[end] == close) closePass--;
                }
                return content.Substring(start, (end - start) + 1);
            }
            return string.Empty;
        }
    }

    public static class TicketFormatter
    {
        public static string[] GetFormattedTicket(Ticket ticket, IEnumerable<TicketItem> lines, PrinterTemplate template)
        {
            if (template.MergeLines) lines = MergeLines(lines);
            var orderNo = lines.Count() > 0 ? lines.ElementAt(0).OrderNumber : 0;
            var userNo = lines.Count() > 0 ? lines.ElementAt(0).CreatingUserId : 0;
            var departmentNo = lines.Count() > 0 ? lines.ElementAt(0).DepartmentId : ticket.DepartmentId;
            var header = ReplaceDocumentVars(template.HeaderTemplate, ticket, orderNo, userNo, departmentNo);
            var footer = ReplaceDocumentVars(template.FooterTemplate, ticket, orderNo, userNo, departmentNo);
            var lns = GetFormattedLines(lines, template);

            var result = header.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            result.AddRange(lns);
            result.AddRange(footer.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

            return result.ToArray();
        }

        private static IEnumerable<string> GetFormattedLines(IEnumerable<TicketItem> lines, PrinterTemplate template)
        {
            if (!string.IsNullOrEmpty(template.GroupTemplate))
            {
                if (template.GroupTemplate.Contains("{PRODUCT GROUP}"))
                {
                    var groups = lines.GroupBy(GetMenuItemGroup);
                    var result = new List<string>();
                    foreach (var grp in groups)
                    {
                        var grpSep = template.GroupTemplate.Replace("{PRODUCT GROUP}", grp.Key);
                        result.AddRange(grpSep.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                        result.AddRange(grp.SelectMany(x => FormatLines(template, x).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
                    }
                    return result;
                }

                if (template.GroupTemplate.Contains("{PRODUCT TAG}"))
                {
                    var groups = lines.GroupBy(GetMenuItemTag);
                    var result = new List<string>();
                    foreach (var grp in groups)
                    {
                        var grpSep = template.GroupTemplate.Replace("{PRODUCT TAG}", grp.Key);
                        result.AddRange(grpSep.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                        result.AddRange(grp.SelectMany(x => FormatLines(template, x).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
                    }
                    return result;
                }

                if (template.GroupTemplate.Contains("{ITEM TAG}"))
                {
                    var groups = lines.GroupBy(x => (x.Tag ?? "").Split('|')[0]);
                    var result = new List<string>();
                    foreach (var grp in groups)
                    {
                        var grpSep = template.GroupTemplate.Replace("{ITEM TAG}", grp.Key);
                        result.AddRange(grpSep.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                        result.AddRange(grp.SelectMany(x => FormatLines(template, x).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
                    }
                    return result;
                }

                if (template.GroupTemplate.Contains("{ORDER NO}"))
                {
                    var groups = lines.GroupBy(x => x.OrderNumber);
                    var result = new List<string>();
                    foreach (var grp in groups)
                    {
                        var grpSep = template.GroupTemplate.Replace("{ORDER NO}", grp.Key.ToString());
                        result.AddRange(grpSep.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                        result.AddRange(grp.SelectMany(x => FormatLines(template, x).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)));
                    }
                    return result;
                }
            }
            return lines.SelectMany(x => FormatLines(template, x).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).ToArray();
        }

        private static IEnumerable<TicketItem> MergeLines(IEnumerable<TicketItem> lines)
        {
            var group = lines.Where(x => x.Properties.Count == 0).GroupBy(x => new
                                                {
                                                    x.MenuItemId,
                                                    x.MenuItemName,
                                                    x.Voided,
                                                    x.Gifted,
                                                    x.Price,
                                                    x.VatIncluded,
                                                    x.VatAmount,
                                                    x.VatTemplateId,
                                                    x.PortionName,
                                                    x.PortionCount,
                                                    x.ReasonId,
                                                    x.CurrencyCode,
                                                    x.Tag
                                                });

            var result = group.Select(x => new TicketItem
                                    {
                                        MenuItemId = x.Key.MenuItemId,
                                        MenuItemName = x.Key.MenuItemName,
                                        ReasonId = x.Key.ReasonId,
                                        Voided = x.Key.Voided,
                                        Gifted = x.Key.Gifted,
                                        Price = x.Key.Price,
                                        VatAmount = x.Key.VatAmount,
                                        VatTemplateId = x.Key.VatTemplateId,
                                        VatIncluded = x.Key.VatIncluded,
                                        CreatedDateTime = x.Last().CreatedDateTime,
                                        CreatingUserId = x.Last().CreatingUserId,
                                        OrderNumber = x.Last().OrderNumber,
                                        TicketId = x.Last().TicketId,
                                        PortionName = x.Key.PortionName,
                                        PortionCount = x.Key.PortionCount,
                                        CurrencyCode = x.Key.CurrencyCode,
                                        Tag = x.Key.Tag,
                                        Quantity = x.Sum(y => y.Quantity)
                                    });

            result = result.Union(lines.Where(x => x.Properties.Count > 0)).OrderBy(x => x.CreatedDateTime);

            return result;
        }

        private static string ReplaceDocumentVars(string document, Ticket ticket, int orderNo, int userNo, int departmentNo)
        {
            string result = document;
            if (string.IsNullOrEmpty(document)) return "";
            result = FormatData(result, Resources.TF_TerminalName, () => AppServices.CurrentTerminal.Name);
            result = FormatData(result, Resources.TF_TicketDate, () => ticket.Date.ToShortDateString());
            result = FormatData(result, Resources.TF_TicketTime, () => ticket.Date.ToShortTimeString());
            result = FormatData(result, Resources.TF_DayDate, () => DateTime.Now.ToShortDateString());
            result = FormatData(result, Resources.TF_DayTime, () => DateTime.Now.ToShortTimeString());
            result = FormatData(result, Resources.TF_UniqueTicketId, () => ticket.Id.ToString());
            result = FormatData(result, Resources.TF_TicketNumber, () => ticket.TicketNumber);
            result = FormatData(result, Resources.TF_LineOrderNumber, orderNo.ToString);
            result = FormatData(result, Resources.TF_TicketTag, ticket.GetTagData);
            result = FormatDataIf(true, result, "{DEPARTMENT}", () => GetDepartmentName(departmentNo));

            var ticketTagPattern = Resources.TF_OptionalTicketTag + "[^}]+}";

            while (Regex.IsMatch(result, ticketTagPattern))
            {
                var value = Regex.Match(result, ticketTagPattern).Groups[0].Value;
                var tags = "";
                try
                {
                    var tag = value.Trim('{', '}').Split(':')[1];
                    tags = tag.Split(',').Aggregate(tags, (current, t) => current +
                        (!string.IsNullOrEmpty(ticket.GetTagValue(t.Trim()))
                        ? (t + ": " + ticket.GetTagValue(t.Trim()) + "\r")
                        : ""));
                    result = FormatData(result.Trim('\r'), value, () => tags);
                }
                catch (Exception)
                {
                    result = FormatData(result, value, () => "");
                }
            }

            const string ticketTag2Pattern = "{TICKETTAG:[^}]+}";

            while (Regex.IsMatch(result, ticketTag2Pattern))
            {
                var value = Regex.Match(result, ticketTag2Pattern).Groups[0].Value;
                var tag = value.Trim('{', '}').Split(':')[1];
                var tagValue = ticket.GetTagValue(tag);
                try
                {
                    result = FormatData(result, value, () => tagValue);
                }
                catch (Exception)
                {
                    result = FormatData(result, value, () => "");
                }
            }

            var userName = AppServices.MainDataContext.GetUserName(userNo);

            var title = ticket.LocationName;
            if (string.IsNullOrEmpty(ticket.LocationName))
                title = userName;

            result = FormatData(result, Resources.TF_TableOrUserName, () => title);
            result = FormatData(result, Resources.TF_UserName, () => userName);
            result = FormatData(result, Resources.TF_TableName, () => ticket.LocationName);
            result = FormatData(result, Resources.TF_TicketNote, () => ticket.Note ?? "");
            result = FormatData(result, Resources.TF_AccountName, () => ticket.CustomerName);
            result = FormatData(result, "{ACC GROUPCODE}", () => ticket.CustomerGroupCode);

            if (ticket.CustomerId > 0 && (result.Contains(Resources.TF_AccountAddress) || result.Contains(Resources.TF_AccountPhone) || result.Contains("{ACC NOTE}")))
            {
                var customer = Dao.SingleWithCache<Customer>(x => x.Id == ticket.CustomerId);
                result = FormatData(result, Resources.TF_AccountAddress, () => customer.Address);
                result = FormatData(result, Resources.TF_AccountPhone, () => customer.PhoneNumber);
                result = FormatData(result, "{ACC NOTE}", () => customer.Note);
            }

            if (ticket.CustomerId > 0 && result.Contains("{ACC BALANCE}"))
            {
                var accBalance = CashService.GetAccountBalance(ticket.CustomerId);
                result = FormatDataIf(accBalance != 0, result, "{ACC BALANCE}", () => accBalance.ToString("#,#0.00"));
            }

            result = RemoveTag(result, Resources.TF_AccountAddress);
            result = RemoveTag(result, Resources.TF_AccountPhone);

            var payment = ticket.GetPaymentAmount();
            var remaining = ticket.GetRemainingAmount();
            var discount = ticket.GetDiscountAndRoundingTotal();
            var plainTotal = ticket.GetPlainSum();
            var giftAmount = ticket.GetTotalGiftAmount();
            var vatAmount = GetTaxTotal(ticket.TicketItems, plainTotal, ticket.GetDiscountTotal());
            var taxServicesTotal = ticket.GetTaxServicesTotal();
            var ticketPaymentAmount = ticket.GetPaymentAmount();

            result = FormatDataIf(vatAmount > 0 || discount > 0 || taxServicesTotal > 0, result, "{PLAIN TOTAL}", () => plainTotal.ToString("#,#0.00"));
            result = FormatDataIf(discount > 0, result, "{DISCOUNT TOTAL}", () => discount.ToString("#,#0.00"));
            result = FormatDataIf(vatAmount > 0, result, "{TAX TOTAL}", () => vatAmount.ToString("#,#0.00"));
            result = FormatDataIf(taxServicesTotal > 0, result, "{SERVICE TOTAL}", () => taxServicesTotal.ToString("#,#0.00"));
            result = FormatDataIf(vatAmount > 0, result, "{TAX DETAILS}", () => GetTaxDetails(ticket.TicketItems, plainTotal, discount));
            result = FormatDataIf(taxServicesTotal > 0, result, "{SERVICE DETAILS}", () => GetServiceDetails(ticket));

            result = FormatDataIf(payment > 0, result, Resources.TF_RemainingAmountIfPaid,
                () => string.Format(Resources.RemainingAmountIfPaidValue_f, payment.ToString("#,#0.00"), remaining.ToString("#,#0.00")));

            result = FormatDataIf(discount > 0, result, Resources.TF_DiscountTotalAndTicketTotal,
                () => string.Format(Resources.DiscountTotalAndTicketTotalValue_f, (plainTotal).ToString("#,#0.00"), discount.ToString("#,#0.00")));

            result = FormatDataIf(giftAmount > 0, result, Resources.TF_GiftTotal, () => giftAmount.ToString("#,#0.00"));
            result = FormatDataIf(discount < 0, result, Resources.TF_IfFlatten, () => string.Format(Resources.IfNegativeDiscountValue_f, discount.ToString("#,#0.00")));

            result = FormatData(result, Resources.TF_TicketTotal, () => ticket.GetSum().ToString("#,#0.00"));

            result = FormatDataIf(ticketPaymentAmount > 0, result, Resources.TF_TicketPaidTotal, () => ticketPaymentAmount.ToString("#,#0.00"));
            result = FormatData(result, Resources.TF_TicketRemainingAmount, () => ticket.GetRemainingAmount().ToString("#,#0.00"));

            result = FormatData(result, "{TOTAL TEXT}", () => HumanFriendlyInteger.CurrencyToWritten(ticket.GetSum()));
            result = FormatData(result, "{TOTALTEXT}", () => HumanFriendlyInteger.CurrencyToWritten(ticket.GetSum(), true));

            result = UpdateGlobalValues(result);

            return result;
        }

        private static string UpdateGlobalValues(string data)
        {
            data = UpdateSettings(data);
            data = UpdateExpressions(data);

            return data;
        }

        private static string UpdateSettings(string result)
        {
            while (Regex.IsMatch(result, "{SETTING:[^}]+}", RegexOptions.Singleline))
            {
                var match = Regex.Match(result, "{SETTING:([^}]+)}");
                var tagName = match.Groups[0].Value;
                var settingName = match.Groups[1].Value;
                var tagData = new TagData(result, tagName);
                var value = !string.IsNullOrEmpty(settingName) ? AppServices.SettingService.ReadSetting(settingName).StringValue : "";
                var replace = !string.IsNullOrEmpty(value) ? tagData.Title.Replace("<value>", value) : "";
                result = result.Replace(tagData.DataString, replace);
            }
            return result;
        }

        private static string UpdateExpressions(string data)
        {
            while (Regex.IsMatch(data, "\\[=[^\\]]+\\]", RegexOptions.Singleline))
            {
                var match = Regex.Match(data, "\\[=([^\\]]+)\\]");
                var tag = match.Groups[0].Value;
                var expression = match.Groups[1].Value;
                var e = new Expression(expression);
                e.EvaluateFunction += delegate(string name, FunctionArgs args)
                                          {
                                              if (name == "Format" || name == "F")
                                              {
                                                  var fmt = args.Parameters.Length > 1
                                                                ? args.Parameters[1].Evaluate().ToString()
                                                                : "#,#0.00";
                                                  args.Result = ((double)args.Parameters[0].Evaluate()).ToString(fmt);
                                              }
                                              if (name == "ToNumber" || name == "TN")
                                              {
                                                  double d;
                                                  double.TryParse(args.Parameters[0].Evaluate().ToString(), NumberStyles.Any, CultureInfo.CurrentCulture, out d);
                                                  args.Result = d;
                                              }
                                          };
                string result;
                try
                {
                    result = e.Evaluate().ToString();
                }
                catch (EvaluationException)
                {
                    result = "";
                }

                data = data.Replace(tag, result);
            }

            return data;
        }

        private static string GetDepartmentName(int departmentId)
        {
            var dep = AppServices.MainDataContext.Departments.SingleOrDefault(x => x.Id == departmentId);
            return dep != null ? dep.Name : Resources.UndefinedWithBrackets;
        }

        private static string GetServiceDetails(Ticket ticket)
        {
            var sb = new StringBuilder();
            foreach (var taxService in ticket.TaxServices)
            {
                var service = taxService;
                var ts = AppServices.MainDataContext.TaxServiceTemplates.FirstOrDefault(x => x.Id == service.TaxServiceId);
                var tsTitle = ts != null ? ts.Name : Resources.UndefinedWithBrackets;
                sb.AppendLine("<J>" + tsTitle + ":|" + service.CalculationAmount.ToString("#,#0.00"));
            }
            return string.Join("\r", sb);
        }

        private static string GetTaxDetails(IEnumerable<TicketItem> ticketItems, decimal plainSum, decimal discount)
        {
            var sb = new StringBuilder();
            var groups = ticketItems.Where(x => x.VatTemplateId > 0).GroupBy(x => x.VatTemplateId);
            foreach (var @group in groups)
            {
                var iGroup = @group;
                var tb = AppServices.MainDataContext.VatTemplates.FirstOrDefault(x => x.Id == iGroup.Key);
                var tbTitle = tb != null ? tb.Name : Resources.UndefinedWithBrackets;
                var total = @group.Sum(x => x.GetTotalVatAmount());
                if (discount > 0)
                {
                    total -= (total * discount) / plainSum;
                }
                if (total > 0) sb.AppendLine("<J>" + tbTitle + ":|" + total.ToString("#,#0.00"));
            }
            return string.Join("\r", sb);
        }

        private static decimal GetTaxTotal(IEnumerable<TicketItem> ticketItems, decimal plainSum, decimal discount)
        {
            var result = ticketItems.Sum(x => x.GetTotalVatAmount());
            if (discount > 0)
            {
                result -= (result * discount) / plainSum;
            }
            return result;
        }

        private static string FormatData(string data, string tag, Func<string> valueFunc)
        {
            if (!data.Contains(tag)) return data;

            var i = 0;
            while (data.Contains(tag) && i < 99)
            {
                var value = valueFunc.Invoke();
                var tagData = new TagData(data, tag);
                if (!string.IsNullOrEmpty(value)) value =
                    !string.IsNullOrEmpty(tagData.Title) && tagData.Title.Contains("<value>")
                    ? tagData.Title.Replace("<value>", value)
                    : tagData.Title + value;
                var spos = data.IndexOf(tagData.DataString);
                data = data.Remove(spos, tagData.Length).Insert(spos, value ?? "");
                i++;
            }
            return data;
        }

        private static string FormatDataIf(bool condition, string data, string tag, Func<string> valueFunc)
        {
            if (condition && data.Contains(tag))
            {
                Func<string> value = valueFunc.Invoke;
                data = FormatData(data, tag, value);
                return data;
            }
            return RemoveTag(data, tag);
        }

        private static string RemoveTag(string data, string tag)
        {
            var i = 0;
            while (data.Contains(tag) && i < 99)
            {
                var tagData = new TagData(data, tag);
                var spos = data.IndexOf(tagData.DataString);
                data = data.Remove(spos, tagData.Length);
                i++;
            }
            return data;
        }

        private static string FormatLines(PrinterTemplate template, TicketItem ticketItem)
        {
            if (ticketItem.Gifted)
            {
                if (!string.IsNullOrEmpty(template.GiftLineTemplate))
                {
                    return template.GiftLineTemplate.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Aggregate("", (current, s) => current + ReplaceLineVars(s, ticketItem));
                }
                return "";
            }

            if (ticketItem.Voided)
            {
                if (!string.IsNullOrEmpty(template.VoidedLineTemplate))
                {
                    return template.VoidedLineTemplate.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Aggregate("", (current, s) => current + ReplaceLineVars(s, ticketItem));
                }
                return "";
            }

            if (!string.IsNullOrEmpty(template.LineTemplate))
                return template.LineTemplate.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Aggregate("", (current, s) => current + ReplaceLineVars(s, ticketItem));
            return "";
        }

        private static string ReplaceLineVars(string line, TicketItem ticketItem)
        {
            string result = line;

            if (ticketItem != null)
            {
                result = FormatData(result, Resources.TF_LineItemQuantity, () => ticketItem.Quantity.ToString("#,#0.##"));
                result = FormatData(result, Resources.TF_LineItemName, () => ticketItem.MenuItemName + ticketItem.GetPortionDesc());
                result = FormatData(result, Resources.TF_LineItemPrice, () => ticketItem.Price.ToString("#,#0.00"));
                result = FormatData(result, Resources.TF_LineItemTotal, () => ticketItem.GetItemPrice().ToString("#,#0.00"));
                result = FormatData(result, Resources.TF_LineItemTotalAndQuantity, () => ticketItem.GetItemValue().ToString("#,#0.00"));
                result = FormatData(result, Resources.TF_LineItemPriceCents, () => (ticketItem.Price * 100).ToString("#,##"));
                result = FormatData(result, Resources.TF_LineItemTotalWithoutGifts, () => ticketItem.GetTotal().ToString("#,#0.00"));
                result = FormatData(result, Resources.TF_LineOrderNumber, () => ticketItem.OrderNumber.ToString());
                result = FormatData(result, Resources.TF_LineGiftOrVoidReason, () => AppServices.MainDataContext.GetReason(ticketItem.ReasonId));
                result = FormatData(result, "{MENU ITEM GROUP}", () => GetMenuItemGroup(ticketItem));
                result = FormatData(result, "{MENU ITEM TAG}", () => GetMenuItemTag(ticketItem));
                result = FormatData(result, "{PRICE TAG}", () => ticketItem.PriceTag);
                result = FormatData(result, "{ITEM TAG}", () => ticketItem.Tag);

                while (Regex.IsMatch(result, "{ITEM TAG:[^}]+}", RegexOptions.Singleline))
                {
                    var tags = ticketItem.Tag.Split('|');
                    var match = Regex.Match(result, "{ITEM TAG:([^}]+)}");
                    var tagName = match.Groups[0].Value;
                    int index;
                    int.TryParse(match.Groups[1].Value, out index);
                    var value = tags.Count() > index ? tags[index].Trim() : "";
                    result = result.Replace(tagName, value);
                }

                if (result.Contains(Resources.TF_LineItemDetails.Substring(0, Resources.TF_LineItemDetails.Length - 1)))
                {
                    string lineFormat = result;
                    if (ticketItem.Properties.Count > 0)
                    {
                        string label = "";
                        foreach (var property in ticketItem.Properties)
                        {
                            var itemProperty = property;
                            var lineValue = FormatData(lineFormat, Resources.TF_LineItemDetails, () => itemProperty.Name);
                            lineValue = FormatData(lineValue, Resources.TF_LineItemDetailQuantity, () => itemProperty.Quantity.ToString("#.##"));
                            lineValue = FormatData(lineValue, Resources.TF_LineItemDetailPrice, () => itemProperty.CalculateWithParentPrice ? "" : itemProperty.PropertyPrice.Amount.ToString("#,#0.00"));
                            label += lineValue + "\r\n";
                        }
                        result = "\r\n" + label;
                    }
                    else result = "";
                }

                result = UpdateGlobalValues(result);
                result = result.Replace("<", "\r\n<");
            }
            return result;
        }

        private static string GetMenuItemGroup(TicketItem ticketItem)
        {
            return Dao.SingleWithCache<MenuItem>(x => x.Id == ticketItem.MenuItemId).GroupCode;
        }

        private static string GetMenuItemTag(TicketItem ticketItem)
        {
            var result = Dao.SingleWithCache<MenuItem>(x => x.Id == ticketItem.MenuItemId).Tag;
            if (string.IsNullOrEmpty(result)) result = ticketItem.MenuItemName;
            return result;
        }
    }

    public static class HumanFriendlyInteger
    {
        static readonly string[] Ones = new[] { "", Resources.One, Resources.Two, Resources.Three, Resources.Four, Resources.Five, Resources.Six, Resources.Seven, Resources.Eight, Resources.Nine };
        static readonly string[] Teens = new[] { Resources.Ten, Resources.Eleven, Resources.Twelve, Resources.Thirteen, Resources.Fourteen, Resources.Fifteen, Resources.Sixteen, Resources.Seventeen, Resources.Eighteen, Resources.Nineteen };
        static readonly string[] Tens = new[] { Resources.Twenty, Resources.Thirty, Resources.Forty, Resources.Fifty, Resources.Sixty, Resources.Seventy, Resources.Eighty, Resources.Ninety };
        static readonly string[] ThousandsGroups = { "", " " + Resources.Thousand, " " + Resources.Million, " " + Resources.Billion };

        private static string FriendlyInteger(int n, string leftDigits, int thousands)
        {
            if (n == 0)
            {
                return leftDigits;
            }
            string friendlyInt = leftDigits;
            if (friendlyInt.Length > 0)
            {
                friendlyInt += " ";
            }
            if (n < 10)
            {
                friendlyInt += Ones[n];
            }
            else if (n < 20)
            {
                friendlyInt += Teens[n - 10];
            }
            else if (n < 100)
            {
                friendlyInt += FriendlyInteger(n % 10, Tens[n / 10 - 2], 0);
            }
            else if (n < 1000)
            {
                var t = Ones[n / 100] + " " + Resources.Hundred;
                if (n / 100 == 1) t = Resources.OneHundred;
                friendlyInt += FriendlyInteger(n % 100, t, 0);
            }
            else if (n < 10000 && thousands == 0)
            {
                var t = Ones[n / 1000] + " " + Resources.Thousand;
                if (n / 1000 == 1) t = Resources.OneThousand;
                friendlyInt += FriendlyInteger(n % 1000, t, 0);
            }
            else
            {
                friendlyInt += FriendlyInteger(n % 1000, FriendlyInteger(n / 1000, "", thousands + 1), 0);
            }

            return friendlyInt + ThousandsGroups[thousands];
        }

        public static string CurrencyToWritten(decimal d, bool upper = false)
        {
            var result = "";
            var fraction = d - Math.Floor(d);
            var value = d - fraction;
            if (value > 0)
            {
                var start = IntegerToWritten(Convert.ToInt32(value));
                if (upper) start = start.Replace(" ", "").ToUpper();
                result += string.Format("{0} {1} ", start, LocalSettings.MajorCurrencyName + GetPlural(value));
            }

            if (fraction > 0)
            {
                var end = IntegerToWritten(Convert.ToInt32(fraction * 100));
                if (upper) end = end.Replace(" ", "").ToUpper();
                result += string.Format("{0} {1} ", end, LocalSettings.MinorCurrencyName + GetPlural(fraction));
            }
            return result.Replace("  ", " ").Trim();
        }

        private static string GetPlural(decimal number)
        {
            return number == 1 ? "" : LocalSettings.PluralCurrencySuffix;
        }

        public static string IntegerToWritten(int n)
        {
            if (n == 0)
            {
                return Resources.Zero;
            }
            if (n < 0)
            {
                return Resources.Negative + " " + IntegerToWritten(-n);
            }

            return FriendlyInteger(n, "", 0);
        }

    }
}
