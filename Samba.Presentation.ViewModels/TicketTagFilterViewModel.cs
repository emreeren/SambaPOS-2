using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Samba.Infrastructure;
using Samba.Localization.Properties;

namespace Samba.Presentation.ViewModels
{
    public class TicketTagFilterViewModel : IStringCompareable
    {
        public string ButtonDisplay
        {
            get
            {
                var result = Resources.Back;
                if (TagValue == "*") return Resources.All;
                if (TagValue == " ") result = Resources.Empty;
                if (!string.IsNullOrEmpty(TagValue.Trim())) { result = TagValue; }
                if (Count > 0)
                    result += " [" + Count + "]";
                return result;
            }
        }
        public string TagGroup { get; set; }
        public string TagValue { get; set; }
        public int Count { get; set; }
        public string ButtonColor { get; set; }

        public TicketTagFilterViewModel()
        {
            ButtonColor = "Gray";
        }

        public string GetStringValue()
        {
            return TagValue;
        }
    }
}
