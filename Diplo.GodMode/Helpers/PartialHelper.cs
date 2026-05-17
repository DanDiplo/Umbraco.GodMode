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
        /// Regular expressions to find partials in the template text.
        /// </summary>
        private static readonly Regex HtmlPartialRegex = HtmlPartialCall();

        private static readonly Regex PartialTagRegex = PartialTagStart();

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

            foreach (Match match in HtmlPartialRegex.Matches(content).Cast<Match>())
            {
                if (match.Success && TryParsePartialCall(content, match.Index + match.Length - 1, out var name))
                {
                    partials.Add(CreatePartialMap(id, alias, name));
                }
            }

            foreach (Match match in PartialTagRegex.Matches(content).Cast<Match>())
            {
                if (match.Success && TryParsePartialTag(content, match.Index, out var name))
                {
                    partials.Add(CreatePartialMap(id, alias, name));
                }
            }

            return partials;
        }

        private static PartialMap CreatePartialMap(int id, string alias, string name)
        {
            var partial = new PartialMap
            {
                TemplateId = id,
                TemplateAlias = alias,
                Name = ReplaceValues(name).Replace(".cshtml", string.Empty, StringComparison.OrdinalIgnoreCase)
            };

            partial.Path = ReplaceValues(partial.Name);

            if (!partial.Path.InvariantEndsWith(".cshtml"))
            {
                partial.Path += ".cshtml";
            }

            return partial;
        }

        private static bool TryParsePartialCall(string content, int openParenIndex, out string name)
        {
            name = string.Empty;

            var closeParenIndex = FindClosingParenthesis(content, openParenIndex);
            if (closeParenIndex < 0)
            {
                return false;
            }

            var arguments = content.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
            var splitIndex = FindTopLevelComma(arguments);
            var firstArgument = splitIndex < 0 ? arguments : arguments[..splitIndex];

            return TryUnquote(firstArgument.Trim(), out name);
        }

        private static bool TryParsePartialTag(string content, int tagStartIndex, out string name)
        {
            name = string.Empty;

            var tagEndIndex = FindTagEnd(content, tagStartIndex);
            if (tagEndIndex < 0)
            {
                return false;
            }

            var tag = content[tagStartIndex..(tagEndIndex + 1)];
            var match = PartialNameAttribute().Match(tag);
            if (!match.Success)
            {
                return false;
            }

            name = match.Groups["name"].Value.Trim();
            return name.Length > 0;
        }

        private static int FindClosingParenthesis(string content, int openParenIndex)
        {
            var depth = 0;
            var inString = false;
            var stringDelimiter = '\0';
            var isEscaped = false;
            var inVerbatimString = false;

            for (var i = openParenIndex; i < content.Length; i++)
            {
                var c = content[i];

                if (inString)
                {
                    if (inVerbatimString && c == '"' && i + 1 < content.Length && content[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (!inVerbatimString && c == '\\' && !isEscaped)
                    {
                        isEscaped = true;
                        continue;
                    }

                    if (c == stringDelimiter && (inVerbatimString || !isEscaped))
                    {
                        inString = false;
                        inVerbatimString = false;
                    }

                    isEscaped = false;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringDelimiter = c;
                    inVerbatimString = c == '"' && i > 0 && content[i - 1] == '@';
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private static int FindTopLevelComma(string text)
        {
            var parenthesisDepth = 0;
            var braceDepth = 0;
            var bracketDepth = 0;
            var inString = false;
            var stringDelimiter = '\0';
            var isEscaped = false;
            var inVerbatimString = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inString)
                {
                    if (inVerbatimString && c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        i++;
                        continue;
                    }

                    if (!inVerbatimString && c == '\\' && !isEscaped)
                    {
                        isEscaped = true;
                        continue;
                    }

                    if (c == stringDelimiter && (inVerbatimString || !isEscaped))
                    {
                        inString = false;
                        inVerbatimString = false;
                    }

                    isEscaped = false;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringDelimiter = c;
                    inVerbatimString = c == '"' && i > 0 && text[i - 1] == '@';
                    continue;
                }

                switch (c)
                {
                    case '(':
                        parenthesisDepth++;
                        break;
                    case ')':
                        parenthesisDepth--;
                        break;
                    case '{':
                        braceDepth++;
                        break;
                    case '}':
                        braceDepth--;
                        break;
                    case '[':
                        bracketDepth++;
                        break;
                    case ']':
                        bracketDepth--;
                        break;
                    case ',' when parenthesisDepth == 0 && braceDepth == 0 && bracketDepth == 0:
                        return i;
                }
            }

            return -1;
        }

        private static int FindTagEnd(string content, int tagStartIndex)
        {
            var inString = false;
            var stringDelimiter = '\0';

            for (var i = tagStartIndex; i < content.Length; i++)
            {
                var c = content[i];

                if (inString)
                {
                    if (c == stringDelimiter)
                    {
                        inString = false;
                    }

                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringDelimiter = c;
                    continue;
                }

                if (c == '>')
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool TryUnquote(string text, out string value)
        {
            value = string.Empty;

            if (text.Length < 2)
            {
                return false;
            }

            if ((text.StartsWith('"') && text.EndsWith('"')) || (text.StartsWith('\'') && text.EndsWith('\'')))
            {
                value = text[1..^1];
                return value.Length > 0;
            }

            return false;
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

        [GeneratedRegex(@"Html\.(?:Render|Cached)?Partial(?:Async)?\s*\(", RegexOptions.IgnoreCase)]
        private static partial Regex HtmlPartialCall();

        [GeneratedRegex(@"<partial\b", RegexOptions.IgnoreCase)]
        private static partial Regex PartialTagStart();

        [GeneratedRegex(@"\bname\s*=\s*(?:""(?<name>[^""]+)""|'(?<name>[^']+)')", RegexOptions.IgnoreCase)]
        private static partial Regex PartialNameAttribute();
    }
}
