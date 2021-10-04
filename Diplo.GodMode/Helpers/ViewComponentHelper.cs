using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Diplo.GodMode.Models;

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
        private static readonly Regex ViewComponentRegex = new Regex(@"Component.InvokeAsync\((.\S+)(.*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex ViewComponentTagRegex = new Regex(@"<vc:(.\S+)(.+?)>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Gets the partials from the given template content
        /// </summary>
        /// <param name="content">The template content</param>
        /// <param name="id">The template Id</param>
        /// <param name="alias">The template Alias</param>
        /// <returns>Any components in the template</returns>
        internal static IEnumerable<ComponentMap> GetViewComponentInfo(string content, int id, string alias)
        {
            var components = new List<ComponentMap>();

            MatchCollection matches = ViewComponentRegex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var component = new ComponentMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = match.Groups[1]?.Value?.Replace("\"", string.Empty)?.Replace(",", string.Empty),
                        Parameters = match.Groups[2]?.Value,
                        TagHelper = false
                    };

                    components.Add(component);
                }
            }

            matches = ViewComponentTagRegex.Matches(content);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var component = new ComponentMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = match.Groups[1].Value,
                        Parameters = match.Groups[2]?.Value,
                        TagHelper = true
                    };

                    components.Add(component);
                }
            }

            return components;
        }
    }
}