using System;
using Umbraco.Core.Models;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a property type mapping
    /// </summary>
    public class PropertyTypeMap : ItemBase
    {
        public PropertyTypeMap()
        {
        }

        /// <summary>
        /// Instantiates a property type map from an Umbraco property type
        /// </summary>
        /// <param name="pt">The Umbraco property type</param>
        public PropertyTypeMap(PropertyType pt)
        {
            this.Alias = pt.Alias;
            this.Description = pt.Description;
            this.EditorAlias = pt.PropertyEditorAlias;
            this.EditorId = pt.DataTypeId;
            this.Id = pt.Id;
            this.Name = pt.Name;
            this.Description = pt.Description;
            this.VariesBy = pt.Variations.ToString();
            this.SupportsPublishing = pt.SupportsPublishing;
        }

        public string EditorAlias { get; set; }

        public string Description { get; set; }

        public int EditorId { get; set; }

        public string VariesBy { get; set; }

        public bool SupportsPublishing { get; set; }
    }
}