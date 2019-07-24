using System;

namespace Diplo.GodMode.Models
{
    public class NameValue
    {
        public NameValue()
        {
        }

        public NameValue(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}