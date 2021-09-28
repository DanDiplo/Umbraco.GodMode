using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents an item from the nuCache
    /// </summary>
    public class NuCacheItem
    {
        public int Id { get; set; }

        public string Data { get; set; }

        public string Title { get; set; }

        public DateTime CreateDate { get; set; }
    }
}
