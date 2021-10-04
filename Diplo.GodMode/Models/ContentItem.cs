using System;
using System.Collections.Generic;

namespace Diplo.GodMode.Models
{
    public class ContentBasic
    {
        public int Id { get; set; }

        public Guid Udi { get; set; }

        public string Alias { get; set; }

        public string Name { get; set; }

        public string Icon { get; set; }
    }

    public class ContentTags : ContentBasic
    {
        public string Type { get; set; }

        public IEnumerable<Tag> Tags {  get; set; }
    }

    public class ContentItem : ContentBasic
    {
        public int ParentId { get; set; }

        public int Level { get; set; }

        public bool Trashed { get; set; }

        public string Path { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public int CreatorId { get; set; }

        public string CreatorName { get; set; }

        public int UpdaterId { get; set; }

        public string UpdaterName { get; set; }

        public string Culture { get; set; }
    }
}