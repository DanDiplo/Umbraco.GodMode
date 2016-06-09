using System;
using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents a model of a template
    /// </summary>
    public class TemplateModel : ItemBase
    {
        /// <summary>
        /// Instantiate the template model from an Umbraco template
        /// </summary>
        /// <param name="temp">The template interface</param>
        public TemplateModel(ITemplate temp)
        {
            this.Id = temp.Id;
            this.Name = temp.Name;
            this.Alias = temp.Alias;
            this.FilePath = temp.VirtualPath;
        }

        public bool IsMaster { get; set; }

        public IEnumerable<TemplateModel> Parents { get; set; }

        public string Path { get; set; }

        public string MasterAlias { get; set; }

        public string FilePath { get; set; }

        public IEnumerable<PartialMap> Partials { get; set; }
    }
}
