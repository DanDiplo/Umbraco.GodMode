using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NPoco;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Diplo.GodMode.Services;

public sealed class GodModeLogService : IGodModeLogService
{
    private const int MaxFilesToScan = 60;
    private const long MaxPageSize = 100;

    private readonly IWebHostEnvironment env;
    private readonly ILogger<GodModeLogService> logger;
    private readonly IMemoryCache memoryCache;
    private readonly IScopeProvider scopeProvider;

    public GodModeLogService(IWebHostEnvironment env, ILogger<GodModeLogService> logger, IMemoryCache memoryCache, IScopeProvider scopeProvider)
    {
        this.env = env;
        this.logger = logger;
        this.memoryCache = memoryCache;
        this.scopeProvider = scopeProvider;
    }

    public GodModeLogOverview GetOverview()
    {
        var folder = GetLogFolder();

        return new GodModeLogOverview
        {
            LogFolder = folder,
            Exists = Directory.Exists(folder),
            FileCount = Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*.json").Count() : 0
        };
    }

    public Page<GodModeLogEvent> GetLogs(long page, long pageSize, DateTimeOffset? from, DateTimeOffset? to, string? level, string? search, string? queryExpression)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var fromUtc = from?.ToUniversalTime();
        var toUtc = to?.ToUniversalTime();
        var normalizedLevel = NormalizeLevel(level);
        var normalizedSearch = search?.Trim();

        var events = ReadEvents()
            .Where(log => MatchesDate(log, fromUtc, toUtc))
            .Where(log => string.IsNullOrWhiteSpace(normalizedLevel) || string.Equals(NormalizeLevel(log.Level), normalizedLevel, StringComparison.OrdinalIgnoreCase))
            .Where(log => MatchesSearch(log, normalizedSearch))
            .Where(log => MatchesQueryExpression(log, queryExpression))
            .OrderByDescending(log => log.Timestamp ?? DateTimeOffset.MinValue)
            .ThenByDescending(log => log.LogFile)
            .ToList();

        var total = events.Count;
        var items = events
            .Skip((int)((page - 1) * pageSize))
            .Take((int)pageSize)
            .ToList();

