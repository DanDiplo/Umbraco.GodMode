using System;

namespace Diplo.GodMode.Models
{
    public class ContentTypeData : ItemBase
    {
        public string Icon { get; set; }

        public string Description { get; set; }

        public bool IsMaster { get; set; }

        public bool HasCompositions { get; set; }

        public bool Selected { get; set; }
    }

    public class ContentTypeCompositionData : ContentTypeData
    {
        public bool IsElement { get; set; }

        public string VariesBy { get; set; }

        public bool VariesByCulture { get; set; }

        public int PropertyCount { get; set; }

        public int PropertyGroupCount { get; set; }
    }
}
