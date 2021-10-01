using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Diplo.GodMode.Models;
using Umbraco.Extensions;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Helper for dealing with partial views
    /// </summary>
    internal static class PartialHelper
    {
        /// <summary>
        /// Reguar expression to find partials in the template text. Adds a group for cached partials.
        /// </summary>
        private static readonly Regex HtmlPartialRegex = new Regex(@"Html.(Cached)?Partial(Async)?\(\""(.+?)\"".*\)", RegexOptions.Compiled);

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
            var partials = new List<PartialMap>();

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var partial = new PartialMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = match.Groups[3].Value,
                        IsCached = match.Groups[1].Value.InvariantEquals("Cached"),
                        IsAsync = match.Groups[2].Value.InvariantEquals("Async")
                    };

                    partial.Path = partial.Name.Replace("/", "%252F").Replace("~%252FViews%252FPartials%252F", string.Empty);
                    partials.Add(partial);
                }
            }

            return partials;
        }
    }
}