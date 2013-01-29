using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Samba.Domain.Models.Actions;
using Samba.Infrastructure.Data.Serializer;
using Samba.Services;

namespace Samba.Presentation.Common
{
    public class ActionData
    {
        public AppAction Action { get; set; }
        public string ParameterValues { get; set; }
        public object DataObject { get; set; }

        public T GetDataValue<T>(string parameterName) where T : class
        {
            var property = DataObject.GetType().GetProperty(parameterName);
            if (property != null)
                return property.GetValue(DataObject, null) as T;
            return null;
        }

        public bool GetAsBoolean(string parameterName)
        {
            bool result;
            bool.TryParse(GetAsString(parameterName), out result);
            return result;
        }

        public string GetAsString(string parameterName)
        {
            return Action.GetFormattedParameter(parameterName, DataObject, ParameterValues);
        }

        public decimal GetAsDecimal(string parameterName)
        {
            decimal result;
            decimal.TryParse(GetAsString(parameterName), out result);
            return result;
        }

        public int GetAsInteger(string parameterName)
        {
            int result;
            int.TryParse(GetAsString(parameterName), out result);
            return result;
        }
    }

    public static class SettingAccessor
    {
        private static readonly IDictionary<string, string> Cache = new Dictionary<string, string>();

        public static void ResetCache()
        {
            Cache.Clear();
        }

        public static string ReplaceSettings(string value)
        {
            if (value == null) return "";
            while (Regex.IsMatch(value, "\\{:[^}]+\\}", RegexOptions.Singleline))
            {
                var match = Regex.Match(value, "\\{:([^}]+)\\}");
                var tagName = match.Groups[0].Value;
                var settingName = match.Groups[1].Value;
                if (!Cache.ContainsKey(settingName))
                    Cache.Add(settingName, AppServices.SettingService.ReadSetting(settingName).StringValue);
                var settingValue = Cache[settingName];
                value = value.Replace(tagName, settingValue);
            }
            return value;
        }
    }

    public static class RuleExecutor
    {
        public static void NotifyEvent(string eventName, object dataObject)
        {
            SettingAccessor.ResetCache();
            var rules = AppServices.MainDataContext.Rules.Where(x => x.EventName == eventName);
            foreach (var rule in rules.Where(x => string.IsNullOrEmpty(x.EventConstraints) || SatisfiesConditions(x, dataObject)))
            {
                foreach (var actionContainer in rule.Actions)
                {
                    var container = actionContainer;
                    var action = AppServices.MainDataContext.Actions.SingleOrDefault(x => x.Id == container.AppActionId);
                    if (action != null)
                    {
                        var clonedAction = ObjectCloner.Clone(action);
                        var containerParameterValues = container.ParameterValues ?? "";
                        clonedAction.Parameter = SettingAccessor.ReplaceSettings(clonedAction.Parameter);
                        containerParameterValues = SettingAccessor.ReplaceSettings(containerParameterValues);
                        var data = new ActionData { Action = clonedAction, DataObject = dataObject, ParameterValues = containerParameterValues };
                        data.PublishEvent(EventTopicNames.ExecuteEvent, true);
                    }
                }
            }
        }

        private static bool SatisfiesConditions(AppRule appRule, object dataObject)
        {
            var constraints = SettingAccessor.ReplaceSettings(appRule.EventConstraints);

            var conditions = constraints.Split('#')
                .Select(x => new RuleConstraintViewModel(x));

            var parameterNames = dataObject.GetType().GetProperties().Select(x => x.Name);

            foreach (var condition in conditions)
            {
                var parameterName = parameterNames.FirstOrDefault(condition.Name.Equals);

                if (!string.IsNullOrEmpty(parameterName))
                {
                    var property = dataObject.GetType().GetProperty(parameterName);
                    var parameterValue = property.GetValue(dataObject, null) ?? "";
                    if (!condition.ValueEquals(parameterValue)) return false;
                }
                else
                {
                    if (condition.Name.StartsWith("SN$"))
                    {
                        var settingName = condition.Name.Replace("SN$", "");
                        while (Regex.IsMatch(settingName, "\\[[^\\]]+\\]"))
                        {
                            var paramvalue = Regex.Match(settingName, "\\[[^\\]]+\\]").Groups[0].Value;
                            var insideValue = paramvalue.Trim(new[] { '[', ']' });
                            if (parameterNames.Contains(insideValue))
                            {
                                var v = dataObject.GetType().GetProperty(insideValue).GetValue(dataObject, null).ToString();
                                settingName = settingName.Replace(paramvalue, v);
                            }
                            else
                            {
                                if (paramvalue == "[Day]")
                                    settingName = settingName.Replace(paramvalue, DateTime.Now.Day.ToString());
                                else if (paramvalue == "[Month]")
                                    settingName = settingName.Replace(paramvalue, DateTime.Now.Month.ToString());
                                else if (paramvalue == "[Year]")
                                    settingName = settingName.Replace(paramvalue, DateTime.Now.Year.ToString());
                                else settingName = settingName.Replace(paramvalue, "");
                            }
                        }

                        var customSettingValue = AppServices.SettingService.ReadSetting(settingName).StringValue ?? "";
                        if (!condition.ValueEquals(customSettingValue)) return false;
                    }
                    if (condition.Name == "TerminalName" && !string.IsNullOrEmpty(condition.Value))
                    {
                        if (!condition.Value.Equals(AppServices.CurrentTerminal.Name))
                        {
                            return false;
                        }
                    }
                    if (condition.Name == "DepartmentName" && !string.IsNullOrEmpty(condition.Value))
                    {
                        if (AppServices.MainDataContext.SelectedDepartment == null ||
                            !condition.Value.Equals(AppServices.MainDataContext.SelectedDepartment.Name))
                        {
                            return false;
                        }
                    }

                    if (condition.Name == "UserName" && !string.IsNullOrEmpty(condition.Value))
                    {
                        if (AppServices.CurrentLoggedInUser == null ||
                            !condition.Value.Equals(AppServices.CurrentLoggedInUser.Name))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

    }
}
