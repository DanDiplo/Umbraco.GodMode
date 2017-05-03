using System;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents the base item for data
    /// </summary>
    public class ItemBase
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }
    }
}