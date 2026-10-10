using System.Net;
using System.Text.Json;
using System.Reflection;
using Diplo.GodMode.Helpers;
using Diplo.GodMode.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class PackageReleaseServiceTests
{
    [TestMethod]
    public async Task CountsDistinctListedStableVersionsWithinRunningMajor()
    {
        using var handler = new FeedHandler(InlineFeed(
            Entry("17.4.1"), Entry("17.4.2"), Entry("17.5.0"), Entry("17.5.0+build"),
            Entry("17.5.0.0"), Entry("17.7.1"), Entry("17.10.0"), Entry("18.0.0"),
            Entry("17.11.0-beta1"), Entry("17.12.0", false)));
        var service = Create(handler);
        var status = await service.GetStatusAsync();
        Assert.AreEqual("behind", status.Status);
        Assert.AreEqual("17.10.0", status.LatestVersion);
        Assert.AreEqual(3, status.ReleasesBehind);
        Assert.AreEqual("https://releases.umbraco.com/release/umbraco/Umbraco-CMS/17.10.0", status.ReleaseNotesUrl);
        Assert.IsFalse(status.IsStale);
        Assert.IsNotNull(status.CheckedAt);
        await service.GetStatusAsync();
        Assert.AreEqual(2, handler.Calls); // Discovery plus inlined registration; cached on second call.
    }

    [TestMethod]
    [DataRow("17.7.1+abcdef", "upToDate", 0)]
    [DataRow("17.8.0", "newerThanFeed", 0)]
    [DataRow("17.7.1-rc1+abcdef", "prerelease", null)]
    public async Task HandlesBuildMetadataPreviewAndNewerInstallations(string current, string expected, int? behind)
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1")));
        var status = await Create(handler, current: current).GetStatusAsync();
        Assert.AreEqual(expected, status.Status);
        Assert.AreEqual(behind, status.ReleasesBehind);
    }

    [TestMethod]
    public async Task FetchesOnlyOverlappingPagesAndHandlesMissingListedFlag()
    {
        var feed = JsonSerializer.Serialize(new { items = new object[]
        {
            PageLink("16.0.0", "16.9.0", "https://api.nuget.org/unused16"),
            PageLink("17.0.0-rc1", "18.0.0-beta1", "https://api.nuget.org/page"),
            PageLink("18.0.0", "18.9.0", "https://api.nuget.org/unused18")
        }});
        using var handler = new FeedHandler(feed)
        {
            Page = JsonSerializer.Serialize(new { items = new object[]
            {
                new { catalogEntry = new { version = "17.7.1" } }, Entry("18.0.0")
            }})
        };
        var status = await Create(handler).GetStatusAsync();
        Assert.AreEqual("17.7.1", status.LatestVersion);
        Assert.AreEqual(1, status.ReleasesBehind);
        Assert.AreEqual(3, handler.Calls);
    }

    [TestMethod]
    public async Task FailureBackoffKeepsLastSuccessfulResultAndRecovers()
    {
        var clock = new MutableClock();
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1")));
        var service = Create(handler, clock);
        var initial = await service.GetStatusAsync();
        clock.Advance(TimeSpan.FromHours(6));
        handler.Fail = true;
        var stale = await service.GetStatusAsync();
        Assert.IsTrue(stale.IsStale);
        Assert.AreEqual(initial.CheckedAt, stale.CheckedAt);
        Assert.AreEqual("17.7.1", stale.LatestVersion);
        Assert.AreEqual(clock.GetUtcNow(), stale.LastAttemptAt);
        var callsAfterFailure = handler.Calls;
        clock.Advance(TimeSpan.FromMinutes(9));
        await service.GetStatusAsync();
        Assert.AreEqual(callsAfterFailure, handler.Calls);
        clock.Advance(TimeSpan.FromMinutes(1));
        handler.Fail = false;
        handler.Feed = InlineFeed(Entry("17.7.1"), Entry("17.7.2"));
        var recovered = await service.GetStatusAsync();
        Assert.IsFalse(recovered.IsStale);
        Assert.AreEqual("17.7.2", recovered.LatestVersion);
        Assert.AreEqual(2, recovered.ReleasesBehind);
    }

    [TestMethod]
    public async Task InitialFailureIsUnavailableAndBackedOff()
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1"))) { Fail = true };
        var service = Create(handler);
        var result = await service.GetStatusAsync();
        Assert.AreEqual("unavailable", result.Status);
        Assert.IsNull(result.ReleasesBehind);
        Assert.IsNull(result.CheckedAt);
        Assert.IsFalse(result.IsStale);
        await service.GetStatusAsync();
        Assert.AreEqual(1, handler.Calls);
    }

    [TestMethod]
    public async Task ConcurrentCallersShareLookupAndCancellationDoesNotCancelOthers()
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1")));
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        handler.BeforeResponse = async token => await hold.Task.WaitAsync(token);
        var service = Create(handler);
        using var cancellation = new CancellationTokenSource();
        var cancelled = service.GetStatusAsync(cancellation.Token);
        var others = Enumerable.Range(0, 20).Select(_ => service.GetStatusAsync()).ToArray();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cancelled);
        hold.SetResult();
        var results = await Task.WhenAll(others);
        Assert.IsTrue(results.All(result => result.Status == "behind"));
        Assert.AreEqual(2, handler.Calls);
    }

    [TestMethod]
    public async Task OverallDeadlineStopsLookupAndCachesFailure()
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1")));
        handler.BeforeResponse = token => Task.Delay(Timeout.InfiniteTimeSpan, token);
        var service = Create(handler, timeout: TimeSpan.FromMilliseconds(50));
        var result = await service.GetStatusAsync().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.AreEqual("unavailable", result.Status);
        await service.GetStatusAsync();
        Assert.AreEqual(1, handler.Calls);
    }

    [TestMethod]
    [DataRow("{}")]
    [DataRow("not json")]
    [DataRow("{\"items\":[]}")]
    [DataRow("{\"items\":[{\"lower\":null,\"upper\":\"17.7.1\",\"items\":[]}]}")]
    [DataRow("{\"items\":[{\"lower\":\"17.0.0\",\"upper\":\"17.7.1\",\"items\":[{\"catalogEntry\":{\"version\":null}}]}]}")]
    public async Task IncompleteOrMalformedFeedDoesNotReportUpToDate(string feed)
    {
        using var handler = new FeedHandler(feed);
        var result = await Create(handler).GetStatusAsync();
        Assert.AreEqual("unavailable", result.Status);
        Assert.IsNull(result.ReleasesBehind);
    }

    [TestMethod]
    public async Task PartialPageFailureDoesNotCacheAnIncompleteCount()
    {
        var feed = JsonSerializer.Serialize(new { items = new object[]
        {
            new { lower = "17.0.0", upper = "17.5.0", items = new[] { Entry("17.5.0") } },
            PageLink("17.5.1", "17.7.1", "https://api.nuget.org/page")
        }});
        using var handler = new FeedHandler(feed) { Page = "{}" };
        var result = await Create(handler).GetStatusAsync();
        Assert.AreEqual("unavailable", result.Status);
        Assert.IsNull(result.LatestVersion);
        Assert.AreEqual(3, handler.Calls);
    }

    [TestMethod]
    public async Task RejectsOffHostPageLinksWithoutSendingRequest()
    {
        using var handler = new FeedHandler(JsonSerializer.Serialize(new { items = new[]
        {
            PageLink("17.0.0", "17.7.1", "https://other.example/page")
        }}));
        var result = await Create(handler).GetStatusAsync();
        Assert.AreEqual("unavailable", result.Status);
        Assert.AreEqual(2, handler.Calls);
    }

    private static object Entry(string version, bool listed = true) => new { catalogEntry = new { version, listed } };

    [TestMethod]
    public async Task GodModeUsesItsOwnPackageFeedCountAndVersionLink()
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1")))
        {
            GodModeFeed = InlineFeed(Entry("16.9.0"), Entry("17.1.0"), Entry("17.2.0"), Entry("17.2.2"),
                Entry("17.2.2+build"), Entry("17.3.0-rc1"), Entry("17.4.0", false), Entry("18.0.0"))
        };
        var factory = new ClientFactory(handler);
        var godMode = new GodModeReleaseService(factory, "17.1.0+abc", NullLogger<GodModeReleaseService>.Instance, new MutableClock());
        var umbraco = Create(handler);
        var results = await Task.WhenAll(godMode.GetStatusAsync(), umbraco.GetStatusAsync());
        Assert.AreEqual("17.2.2", results[0].LatestVersion);
        Assert.AreEqual(2, results[0].ReleasesBehind);
        Assert.AreEqual("https://www.nuget.org/packages/Diplo.GodMode", results[0].SourceUrl);
        Assert.AreEqual("https://www.nuget.org/packages/Diplo.GodMode/17.2.2", results[0].ReleaseNotesUrl);
        Assert.AreEqual("17.7.1", results[1].LatestVersion);
        await Task.WhenAll(godMode.GetStatusAsync(), umbraco.GetStatusAsync());
        Assert.AreEqual(4, handler.Calls);
    }

    [TestMethod]
    public async Task GodModeRefreshFailureDoesNotAffectUmbracoCache()
    {
        var clock = new MutableClock();
        using var handler = new FeedHandler(InlineFeed(Entry("17.7.1"))) { GodModeFeed = InlineFeed(Entry("17.2.2")) };
        var godMode = new GodModeReleaseService(new ClientFactory(handler), "17.1.0", NullLogger<GodModeReleaseService>.Instance, clock);
        var umbraco = Create(handler, clock);
        await Task.WhenAll(godMode.GetStatusAsync(), umbraco.GetStatusAsync());
        clock.Advance(TimeSpan.FromHours(6));
        handler.FailGodMode = true;
        handler.Feed = InlineFeed(Entry("17.7.1"), Entry("17.7.2"));
        var results = await Task.WhenAll(godMode.GetStatusAsync(), umbraco.GetStatusAsync());
        Assert.IsTrue(results[0].IsStale);
        Assert.AreEqual("17.2.2", results[0].LatestVersion);
        Assert.IsFalse(results[1].IsStale);
        Assert.AreEqual("17.7.2", results[1].LatestVersion);
    }

    [TestMethod]
    [DataRow("17.2.2+abc", "upToDate")]
    [DataRow("17.3.0+abc", "newerThanFeed")]
    [DataRow("17.3.0-rc1+abc", "prerelease")]
    public async Task GodModeHandlesCurrentLocalAndPreviewBuilds(string current, string expected)
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.2.2")));
        var service = new GodModeReleaseService(new ClientFactory(handler), current,
            NullLogger<GodModeReleaseService>.Instance, new MutableClock());
        Assert.AreEqual(expected, (await service.GetStatusAsync()).Status);
    }

    [TestMethod]
    public async Task GodModeVersionComesFromTheInstalledCoreAssembly()
    {
        using var handler = new FeedHandler(InlineFeed(Entry("17.99.0")));
        var service = new GodModeReleaseService(new ClientFactory(handler), NullLogger<GodModeReleaseService>.Instance);
        var expected = typeof(UtilitiesService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.AreEqual(expected, GodModeVersionHelper.GetVersion());
        Assert.AreEqual(expected, (await service.GetStatusAsync()).CurrentVersion);
    }

    private static object PageLink(string lower, string upper, string url)
        => new Dictionary<string, object> { ["lower"] = lower, ["upper"] = upper, ["@id"] = url };

    private static string InlineFeed(params object[] entries) => JsonSerializer.Serialize(new { items = new[]
    {
        new { lower = "17.0.0", upper = "18.0.0", items = entries }
    }});

    private static UmbracoReleaseService Create(FeedHandler handler, MutableClock? clock = null,
        string current = "17.4.2+abcdef", TimeSpan? timeout = null)
        => new(new ClientFactory(handler), current, NullLogger<UmbracoReleaseService>.Instance,
            clock ?? new MutableClock(), timeout);

    private sealed class ClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class MutableClock : TimeProvider
    {
        private DateTimeOffset now = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan duration) => now += duration;
    }

    private sealed class FeedHandler(string feed) : HttpMessageHandler
    {
        private int calls;
        public int Calls => calls;
        public string Feed { get; set; } = feed;
        public string? GodModeFeed { get; set; }
        public string Page { get; set; } = "{}";
        public bool Fail { get; set; }
        public bool FailGodMode { get; set; }
        public Func<CancellationToken, Task>? BeforeResponse { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref calls);
            if (BeforeResponse is not null) await BeforeResponse(cancellationToken);
            if (Fail || (FailGodMode && request.RequestUri!.AbsolutePath == "/registration/diplo.godmode/index.json"))
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            var json = request.RequestUri!.AbsolutePath switch
            {
                "/v3/index.json" => "{\"resources\":[{\"@type\":\"RegistrationsBaseUrl/3.6.0\",\"@id\":\"https://api.nuget.org/registration/\"}]}",
                "/registration/umbraco.cms/index.json" => Feed,
                "/registration/diplo.godmode/index.json" => GodModeFeed ?? Feed,
                "/page" => Page,
                _ => throw new InvalidOperationException("Unexpected metadata request: " + request.RequestUri)
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        }
    }
}
