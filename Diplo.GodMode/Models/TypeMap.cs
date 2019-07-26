using System;
using System.Reflection;
using Umbraco.Core;

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
        }

        public string Module { get; set; }

        public string Assembly { get; set; }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string BaseType { get; set; }

        public string LoadableName { get; set; }

        public bool IsUmbraco { get; set; }
    }
}