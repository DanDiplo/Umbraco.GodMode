using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Diplo.GodMode.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NPoco;

namespace Diplo.GodMode.Services;

public sealed class GodModeLogService : IGodModeLogService
{
    private const int MaxFilesToScan = 60;
    private const long MaxPageSize = 100;

    private readonly IWebHostEnvironment env;
    private readonly ILogger<GodModeLogService> logger;

    public GodModeLogService(IWebHostEnvironment env, ILogger<GodModeLogService> logger)
    {
        this.env = env;
        this.logger = logger;
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

    public Page<GodModeLogEvent> GetLogs(long page, long pageSize, DateTimeOffset? from, DateTimeOffset? to, string? level, string? search)
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

    private static string CreateId(string fileName, string json)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fileName + "\n" + json));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
