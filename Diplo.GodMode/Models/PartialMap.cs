using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents an HTML partial view mapping
    /// </summary>
    public class PartialMap
    {
        /// <summary>
        /// Gets the ID of the Template the partial belongs to
        /// </summary>
        public int TemplateId { get; set; }

        /// <summary>
        /// Gets the alias of the template the partial belongs to
        /// </summary>
        public string TemplateAlias { get; set; }

        /// <summary>
        /// Gets the partial name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets whether the partial is cached or not
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// Gets the filepath to the file
        /// </summary>
        public string Path { get; set; }
    }
}