using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Diplo.GodMode.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Extensions;

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

        public void AddDiagnostics(NameValueCollection nvc, bool skipEmpty = true, Func<string, bool> predicate = null)
        {
            if (nvc != null)
            {
                var keys = nvc.AllKeys;

                if (predicate != null)
                {
                    keys = keys.Where(predicate).ToArray();
                }

                foreach (var key in keys)
                {
                    if (skipEmpty && !string.IsNullOrEmpty(nvc[key]) || !skipEmpty)
                    {
                        this.Diagnostics.Add(new Diagnostic(key, nvc[key]));
                    }
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

        public static DiagnosticSection AddDiagnosticSectionFrom(string heading, object obj, bool onlyUmbraco = false)
        {
            var section = new DiagnosticSection(heading);

            if (obj != null)
            {
                section.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFrom(obj, onlyUmbraco));
            }

            return section;
        }

        public static DiagnosticSection AddDiagnosticSectionFrom<T>(string heading, IServiceProvider factory, bool onlyUmbraco = false) where T : class
        {
            var section = new DiagnosticSection(heading);

            var settings = factory.GetRequiredService<IOptions<T>>();

            if (settings != null)
            {
                section.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFrom(settings.Value, onlyUmbraco));
            }

            return section;
        }

        public void AddDiagnosticsFrom(object obj, bool onlyUmbraco = true)
        {
            if (obj != null)
            {
                this.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFrom(obj, onlyUmbraco));
            }
        }

        public void AddDiagnosticsFrom(Type type)
        {
            if (type != null)
            {
                foreach (var item in ReflectionHelper.GetTypesAssignableFrom(type))
                {
                    this.Diagnostics.Add(new Diagnostic(item.Name, item.GetFullNameWithAssembly()));
                }
            }
        }

        public void AddDiagnosticsFromConstant(Type type)
        {
            if (type != null)
            {
                this.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFromConstants(type));
            }
        }

        public static DiagnosticSection AddDiagnosticSectionFrom(string heading, Type type)
        {
            var section = new DiagnosticSection(heading);

            if (type != null)
            {
                foreach (var item in ReflectionHelper.GetTypesAssignableFrom(type))
                {
                    section.Diagnostics.Add(new Diagnostic(item.Name, item.GetFullNameWithAssembly()));
                }
            }

            return section;
        }

        public static DiagnosticSection AddDiagnosticSectionPropertiesFrom(string heading, object obj, string[] ignoreProperties = null)
        {
            var section = new DiagnosticSection(heading);

            if (obj != null)
            {
                var properties = obj.GetType().GetProperties();

                if (ignoreProperties != null)
                {
                    properties = properties.Where(p => !ignoreProperties.Contains(p.Name)).ToArray();
                }

                foreach (var prop in properties)
                {
                    section.Diagnostics.Add(new Diagnostic(prop.Name, prop.GetValue(obj)));
                }
            }

            return section;
        }

        public static DiagnosticSection AddDiagnosticSectionFromConstant(string heading, Type type)
        {
            var section = new DiagnosticSection(heading);

            section.Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFromConstants(type));

            return section;
        }
    }
}