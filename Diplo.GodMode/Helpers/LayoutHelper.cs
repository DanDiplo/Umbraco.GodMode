using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Models;

namespace Diplo.GodMode.Helpers
{
    public class LayoutHelper
    {
        /// <summary>
        /// Reguar expression to get the layout name from a template
        /// </summary>
        private static readonly Regex LayoutRegex = new Regex(@"@{(.*?)Layout(\s*)=(\s*)""(.+).cshtml"";(.*?)}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// Attempts to parse the layout view from the template
        /// </summary>
        /// <param name="template">The template</param>
        /// <returns>The layout name (without .cshtml) or null if not found</returns>
        public static string GetTemplateInfo(ITemplate template)
        {
            MatchCollection matches = LayoutRegex.Matches(template.Content);

            foreach (Match match in matches)
            {
                if (match.Success && match.Groups.Count > 4)
                {
                    return match.Groups[4].Value.Trim();
                }
            }

            return null;
        }
    }
}
