using System.Text.RegularExpressions;
using Diplo.GodMode.Models;

namespace Diplo.GodMode.Controllers
{
    internal static partial class ViewUsageHelper
    {
        internal static IEnumerable<ViewAssetMap> GetAssetInfo(string content, int id, string alias, string webRootPath = null)
        {
            var assets = new List<ViewAssetMap>();

            foreach (Match match in ScriptTag().Matches(content).Cast<Match>())
            {
                var attributes = match.Groups["attributes"].Value.Trim();
                var src = GetAttributeValue(attributes, "src");

                assets.Add(ResolveAsset(new ViewAssetMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = "Script",
                    Url = src,
                    Host = GetHost(src),
                    IsExternal = IsExternal(src),
                    IsInline = string.IsNullOrWhiteSpace(src),
                    Attributes = attributes
                }, webRootPath));
            }

            foreach (Match match in LinkTag().Matches(content).Cast<Match>())
            {
                var attributes = match.Groups["attributes"].Value.Trim();
                var href = GetAttributeValue(attributes, "href");
                var rel = GetAttributeValue(attributes, "rel");
                var asValue = GetAttributeValue(attributes, "as");
                var kind = rel.Contains("stylesheet", StringComparison.OrdinalIgnoreCase) || asValue.Equals("style", StringComparison.OrdinalIgnoreCase)
                    ? "Stylesheet"
                    : "Link";

                assets.Add(ResolveAsset(new ViewAssetMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = kind,
                    Url = href,
                    Host = GetHost(href),
                    IsExternal = IsExternal(href),
                    IsInline = false,
                    Attributes = attributes
                }, webRootPath));
            }

