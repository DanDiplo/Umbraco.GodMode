using System;
using System.Collections.Generic;

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
}
