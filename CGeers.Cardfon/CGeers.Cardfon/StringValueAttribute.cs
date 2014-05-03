using System;
using System.Linq;
using System.Reflection;

namespace CGeers.Cardfon
{
    // The attribute is valid for type-level targets.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]        
    public sealed class StringValueAttribute : Attribute
    {
        private string value;
        public string Value
        {
            get { return value; }
        }

        public StringValueAttribute(string value)
        {
            this.value = value;
        }

        public static string GetStringValue(object value)
        {
            string result = String.Empty;
            Type type = value.GetType();
            FieldInfo fieldInfo = type.GetField(value.ToString());
            StringValueAttribute[] attributes = 
                fieldInfo.GetCustomAttributes(typeof(StringValueAttribute), false) 
                as StringValueAttribute[];
            if (attributes.Length > 0)
            {
                result = attributes[0].Value;
            }
            return result;
        }
    }        
}
