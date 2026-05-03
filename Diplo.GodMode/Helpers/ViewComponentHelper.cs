using System.Text.RegularExpressions;
using Diplo.GodMode.Models;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Helper for dealing with view components in a template
    /// </summary>
    internal static partial class ViewComponentHelper
    {
        /// <summary>
        /// Reguar expression to find partials in the template text. Adds a group for cached partials.
        /// </summary>
        private static readonly Regex ViewComponentRegex = ExtractViewComponents();

        private static readonly Regex ViewComponentTagRegex = ExtractViewComponentTags();

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

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Success)
                {
                    var component = new ComponentMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = match.Groups[1]?.Value?.Replace("\"", string.Empty)?.Replace(",", string.Empty)?.Trim(),
                        Parameters = match.Groups[2]?.Value,
                        TagHelper = false
                    };

                    components.Add(component);
                }
            }

            matches = ViewComponentTagRegex.Matches(content);

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Success)
                {
                    var component = new ComponentMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = match.Groups[1].Value?.Trim(),
                        Parameters = match.Groups[2]?.Value?.Trim(),
                        TagHelper = true
                    };

                    components.Add(component);
                }
            }

            return components;
        }

        [GeneratedRegex(@"Component.InvokeAsync\((.\S+)(.*)\)", RegexOptions.IgnoreCase)]
        private static partial Regex ExtractViewComponents();

        [GeneratedRegex(@"<vc:(.\S*)(.*?)>", RegexOptions.IgnoreCase)]
        private static partial Regex ExtractViewComponentTags();
    }
}