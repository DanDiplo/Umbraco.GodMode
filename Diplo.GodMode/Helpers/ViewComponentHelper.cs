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
        /// Regular expression to find view component calls in template text.
        /// </summary>
        private static readonly Regex ViewComponentRegex = ExtractViewComponentCalls();

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

            foreach (Match match in ViewComponentRegex.Matches(content).Cast<Match>())
            {
                if (match.Success && TryParseViewComponentCall(content, match.Index + match.Length - 1, out var name, out var parameters))
                {
                    var component = new ComponentMap
                    {
                        TemplateId = id,
                        TemplateAlias = alias,
                        Name = name,
                        Parameters = parameters,
                        TagHelper = false
                    };

                    components.Add(component);
                }
            }

            foreach (Match match in ViewComponentTagRegex.Matches(content).Cast<Match>())
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

        private static bool TryParseViewComponentCall(string content, int openParenIndex, out string name, out string parameters)
        {
            name = string.Empty;
            parameters = string.Empty;

            var closeParenIndex = FindClosingParenthesis(content, openParenIndex);
            if (closeParenIndex < 0)
            {
                return false;
            }

            var arguments = content.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
            if (arguments.Length == 0)
            {
                return false;
            }

            var splitIndex = FindTopLevelComma(arguments);
            var rawName = splitIndex < 0 ? arguments : arguments[..splitIndex];

            name = NormalizeComponentName(rawName);
            parameters = splitIndex < 0 ? string.Empty : arguments[(splitIndex + 1)..].Trim();

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

                if ((c == '"' || c == '\'') && !IsLikelyGenericTypeQuote(content, i))
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
            var angleDepth = 0;
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

                if ((c == '"' || c == '\'') && !IsLikelyGenericTypeQuote(text, i))
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
                    case '<':
                        angleDepth++;
                        break;
                    case '>':
                        angleDepth--;
                        break;
                    case ',' when parenthesisDepth == 0 && braceDepth == 0 && bracketDepth == 0 && angleDepth == 0:
                        return i;
                }
            }

            return -1;
        }

        private static string NormalizeComponentName(string rawName)
        {
            var name = rawName.Trim();

            if (name.StartsWith("nameof(", StringComparison.OrdinalIgnoreCase) && name.EndsWith(')'))
            {
                name = name[7..^1].Trim();
            }
            else if (name.StartsWith("typeof(", StringComparison.OrdinalIgnoreCase) && name.EndsWith(')'))
            {
                name = name[7..^1].Trim();
            }

            if ((name.StartsWith('"') && name.EndsWith('"')) || (name.StartsWith('\'') && name.EndsWith('\'')))
            {
                name = name[1..^1];
            }

            if (name.EndsWith("ViewComponent", StringComparison.Ordinal))
            {
                name = name[..^"ViewComponent".Length];
            }

            return name.Trim();
        }

        private static bool IsLikelyGenericTypeQuote(string text, int index)
        {
            return text[index] == '\'' &&
                index > 0 &&
                index + 1 < text.Length &&
                (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_') &&
                (char.IsLetterOrDigit(text[index + 1]) || text[index + 1] == '_');
        }

        [GeneratedRegex(@"Component\.InvokeAsync\s*\(", RegexOptions.IgnoreCase)]
        private static partial Regex ExtractViewComponentCalls();

        [GeneratedRegex(@"<vc:([a-z][\w-]*)([^>]*)/?>", RegexOptions.IgnoreCase)]
        private static partial Regex ExtractViewComponentTags();
    }
}
