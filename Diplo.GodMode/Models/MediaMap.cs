using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a media item mapping
    /// </summary>
    public class MediaMap : ItemBase
    {
        public string Ext { get; set; }

        public string Type { get; set; }

        public int Size { get; set; }

        public DateTime CreateDate { get; set; }
    }
}