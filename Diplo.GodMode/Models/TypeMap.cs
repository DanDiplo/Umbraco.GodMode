using System;
using System.Reflection;
using Umbraco.Extensions;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a simple mapping from a .NET type
    /// </summary>
    public class TypeMap
    {
        public TypeMap(Type t)
        {
            this.Module = t.Module.Name;
            this.Assembly = t.Assembly.FullName;
            this.Origin = GetOrigin(t.Assembly.GetName().Name);
            this.Name = t.Name;
            this.Namespace = t.Namespace;
            this.BaseType = t.BaseType != null ? t.BaseType.Name : String.Empty;
            this.LoadableName = t.GetFullNameWithAssembly();
            this.IsUmbraco = this.Module.StartsWith("Umbraco.");
        }

        public TypeMap(Assembly a)
        {
            this.Module = a.GetName().Name;
            this.Assembly = a.FullName;
            this.Origin = GetOrigin(a.GetName().Name);
        }

        public string Module { get; set; }

        public string Assembly { get; set; }

        public string Origin { get; set; }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string BaseType { get; set; }

        public string LoadableName { get; set; }

        public bool IsUmbraco { get; set; }

        public int ImplementationCount { get; set; }

        private static string GetOrigin(string? assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return "Unknown";
            }

            if (assemblyName.StartsWith("Umbraco.", StringComparison.OrdinalIgnoreCase) || assemblyName.Equals("Umbraco", StringComparison.OrdinalIgnoreCase))
            {
                return "Umbraco";
            }

            var dotIndex = assemblyName.IndexOf('.');
            return dotIndex > 0 ? assemblyName[..dotIndex] : assemblyName;
        }
    }
}
