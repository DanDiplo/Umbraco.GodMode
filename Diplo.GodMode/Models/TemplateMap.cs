using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a template mapping
    /// </summary>
    public class TemplateMap : ItemBase
    {
        public string Path { get; set; }

        public bool IsDefault { get; set; }
    }
}