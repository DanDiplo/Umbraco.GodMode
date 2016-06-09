using System;
using System.Collections.Generic;

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
            this.BaseType = t.BaseType.Name;
            this.IsUmbraco = this.Module.StartsWith("umbraco", StringComparison.OrdinalIgnoreCase);
        }

        public string Module { get; set; }

        public string Assembly { get; set; }

        public string Name { get; set; }

        public string Namespace { get; set; }

        public string BaseType { get; set; }

        public bool IsUmbraco { get; set; }
    }
}