            foreach (Match match in StyleTag().Matches(content).Cast<Match>())
            {
                assets.Add(ResolveAsset(new ViewAssetMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = "Style",
                    Url = string.Empty,
                    Host = string.Empty,
                    IsExternal = false,
                    IsInline = true,
                    Attributes = match.Groups["attributes"].Value.Trim()
                }, webRootPath));
            }

            foreach (Match match in ImageTag().Matches(content).Cast<Match>())
            {
                var attributes = match.Groups["attributes"].Value.Trim();
                var src = GetAttributeValue(attributes, "src");
                var srcset = GetAttributeValue(attributes, "srcset");
                var url = string.IsNullOrWhiteSpace(src) ? srcset : src;

                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                assets.Add(ResolveAsset(new ViewAssetMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = "Image",
                    Url = url,
                    Host = GetHost(url),
                    IsExternal = IsExternal(url),
                    IsInline = false,
                    Attributes = attributes
                }, webRootPath));
            }

            foreach (Match match in CssUrl().Matches(content).Cast<Match>())
            {
                var url = match.Groups["url"].Value.Trim();

                assets.Add(ResolveAsset(new ViewAssetMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = "CssUrl",
                    Url = url,
                    Host = GetHost(url),
                    IsExternal = IsExternal(url),
                    IsInline = false,
                    Attributes = string.Empty
                }, webRootPath));
            }

            return assets
                .GroupBy(asset => $"{asset.Kind}|{asset.Url}|{asset.IsInline}|{asset.Attributes}")
                .Select(group => group.First());
        }

        private static ViewAssetMap ResolveAsset(ViewAssetMap asset, string webRootPath)
        {
            asset.ResolvedPath = string.Empty;
            asset.Warning = string.Empty;

            if (asset.IsInline || asset.IsExternal || string.IsNullOrWhiteSpace(asset.Url) || string.IsNullOrWhiteSpace(webRootPath))
            {
                asset.IsResolved = false;
                asset.Exists = false;
                return asset;
            }

            var normalizedUrl = StripQueryString(asset.Url).Trim();
            if (IsDynamicUrl(normalizedUrl))
            {
                asset.IsResolved = false;
                asset.Exists = false;
                return asset;
            }

            if (!normalizedUrl.StartsWith("~/", StringComparison.Ordinal) && !normalizedUrl.StartsWith("/", StringComparison.Ordinal))
            {
                asset.IsResolved = false;
                asset.Exists = false;
                return asset;
            }

            var relativePath = normalizedUrl.StartsWith("~/", StringComparison.Ordinal)
                ? normalizedUrl[2..]
                : normalizedUrl[1..];

            if (relativePath.Length == 0)
            {
                asset.IsResolved = false;
                asset.Exists = false;
                return asset;
            }

            var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            var rootPath = Path.GetFullPath(webRootPath);

            if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            {
                asset.IsResolved = false;
                asset.Exists = false;
                asset.Warning = "Asset path resolves outside the web root.";
                return asset;
            }

            asset.IsResolved = true;
            asset.ResolvedPath = fullPath;
            asset.Exists = File.Exists(fullPath);
            asset.Warning = asset.Exists ? string.Empty : "Asset file was not found under the web root.";

            return asset;
        }

        internal static IEnumerable<ViewSectionMap> GetSectionInfo(string content, int id, string alias)
        {
            return SectionDeclaration()
                .Matches(content)
                .Cast<Match>()
                .Where(match => match.Success)
                .Select(match => new ViewSectionMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Name = match.Groups["name"].Value.Trim(),
                    IsAsync = false
                })
                .GroupBy(section => section.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
        }

        internal static IEnumerable<ViewFormMap> GetFormInfo(string content, int id, string alias)
        {
            var forms = new List<ViewFormMap>();
            var hasAntiForgeryToken = AntiForgeryToken().IsMatch(content);

            foreach (Match match in FormTag().Matches(content).Cast<Match>())
            {
                var attributes = match.Groups["attributes"].Value.Trim();
                forms.Add(new ViewFormMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = "Tag",
                    Method = GetAttributeValue(attributes, "method"),
                    Action = FirstValue(GetAttributeValue(attributes, "action"), GetAttributeValue(attributes, "asp-action")),
                    Controller = GetAttributeValue(attributes, "asp-controller"),
                    HasAntiForgeryToken = hasAntiForgeryToken
                });
            }

            foreach (Match match in BeginFormCall().Matches(content).Cast<Match>())
            {
                forms.Add(new ViewFormMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = "HtmlHelper",
                    Method = string.Empty,
                    Action = ExtractFirstStringArgument(content, match.Index + match.Length - 1),
                    Controller = ExtractStringArgument(content, match.Index + match.Length - 1, 1),
                    HasAntiForgeryToken = hasAntiForgeryToken
                });
            }

            return forms;
        }

        internal static IEnumerable<ViewTagHelperMap> GetTagHelperInfo(string content, int id, string alias)
        {
            foreach (Match match in TagWithAspAttributes().Matches(content).Cast<Match>())
            {
                var tagName = match.Groups["tag"].Value;
                var attributes = match.Groups["attributes"].Value.Trim();

                yield return new ViewTagHelperMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    TagName = tagName,
                    Kind = GetTagHelperKind(tagName, attributes),
                    Attributes = attributes
                };
            }

            foreach (Match match in EnvironmentTag().Matches(content).Cast<Match>())
            {
                yield return new ViewTagHelperMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    TagName = "environment",
                    Kind = "Environment",
                    Attributes = match.Groups["attributes"].Value.Trim()
                };
            }
        }

        internal static IEnumerable<ViewUmbracoUsageMap> GetUmbracoUsageInfo(string content, int id, string alias)
        {
            var usages = new List<ViewUmbracoUsageMap>();

            AddCallUsages(usages, content, id, alias, ModelValueCall(), "Property Alias", "Model.Value");
            AddCallUsages(usages, content, id, alias, ModelHasValueCall(), "Property Alias", "Model.HasValue");
            AddCallUsages(usages, content, id, alias, ContentValueCall(), "Property Alias", "Content.Value");
            AddCallUsages(usages, content, id, alias, UmbracoContentCall(), "Umbraco Helper", "Umbraco.Content");
            AddCallUsages(usages, content, id, alias, UmbracoMediaCall(), "Umbraco Helper", "Umbraco.Media");
            AddCallUsages(usages, content, id, alias, BlockListCall(), "Block Rendering", "BlockList");
            AddCallUsages(usages, content, id, alias, BlockGridCall(), "Block Rendering", "BlockGrid");
            AddCallUsages(usages, content, id, alias, BlockListModelCall(), "Block Rendering", "BlockList");
            AddCallUsages(usages, content, id, alias, BlockGridModelCall(), "Block Rendering", "BlockGrid");

            return usages
                .GroupBy(usage => $"{usage.Kind}|{usage.Name}|{usage.Expression}")
                .Select(group => group.First());
        }

        private static void AddCallUsages(ICollection<ViewUmbracoUsageMap> usages, string content, int id, string alias, Regex regex, string kind, string expression)
        {
            foreach (Match match in regex.Matches(content).Cast<Match>())
            {
                if (!match.Success)
                {
                    continue;
                }

                usages.Add(new ViewUmbracoUsageMap
                {
                    TemplateId = id,
                    TemplateAlias = alias,
                    Kind = kind,
                    Name = FirstCapturedValue(match, "alias", "value"),
                    Expression = expression
                });
            }
        }

        private static string GetTagHelperKind(string tagName, string attributes)
        {
            if (tagName.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                return "Anchor";
            }

            if (tagName.Equals("form", StringComparison.OrdinalIgnoreCase))
            {
                return "Form";
            }

            if (tagName.Equals("img", StringComparison.OrdinalIgnoreCase))
            {
                return "Image";
            }

            return attributes.Contains("asp-route-", StringComparison.OrdinalIgnoreCase) ? "Route" : "AspNet";
        }

        private static string FirstCapturedValue(Match match, params string[] groupNames)
        {
            foreach (var groupName in groupNames)
            {
                var group = match.Groups[groupName];
                if (group.Success && !string.IsNullOrWhiteSpace(group.Value))
                {
                    return group.Value.Trim();
                }
            }

            return string.Empty;
        }

        private static string GetAttributeValue(string attributes, string name)
        {
            var match = Regex.Match(attributes, $@"\b{name}\s*=\s*(?:""(?<value>[^""]*)""|'(?<value>[^']*)')", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
        }

        private static string FirstValue(params string[] values)
            => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        private static string GetHost(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            if (url.StartsWith("//", StringComparison.Ordinal) && Uri.TryCreate($"https:{url}", UriKind.Absolute, out uri))
            {
                return uri.Host;
            }

            return string.Empty;
        }

        private static bool IsExternal(string url)
            => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("//", StringComparison.Ordinal);

        private static string StripQueryString(string url)
        {
            var markerIndex = url.IndexOfAny(['?', '#']);
            return markerIndex < 0 ? url : url[..markerIndex];
        }

        private static bool IsDynamicUrl(string url)
            => url.Contains('@', StringComparison.Ordinal) ||
                url.Contains('{', StringComparison.Ordinal) ||
                url.Contains('}', StringComparison.Ordinal);

        private static string ExtractFirstStringArgument(string content, int openParenIndex)
            => ExtractStringArgument(content, openParenIndex, 0);

        private static string ExtractStringArgument(string content, int openParenIndex, int argumentIndex)
        {
            var closeParenIndex = FindClosingParenthesis(content, openParenIndex);
            if (closeParenIndex < 0)
            {
                return string.Empty;
            }

            var arguments = content.Substring(openParenIndex + 1, closeParenIndex - openParenIndex - 1).Trim();
            var parts = SplitTopLevelArguments(arguments).ToArray();
            return argumentIndex < parts.Length && TryUnquote(parts[argumentIndex].Trim(), out var value) ? value : string.Empty;
        }

        private static IEnumerable<string> SplitTopLevelArguments(string text)
        {
            var start = 0;
            var parenthesisDepth = 0;
            var braceDepth = 0;
            var bracketDepth = 0;
            var inString = false;
            var stringDelimiter = '\0';
            var isEscaped = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (inString)
                {
                    if (c == '\\' && !isEscaped)
                    {
                        isEscaped = true;
                        continue;
                    }

                    if (c == stringDelimiter && !isEscaped)
                    {
                        inString = false;
                    }

                    isEscaped = false;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringDelimiter = c;
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
                        yield return text[start..i];
                        start = i + 1;
                        break;
                }
            }

            yield return text[start..];
        }

        private static int FindClosingParenthesis(string content, int openParenIndex)
        {
            var depth = 0;
            var inString = false;
            var stringDelimiter = '\0';
            var isEscaped = false;

            for (var i = openParenIndex; i < content.Length; i++)
            {
                var c = content[i];

                if (inString)
                {
                    if (c == '\\' && !isEscaped)
                    {
                        isEscaped = true;
                        continue;
                    }

                    if (c == stringDelimiter && !isEscaped)
                    {
                        inString = false;
                    }

                    isEscaped = false;
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    inString = true;
                    stringDelimiter = c;
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
                return true;
            }

            return false;
        }

        [GeneratedRegex(@"<script\b(?<attributes>[^>]*)>(?:.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ScriptTag();

        [GeneratedRegex(@"<link\b(?<attributes>[^>]*)/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex LinkTag();

        [GeneratedRegex(@"<style\b(?<attributes>[^>]*)>(?:.*?)</style>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex StyleTag();

        [GeneratedRegex(@"<img\b(?<attributes>[^>]*)/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex ImageTag();

        [GeneratedRegex(@"(?<![\w-])url\(\s*(?:""(?<url>[^""]+)""|'(?<url>[^']+)'|(?<url>[^)]+))\s*\)", RegexOptions.IgnoreCase)]
        private static partial Regex CssUrl();

        [GeneratedRegex(@"@section\s+(?<name>[A-Za-z_][\w-]*)\s*\{", RegexOptions.IgnoreCase)]
        private static partial Regex SectionDeclaration();

        [GeneratedRegex(@"<form\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex FormTag();

        [GeneratedRegex(@"Html\.BeginForm\s*\(", RegexOptions.IgnoreCase)]
        private static partial Regex BeginFormCall();

        [GeneratedRegex(@"(?:Html\.)?AntiForgeryToken\s*\(", RegexOptions.IgnoreCase)]
        private static partial Regex AntiForgeryToken();

        [GeneratedRegex(@"<(?<tag>[a-z][\w-]*)\b(?<attributes>[^>]*\basp-[^>]*)/?>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex TagWithAspAttributes();

        [GeneratedRegex(@"<environment\b(?<attributes>[^>]*)>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex EnvironmentTag();

        [GeneratedRegex(@"\bModel\.Value(?:<[^>]+>)?\(\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)')", RegexOptions.IgnoreCase)]
        private static partial Regex ModelValueCall();

        [GeneratedRegex(@"\bModel\.HasValue\(\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)')", RegexOptions.IgnoreCase)]
        private static partial Regex ModelHasValueCall();

        [GeneratedRegex(@"\b(?:content|item|block|page)\.Value(?:<[^>]+>)?\(\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)')", RegexOptions.IgnoreCase)]
        private static partial Regex ContentValueCall();

        [GeneratedRegex(@"\bUmbraco\.Content\(\s*(?:""(?<value>[^""]+)""|'(?<value>[^']+)'|(?<value>[^),]+))", RegexOptions.IgnoreCase)]
        private static partial Regex UmbracoContentCall();

        [GeneratedRegex(@"\bUmbraco\.Media\(\s*(?:""(?<value>[^""]+)""|'(?<value>[^']+)'|(?<value>[^),]+))", RegexOptions.IgnoreCase)]
        private static partial Regex UmbracoMediaCall();

        [GeneratedRegex(@"\bHtml\.GetBlockListHtml(?:Async)?\(\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)'|(?<alias>[^),]+))", RegexOptions.IgnoreCase)]
        private static partial Regex BlockListCall();

        [GeneratedRegex(@"\bHtml\.GetBlockGridHtml(?:Async)?\(\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)'|(?<alias>[^),]+))", RegexOptions.IgnoreCase)]
        private static partial Regex BlockGridCall();

        [GeneratedRegex(@"\bHtml\.GetBlockListHtml(?:Async)?\(\s*[^,]+,\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)')", RegexOptions.IgnoreCase)]
        private static partial Regex BlockListModelCall();

        [GeneratedRegex(@"\bHtml\.GetBlockGridHtml(?:Async)?\(\s*[^,]+,\s*(?:""(?<alias>[^""]+)""|'(?<alias>[^']+)')", RegexOptions.IgnoreCase)]
        private static partial Regex BlockGridModelCall();
    }
}
