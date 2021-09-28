using System;
using System.Collections.Generic;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents an Event that contain many event items
    /// </summary>
    public class UmbracoEvent
    {
        public string FullName { get; set; }

        public string Name { get; set; }

        public List<UmbracoEventItem> Items { get; set; }
    }

    /// <summary>
    /// Represents a single event item
    /// </summary>
    public class UmbracoEventItem
    {
        public string Location { get; set; }

        public string Method { get; set; }

        public string Namespace { get; set; }
    }
}