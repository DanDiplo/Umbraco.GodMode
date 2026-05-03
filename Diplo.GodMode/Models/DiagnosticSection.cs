using System.Collections.Specialized;
using Diplo.GodMode.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Extensions;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a repeatable diagnostic section.
    /// </summary>
    public class DiagnosticSection
    {
        public DiagnosticSection(string heading)
        {
            Heading = heading;
        }

        public DiagnosticSection(string heading, IEnumerable<Diagnostic> diagnostics)
        {
            Heading = heading;
            Diagnostics = diagnostics?.ToList() ?? [];
        }

        public string Heading { get; }

        public List<Diagnostic> Diagnostics { get; } = [];

        public DiagnosticSection Add(string key, object value)
        {
            Diagnostics.Add(new Diagnostic(key, value));
            return this;
        }

        public DiagnosticSection Add(string key, string value)
        {
            Diagnostics.Add(new Diagnostic(key, value));
            return this;
        }

        public DiagnosticSection AddRange(IEnumerable<Diagnostic> diagnostics)
        {
            if (diagnostics != null)
            {
                Diagnostics.AddRange(diagnostics);
            }

            return this;
        }

        public DiagnosticSection AddDiagnostics(NameValueCollection nvc, bool skipEmpty = true, Func<string, bool> predicate = null)
        {
            if (nvc == null)
            {
                return this;
            }

            var keys = nvc.AllKeys;

            if (predicate != null)
            {
                keys = keys.Where(predicate).ToArray();
            }

            foreach (var key in keys)
            {
                if (key == null)
                {
                    continue;
                }

                if ((skipEmpty && !string.IsNullOrEmpty(nvc[key])) || !skipEmpty)
                {
                    Diagnostics.Add(new Diagnostic(key, nvc[key]));
                }
            }

            return this;
        }

        public DiagnosticSection AddDiagnostics(IDictionary<object, object> items)
        {
            if (items == null)
            {
                return this;
            }

            foreach (var key in items.Keys)
            {
                Diagnostics.Add(new Diagnostic(key?.ToString() ?? string.Empty, items[key]));
            }

            return this;
        }

        public DiagnosticSection AddDiagnosticsFrom(object obj, bool onlyUmbraco = true)
        {
            if (obj != null)
            {
                Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFrom(obj, onlyUmbraco));
            }

            return this;
        }

        public DiagnosticSection AddDiagnosticsFrom(Type type)
        {
            if (type == null)
            {
                return this;
            }

            foreach (var item in ReflectionHelper.GetTypesAssignableFrom(type))
            {
                Diagnostics.Add(new Diagnostic(item.Name, item.GetFullNameWithAssembly()));
            }

            return this;
        }

        public DiagnosticSection AddDiagnosticsFromConstant(Type type)
        {
            if (type != null)
            {
                Diagnostics.AddRange(ReflectionHelper.PopulateDiagnosticsFromConstants(type));
            }

            return this;
        }

        public static DiagnosticSection From(string heading, object obj, bool onlyUmbraco = false)
        {
            return new DiagnosticSection(heading)
                .AddDiagnosticsFrom(obj, onlyUmbraco);
        }

        public static DiagnosticSection FromOptions<T>(string heading, IServiceProvider services, bool onlyUmbraco = false)
            where T : class
        {
            var settings = services.GetRequiredService<IOptions<T>>();

            return From(heading, settings.Value, onlyUmbraco);
        }

        public static DiagnosticSection FromAssignableTypes(string heading, Type type)
        {
            return new DiagnosticSection(heading)
                .AddDiagnosticsFrom(type);
        }

        public static DiagnosticSection FromConstants(string heading, Type type)
        {
            return new DiagnosticSection(heading)
                .AddDiagnosticsFromConstant(type);
        }

        public static DiagnosticSection FromProperties(string heading, object obj, string[] ignoreProperties = null)
        {
            var section = new DiagnosticSection(heading);

            if (obj == null)
            {
                return section;
            }

            var properties = obj.GetType().GetProperties();

            if (ignoreProperties != null)
            {
                properties = properties
                    .Where(p => !ignoreProperties.Contains(p.Name))
                    .ToArray();
            }

            foreach (var prop in properties)
            {
                section.Diagnostics.Add(new Diagnostic(prop.Name, prop.GetValue(obj)));
            }

            return section;
        }
    }
}