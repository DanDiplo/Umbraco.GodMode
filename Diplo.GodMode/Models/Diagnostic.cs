using System;
using System.Collections.Generic;
using System.Linq;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents an individual diagnostic item
    /// </summary>
    public class Diagnostic
    {
        public Diagnostic(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public Diagnostic(string key, object value)
        {
            this.Key = key;
            this.Value = value.ToString();
        }

        public Diagnostic(string key, IEnumerable<string> items)
        {
            this.Key = key;
            this.Value = String.Join(", ", items ?? Enumerable.Empty<string>());
        }

        public Diagnostic(string key, IEnumerable<object> items)
        {
            this.Key = key;
            this.Value = String.Join(", ", items ?? Enumerable.Empty<string>());
        }

        public string Key { get; private set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return String.Format("{0}: {1}\n", this.Key, this.Value);
        }
    }
}