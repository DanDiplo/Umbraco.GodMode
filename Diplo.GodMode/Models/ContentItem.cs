using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{
    public class ContentItem
    {
        public int Id { get; set; }

        public int ParentId { get; set; }

        public int Level { get; set; }

        public bool Trashed { get; set; }

        public string Icon { get; set; }

        public string Alias { get; set; }

        public string Name { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public int CreatorId { get; set; }

        public string CreatorName { get; set; }

        public int UpdaterId { get; set; }

        public string UpdaterName { get; set; }
    }
}
