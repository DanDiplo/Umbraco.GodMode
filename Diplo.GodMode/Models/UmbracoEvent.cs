using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{
    public class UmbracoEvent
    {
        public string FullName { get; set; }

        public string Name { get; set; }

        public List<UmbracoEventItem> Items { get; set; }
    }

    public class UmbracoEventItem
    {
        public string Location { get; set; }

        public string Method { get; set; }

        public string Namespace { get; set; }
    }
}
