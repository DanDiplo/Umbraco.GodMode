using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents the search criteria for content
    /// </summary>
    public class ContentCriteria
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public int? Level { get; set; }

        public int? CreatorId { get; set; }

        public int? UpdaterId { get; set; }

        public bool? Trashed { get; set; }

        public string Alias { get; set; }
    }
}