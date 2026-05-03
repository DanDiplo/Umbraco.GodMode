using System.Text;
using System.Text.RegularExpressions;
using Diplo.GodMode.Models;
using Umbraco.Extensions;

namespace Diplo.GodMode.Controllers
{
    /// <summary>
    /// Helper for dealing with partial views
    /// </summary>
    internal static partial class PartialHelper
    {
        /// <summary>
        /// Reguar expressions to find partials in the template text. Adds a group for cached partials.
        /// </summary>
        private static readonly Regex HtmlPartialRegex = PartialExtractor();

        private static readonly Regex PartialTagRegex = PartialTag();

        private static readonly string[] replaceables = ["~/Views/Partials/", "/Views/Partials/", "/Partials/", "Partials/"];

        /// <summary>
        /// Gets the partials from the given template content
        /// </summary>
        /// <param name="content">The template content</param>
        /// <param name="id">The template Id</param>
        /// <param name="alias">The template Alias</param>
        /// <returns>Any partials in the template</returns>
        internal static IEnumerable<PartialMap> GetPartialInfo(string content, int id, string alias)
        {
            var partials = new List<PartialMap>();

            AddMatches(partials, content, id, alias, HtmlPartialRegex, 3);

            AddMatches(partials, content, id, alias, PartialTagRegex, 1);

            return partials;
        }

        private static void AddMatches(List<PartialMap> partials, string content, int id, string alias, Regex rex, int nameIndex)
        {
            MatchCollection matches = rex.Matches(content);

            foreach (Match match in matches.Cast<Match>())
            {
                if (match.Success)
                {
                    var partial = new PartialMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = ReplaceValues(match.Groups[nameIndex].Value).Replace(".cshtml", string.Empty, StringComparison.OrdinalIgnoreCase)
                    };

                    partial.Path = ReplaceValues(partial.Name);

                    if (!partial.Path.InvariantEndsWith(".cshtml"))
                    {
                        partial.Path += ".cshtml";
                    }

                    partials.Add(partial);
                }
            }
        }

        private static string ReplaceValues(string text)
        {
            StringBuilder sb = new(text);

            foreach (var replacement in replaceables)
            {
                sb.Replace(replacement, string.Empty);
            }

            return sb.ToString();
        }

        [GeneratedRegex(@"Html.(Cached)?Partial(Async)?\(\""(.+?)\"".*\)", RegexOptions.IgnoreCase)]
        private static partial Regex PartialExtractor();

        [GeneratedRegex(@"<partial name=\""(.*?)\"".*", RegexOptions.IgnoreCase)]
        private static partial Regex PartialTag();
    }
}