using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Diplo.GodMode.Models;
using Umbraco.Extensions;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Helper for dealing with view components in a template
    /// </summary>
    internal static class ViewComponentHelper
    {
        /// <summary>
        /// Reguar expression to find partials in the template text. Adds a group for cached partials.
        /// </summary>
        private static readonly Regex ViewComponentRegex = new Regex(@"Component.InvokeAsync\((.+?)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the partials from the given template content
        /// </summary>
        /// <param name="content">The template content</param>
        /// <param name="id">The template Id</param>
        /// <param name="alias">The template Alias</param>
        /// <returns>Any components in the template</returns>
        internal static IEnumerable<ComponentMap> GetViewComponentInfo(string content, int id, string alias)
        {
            MatchCollection matches = ViewComponentRegex.Matches(content);
            var components = new List<ComponentMap>();

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var component = new ComponentMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = match.Groups[1].Value
                    };

                    components.Add(component);
                }
            }

            return components;
        }
    }
}