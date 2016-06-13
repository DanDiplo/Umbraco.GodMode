using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Diplo.GodMode.Helpers;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a repeatable diagnostic section
    /// </summary>
    public class DiagnosticSection
    {
        public DiagnosticSection(string heading)
        {
            this.Heading = heading;
            this.Diagnostics = new List<Diagnostic>();
        }

        public DiagnosticSection(string heading, IEnumerable<Diagnostic> diagnostics) : this(heading)
        {
            this.Diagnostics = diagnostics.ToList();
        }

        public string Heading { get; set; }

        public List<Diagnostic> Diagnostics { get; set; }

        public void AddDiagnostics(NameValueCollection nvc)
        {
            if (nvc != null)
            {
                foreach (var key in nvc.AllKeys)
                {
                    this.Diagnostics.Add(new Diagnostic(key, nvc[key]));
                }
            }
        }

        public void AddDiagnostics(IDictionary<object, object> items)
        {
            if (items != null)
            {
                foreach (var key in items.Keys)
                {
                    this.Diagnostics.Add(new Diagnostic(key.ToString(), items[key].ToString()));
                }
            }
        }

        public void AddDiagnosticsFrom(object obj)
        {
            if (obj != null)
            {
                this.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFrom(obj));
            }
        }
    }
}
