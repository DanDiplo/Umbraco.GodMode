using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{

    public class TagMapping
    {
        public string Key { get; set; }

        public Tag Tag { get; set; }

        public IEnumerable<ContentTags> Content { get; set; }
    }

}
