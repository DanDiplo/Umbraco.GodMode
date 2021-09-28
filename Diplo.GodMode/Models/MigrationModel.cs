using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents migration information
    /// </summary>
    public class UmbracoKeyValue
    {
        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime Updated { get; set; }

        public string ToDiagnostic()
        {
            return string.Format("{0}: {1} - {2}", this.Key, this.Value, this.Updated);
        }
    }
}
