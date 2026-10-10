using System.Reflection;
using System.Text.Json;
using Diplo.GodMode.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class GodModeLogEvidenceTests
{
    [TestMethod]
    public void MissingLogFolder_IsUnavailableRatherThanSuccessfulEmptyEvidence()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var result = CreateService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), cache).GetSystemDateMigrationLogs();
        Assert.IsFalse(result.CheckSucceeded);
        Assert.AreEqual(0, result.Entries.Count());
    }

    [TestMethod]
    public void MalformedLog_PreservesReadableEvidenceAndMarksCheckPartial()
    {
        WithLogFile(LogLine("Error", 0) + "\nnot JSON", (path, cache) =>
        {
            var result = CreateService(path, cache).GetSystemDateMigrationLogs();
            Assert.IsFalse(result.CheckSucceeded);
            Assert.AreEqual(1, result.Entries.Count());
            Assert.AreEqual("Error", result.Entries.Single().Level);
        });
    }

    [TestMethod]
    public void UnreadableLog_IsNotSuccessfulEmptyEvidence()
    {
        WithLogFile(LogLine("Error", 0), (path, cache) =>
        {
            using var lockedFile = new FileStream(Path.Combine(path, "log.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.IsFalse(CreateService(path, cache).GetSystemDateMigrationLogs().CheckSucceeded);
        });
    }

    [TestMethod]
    public void OlderMigrationError_IsRetainedBehindManyInformationEntries()
    {
        var lines = Enumerable.Range(1, 60).Select(i => LogLine("Information", i)).Prepend(LogLine("Error", 0));
        WithLogFile(string.Join("\n", lines), (path, cache) =>
        {
            var result = CreateService(path, cache).GetSystemDateMigrationLogs();
            Assert.IsTrue(result.CheckSucceeded);
            Assert.IsTrue(result.Entries.Any(x => x.Level == "Error"));
        });
    }

    private static string LogLine(string level, int second) => JsonSerializer.Serialize(new Dictionary<string, object>
    {
        ["@t"] = new DateTimeOffset(2026, 1, 10, 12, 0, 0, TimeSpan.Zero).AddSeconds(second),
        ["@l"] = level,
        ["@mt"] = "Migration event",
        ["SourceContext"] = "Umbraco.Cms.Infrastructure.Migrations.Upgrade.V_17_0_0.MigrateSystemDatesToUtc"
    });

    private static GodModeLogService CreateService(string directory, IMemoryCache cache)
    {
        var host = DispatchProxy.Create<IWebHostEnvironment, ServerTimeServiceTests.InterfaceStub>();
        ((ServerTimeServiceTests.InterfaceStub)(object)host).Call = method => method.Name switch
        {
            "get_ContentRootPath" or "get_WebRootPath" => directory,
            "get_EnvironmentName" => "Development",
            _ => throw new NotSupportedException(method.Name)
        };
        return new GodModeLogService(host, NullLogger<GodModeLogService>.Instance,
            Options.Create(new LoggingSettings { Directory = directory }), cache, null!);
    }

    private static void WithLogFile(string content, Action<string, IMemoryCache> assertion)
    {
        var directory = Path.Combine(Path.GetTempPath(), "godmode-logs-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "log.json"), content);
            using var cache = new MemoryCache(new MemoryCacheOptions());
            assertion(directory, cache);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
