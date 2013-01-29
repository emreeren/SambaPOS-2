using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using Samba.Infrastructure.Data;

namespace Samba.Domain.Models.Actions
{
    public class AppAction : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ActionType { get; set; }
        [StringLength(500)]
        public string Parameter { get; set; }

        public string GetParameter(string parameterName)
        {
            var param = Parameter.Split('#').Where(x => x.StartsWith(parameterName + "=")).FirstOrDefault();
            if (!string.IsNullOrEmpty(param) && param.Contains("=")) return param.Split('=')[1];
            return "";
        }

        public string GetFormattedParameter(string parameterName, object dataObject, string parameterValues)
        {
            var format = GetParameter(parameterName);
            return !string.IsNullOrEmpty(format) && format.Contains("[") ? Format(format, dataObject, parameterValues) : format;
        }

        public string Format(string s, object dataObject, string parameterValues)
        {
            if (!string.IsNullOrEmpty(parameterValues) && Regex.IsMatch(parameterValues, "\\[([^\\]]+)\\]"))
            {
                foreach (var propertyName in Regex.Matches(parameterValues, "\\[([^\\]]+)\\]").Cast<Match>().Select(match => match.Groups[1].Value).ToList())
                {
                    var value = dataObject.GetType().GetProperty(propertyName).GetValue(dataObject, null) ?? "";
                    parameterValues = parameterValues.Replace(string.Format("[{0}]", propertyName),
                                             value.ToString());
                }
            }

            var parameters = (parameterValues ?? "")
                .Split('#')
                .Select(y => y.Split('='))
                .Where(x => x.Length > 1)
                .ToDictionary(x => x[0], x => x[1]);

            var matches = Regex.Matches(s, "\\[([^\\]]+)\\]").Cast<Match>()
                .Select(match => match.Groups[1].Value)
                .Where(value => parameters.Keys.Contains(value));

            return matches.Aggregate(s, (current, value) => current.Replace(string.Format("[{0}]", value), parameters[value].ToString()));
        }
    }
}