        return new Page<GodModeLogEvent>
        {
            CurrentPage = page,
            ItemsPerPage = pageSize,
            TotalItems = total,
            TotalPages = (long)Math.Ceiling(total / (decimal)pageSize),
            Items = items
        };
    }

    public IEnumerable<GodModeLogInsight> GetInsights(DateTimeOffset? from, DateTimeOffset? to, int take)
    {
        take = Math.Clamp(take, 1, 20);

        var fromUtc = from?.ToUniversalTime();
        var toUtc = to?.ToUniversalTime();
        var cacheKey = $"godmode:logs:insights:{fromUtc?.Ticks.ToString() ?? "null"}:{toUtc?.Ticks.ToString() ?? "null"}:{take}:{GetLogFileSignature()}";

        return memoryCache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            entry.SlidingExpiration = TimeSpan.FromMinutes(1);

            return ReadEvents()
                .Where(log => MatchesDate(log, fromUtc, toUtc))
                .Where(log => IsInsightLevel(log.Level))
                .GroupBy(CreateInsightKey)
                .Select(group => CreateInsight(group))
                .OrderByDescending(insight => InsightSeverityScore(insight.Level))
                .ThenByDescending(insight => insight.Count)
                .ThenByDescending(insight => insight.LastSeen ?? DateTimeOffset.MinValue)
                .Take(take)
                .ToList();
        }) ?? [];
    }

    public IEnumerable<GodModeLogLevelCount> GetLevelCounts(DateTimeOffset? from, DateTimeOffset? to, string? search, string? queryExpression)
    {
        var fromUtc = from?.ToUniversalTime();
        var toUtc = to?.ToUniversalTime();
        var normalizedSearch = search?.Trim();

        return ReadEvents()
            .Where(log => MatchesDate(log, fromUtc, toUtc))
            .Where(log => MatchesSearch(log, normalizedSearch))
            .Where(log => MatchesQueryExpression(log, queryExpression))
            .GroupBy(log => NormalizeLevel(log.Level))
            .Select(group => new GodModeLogLevelCount { Level = group.Key, Count = group.Count() })
            .OrderByDescending(count => InsightSeverityScore(count.Level))
            .ThenBy(count => count.Level)
            .ToList();
    }

    public IEnumerable<GodModeSavedLogQuery> GetSavedQueries()
    {
        try
        {
            using var scope = scopeProvider.CreateScope(autoComplete: true);
            return scope.Database.Fetch<GodModeSavedLogQuery>("SELECT CAST(id AS TEXT) AS id, name, query FROM umbracoLogViewerQuery ORDER BY name");
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Unable to read saved Umbraco log viewer queries.");
            return [];
        }
    }

    private IEnumerable<GodModeLogEvent> ReadEvents()
    {
        var folder = GetLogFolder();

        if (!Directory.Exists(folder))
        {
            yield break;
        }

        var files = Directory.EnumerateFiles(folder, "*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(MaxFilesToScan)
            .ToList();

        foreach (var file in files)
        {
            foreach (var logEvent in ReadFileEvents(file))
            {
                yield return logEvent;
            }
        }
    }

    private string GetLogFileSignature()
    {
        var folder = GetLogFolder();

        if (!Directory.Exists(folder))
        {
            return "missing";
        }

        var signature = Directory.EnumerateFiles(folder, "*.json")
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Take(MaxFilesToScan)
            .Select(file => $"{file.Name}:{file.Length}:{file.LastWriteTimeUtc.Ticks}");

        return CreateId("files", string.Join("|", signature));
    }

    private List<GodModeLogEvent> ReadFileEvents(FileInfo file)
    {
        var events = new List<GodModeLogEvent>();

        try
        {
            using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (reader.ReadLine() is { } line)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                GodModeLogEvent? logEvent;
                try
                {
                    logEvent = ParseEvent(trimmed, file.Name);
                }
                catch (JsonException)
                {
                    continue;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Unable to parse Umbraco log event from {LogFile}", file.FullName);
                    continue;
                }

                if (logEvent is not null)
                {
                    events.Add(logEvent);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to read Umbraco log file {LogFile}", file.FullName);
        }

        return events;
    }

    private static GodModeLogEvent? ParseEvent(string json, string fileName)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var root = document.RootElement;
        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.StartsWith('@'))
            {
                continue;
            }

            properties[property.Name] = ConvertJsonValue(property.Value);
        }

        var timestamp = GetDate(root, "@t") ?? GetDate(root, "Timestamp");
        var message = GetString(root, "@m") ?? GetString(root, "Message") ?? GetString(root, "RenderedMessage") ?? string.Empty;
        var messageTemplate = GetString(root, "@mt") ?? GetString(root, "MessageTemplate") ?? message;

        return new GodModeLogEvent
        {
            Id = CreateId(fileName, json),
            Timestamp = timestamp,
            Level = GetString(root, "@l") ?? GetString(root, "Level") ?? "Information",
            Message = message,
            MessageTemplate = messageTemplate,
            Exception = GetString(root, "@x") ?? GetString(root, "Exception") ?? string.Empty,
            SourceContext = GetString(root, "SourceContext") ?? string.Empty,
            RequestId = GetString(root, "RequestId") ?? string.Empty,
            RequestPath = GetString(root, "RequestPath") ?? string.Empty,
            MachineName = GetString(root, "MachineName") ?? GetString(root, "Machine") ?? string.Empty,
            ProcessId = GetInt(root, "ProcessId"),
            ThreadId = GetInt(root, "ThreadId"),
            LogFile = fileName,
            Properties = properties,
            RawJson = json
        };
    }

    private string GetLogFolder()
    {
        var umbracoLogs = Path.Combine(env.ContentRootPath, "umbraco", "Logs");
        if (Directory.Exists(umbracoLogs))
        {
            return umbracoLogs;
        }

        return Path.Combine(env.ContentRootPath, "Logs");
    }

    private static bool MatchesDate(GodModeLogEvent log, DateTimeOffset? fromUtc, DateTimeOffset? toUtc)
    {
        if (log.Timestamp is null)
        {
            return true;
        }

        var timestamp = log.Timestamp.Value.ToUniversalTime();
        if (fromUtc is not null && timestamp < fromUtc)
        {
            return false;
        }

        return toUtc is null || timestamp <= toUtc;
    }

    private static bool MatchesSearch(GodModeLogEvent log, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        return Contains(log.Message, search)
            || Contains(log.MessageTemplate, search)
            || Contains(log.Exception, search)
            || Contains(log.SourceContext, search)
            || Contains(log.RequestId, search)
            || Contains(log.RequestPath, search)
            || Contains(log.MachineName, search)
            || log.Properties.Any(property => Contains(property.Key, search) || Contains(property.Value?.ToString(), search));
    }

    private static bool MatchesQueryExpression(GodModeLogEvent log, string? queryExpression)
    {
        if (string.IsNullOrWhiteSpace(queryExpression))
        {
            return true;
        }

        var parts = System.Text.RegularExpressions.Regex.Split(queryExpression, @"\s+and\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return parts.Count == 0 || parts.All(part => MatchesQueryPart(log, part.Trim()));
    }

    private static bool MatchesQueryPart(GodModeLogEvent log, string expression)
    {
        var startsWith = System.Text.RegularExpressions.Regex.Match(expression, @"^StartsWith\(\s*(?<property>@?\w+)\s*,\s*'(?<value>[^']*)'\s*\)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (startsWith.Success)
        {
            return GetLogValue(log, startsWith.Groups["property"].Value)?.StartsWith(startsWith.Groups["value"].Value, StringComparison.OrdinalIgnoreCase) == true;
        }

        var has = System.Text.RegularExpressions.Regex.Match(expression, @"^Has\(\s*(?<property>@?\w+)\s*\)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (has.Success)
        {
            return !string.IsNullOrWhiteSpace(GetLogValue(log, has.Groups["property"].Value));
        }

        var like = System.Text.RegularExpressions.Regex.Match(expression, @"^(?<property>@?\w+)\s+like\s+'(?<value>[^']*)'$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (like.Success)
        {
            var needle = like.Groups["value"].Value.Trim('%');
            return Contains(GetLogValue(log, like.Groups["property"].Value), needle);
        }

        var equals = System.Text.RegularExpressions.Regex.Match(expression, @"^(?<property>@?\w+)\s*=\s*'?(?<value>[^']*)'?$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (equals.Success)
        {
            return string.Equals(GetLogValue(log, equals.Groups["property"].Value), equals.Groups["value"].Value, StringComparison.OrdinalIgnoreCase);
        }

        return MatchesSearch(log, expression);
    }

    private static string? GetLogValue(GodModeLogEvent log, string property)
    {
        var name = property.TrimStart('@');
        return name switch
        {
            "Message" or "m" => log.Message,
            "MessageTemplate" or "mt" => log.MessageTemplate,
            "Level" or "l" => NormalizeLevel(log.Level),
            "Exception" or "x" => log.Exception,
            "SourceContext" => log.SourceContext,
            "RequestId" => log.RequestId,
            "RequestPath" => log.RequestPath,
            "MachineName" or "Machine" => log.MachineName,
            "ProcessId" => log.ProcessId?.ToString(),
            "ThreadId" => log.ThreadId?.ToString(),
            _ => log.Properties.TryGetValue(name, out var value) ? value?.ToString() : null
        };
    }

    private static bool Contains(string? value, string search)
        => value?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;

    private static string? GetString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) ? JsonValueToString(value) : null;

    private static DateTimeOffset? GetDate(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), out var date)
            ? date
            : null;

    private static int? GetInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number) ? number : null;
    }

    private static object? ConvertJsonValue(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number when value.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };

    private static string? JsonValueToString(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };

    private static string NormalizeLevel(string? level)
        => level?.Trim() switch
        {
            "Verbose" => "Verbose",
            "Debug" => "Debug",
            "Information" => "Information",
            "Info" => "Information",
            "Warning" => "Warning",
            "Warn" => "Warning",
            "Error" => "Error",
            "Fatal" => "Fatal",
            _ => level?.Trim() ?? string.Empty
        };

    private static bool IsInsightLevel(string? level)
    {
        var normalized = NormalizeLevel(level);
        return normalized is "Warning" or "Error" or "Fatal";
    }

    private static string CreateInsightKey(GodModeLogEvent log)
    {
        var exceptionType = ExtractExceptionType(log.Exception);
        var message = NormalizeForGrouping(!string.IsNullOrWhiteSpace(log.MessageTemplate) ? log.MessageTemplate : log.Message);
        var source = log.SourceContext.Trim();
        var exceptionMessage = NormalizeForGrouping(ExtractFirstExceptionLine(log.Exception));

        return string.Join("|", NormalizeLevel(log.Level), source, exceptionType, message, exceptionMessage);
    }

    private static GodModeLogInsight CreateInsight(IGrouping<string, GodModeLogEvent> group)
    {
        var events = group
            .OrderByDescending(log => log.Timestamp ?? DateTimeOffset.MinValue)
            .ToList();
        var sample = events.First();
        var exceptionType = ExtractExceptionType(sample.Exception);
        var normalizedMessage = NormalizeForGrouping(!string.IsNullOrWhiteSpace(sample.MessageTemplate) ? sample.MessageTemplate : sample.Message);

        return new GodModeLogInsight
        {
            Id = CreateId("insight", group.Key),
            Level = NormalizeLevel(sample.Level),
            Title = BuildInsightTitle(sample, exceptionType, normalizedMessage),
            Count = events.Count,
            FirstSeen = events.Min(log => log.Timestamp),
            LastSeen = events.Max(log => log.Timestamp),
            SourceContext = sample.SourceContext,
            RequestPaths = events
                .Select(log => log.RequestPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList(),
            ExceptionType = exceptionType,
            NormalizedMessage = normalizedMessage,
            Sample = sample,
            Samples = events.Take(3).ToList()
        };
    }

    private static string BuildInsightTitle(GodModeLogEvent sample, string exceptionType, string normalizedMessage)
        => !string.IsNullOrWhiteSpace(exceptionType)
            ? $"{exceptionType}: {normalizedMessage}"
            : normalizedMessage.Length > 0
                ? normalizedMessage
                : sample.Message;

    private static string ExtractExceptionType(string? exception)
    {
        if (string.IsNullOrWhiteSpace(exception))
        {
            return string.Empty;
        }

        var firstLine = ExtractFirstExceptionLine(exception);
        var marker = firstLine.IndexOf(':');
        return marker > 0 ? firstLine[..marker].Trim() : firstLine.Trim();
    }

    private static string ExtractFirstExceptionLine(string? exception)
        => exception?
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(line => !line.TrimStart().StartsWith("at ", StringComparison.Ordinal))?
            .Trim() ?? string.Empty;

    private static string NormalizeForGrouping(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\b[0-9a-fA-F]{8}\b-[0-9a-fA-F-]{27,}\b", "{guid}");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\b\d+\b", "{number}");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
        return normalized.Length > 220 ? normalized[..220] : normalized;
    }

    private static int InsightSeverityScore(string? level)
        => NormalizeLevel(level) switch
        {
            "Fatal" => 3,
            "Error" => 2,
            "Warning" => 1,
            _ => 0
        };

    private static string CreateId(string fileName, string json)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fileName + "\n" + json));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
