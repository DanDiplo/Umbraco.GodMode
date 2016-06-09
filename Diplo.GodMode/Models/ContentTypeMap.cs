using System;
using System.Collections.Generic;

namespace Diplo.GodMode.Models
{
    public class ContentTypeMap : ContentTypeData
    {
        public IEnumerable<TemplateMap> Templates { get; set; }

        public IEnumerable<PropertyTypeMap> Properties { get; set; }

        public IEnumerable<PropertyTypeMap> CompositionProperties { get; set; }

        public IEnumerable<PropertyTypeMap> AllProperties { get; set; }

        public IEnumerable<ContentTypeData> Compositions { get; set; }

        public IEnumerable<string> PropertyGroups { get; set; }

        public bool HasTemplates { get; set; }

        public bool IsListView { get; set;  }

        public bool AllowedAtRoot { get; set; }

        public bool IsComposition { get; set; }
    }
}
