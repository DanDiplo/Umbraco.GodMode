using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Diplo.GodMode.Models;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Helper for dealing with partial views
    /// </summary>
    internal static class PartialHelper
    {
        private static readonly Regex HtmlPartialRegex = new Regex(@"@Html.(Cached)?Partial\(\""(.+?)\"".*\)", RegexOptions.Compiled);

        /// <summary>
        /// Gets the partials from the given template content
        /// </summary>
        /// <param name="content">The template content</param>
        /// <param name="id">The template Id</param>
        /// <param name="alias">The template Alias</param>
        /// <returns>Any partials in the template</returns>
        internal static IEnumerable<PartialMap> GetPartialInfo(string content, int id, string alias)
        {
            MatchCollection matches = HtmlPartialRegex.Matches(content);
            List<PartialMap> partials = new List<PartialMap>();

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    PartialMap partial = new PartialMap();
                    partial.TemplateId = id;
                    partial.TemplateAlias = alias;
                    partial.Name = match.Groups[2].Value;
                    partial.IsCached = match.Groups[1].Value == "Cached";
                    partial.Path = partial.Name.Replace("/", "%252F") + ".cshtml";
                    partials.Add(partial);
                }
            }

            return partials;
        }
    }
}