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
        /// <param name="ct"></param>
        public PropertyTypeMap(PropertyType ct)
        {
            this.Alias = ct.Alias;
            this.Description = ct.Description;
            this.EditorAlias = ct.PropertyEditorAlias;
            this.EditorId = ct.DataTypeId;
            this.Id = ct.Id;
            this.Name = ct.Name;
            this.Description = ct.Description;
        }

        public string EditorAlias { get; set; }

        public string Description { get; set; }

        public int EditorId { get; set; }
    }
}