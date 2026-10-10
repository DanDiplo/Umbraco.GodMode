# Overview release checks

The Overview's App card checks the running Umbraco version against listed stable versions of `Umbraco.Cms` on NuGet within the same major. The GodMode card performs the same check for the installed `Diplo.GodMode` assembly version, using the same version helper as the overview diagnostics. Both checks share the lookup, comparison, cache and rendering helpers.

Each card shows the latest version and counts distinct newer versions; minor and patch releases each count as one release. Preview releases and unlisted packages are excluded. Commit/build metadata does not affect comparison. Preview installations are labelled as previews, and versions newer than the feed are shown explicitly, including unpublished local builds.

The check loads independently of the overview diagnostics. It requires outbound HTTPS access to `api.nuget.org` from the server; the browser uses God Mode's authenticated Management API. The service discovers NuGet's SemVer 2 registration resource and reads only metadata pages overlapping the running major.

Results are cached in memory for six hours per package per application process. Concurrent requests for the same package share one lookup. Each package's discovery and metadata requests share a three-second deadline. Failures are cached for ten minutes to avoid repeated requests; the previous successful result remains visible as **Last known**, with its check time. With no successful result, the card says **Release check unavailable**. Each card loads independently and one failed check does not affect the other. The caches reset when the application restarts.

Any failure during a check is treated the same way, whether it is a network error, a timeout, an unexpected response from NuGet or an unexpected error in the check itself: the card shows the last known result or **Release check unavailable**, and the check backs off for ten minutes. Failures are logged at `Debug` level so an offline server does not fill the logs. If the running version cannot be read as a `major.minor.patch` version (for example an empty version or a four-part assembly version with a nonzero revision), that card shows **Release check unavailable** and no request is made. A zero revision is accepted: `17.3.0.0` is compared as `17.3.0`.

## Turning the check off

Servers that must not make outbound requests can turn the check off in `appsettings.json`:

```json
{
  "GodMode": {
    "EnableReleaseCheck": false
  }
}
```

When it is off, God Mode starts no new release lookups and the cards show their versions without any release status when the Overview loads. The setting is read on each check, so changing it takes effect without restarting the application. A lookup already in progress can finish, including making further metadata requests within its existing three-second deadline. Reload an already-open Overview to hide its release status. For servers that must never contact NuGet, set this to `false` before starting the application. It defaults to `true`.

## Interpreting the count

The count describes newer listed stable package versions, so it may differ from historical release counts if packages are unlisted. It is an upgrade awareness indicator, not a security audit or an upgrade compatibility assessment. Umbraco links to its release notes; God Mode links to the version-specific NuGet package page.

## Initial live probe

On 10 October 2026, a cold NuGet lookup (service index, registration index and one selected metadata page) took 899 ms. It returned 25 listed stable v17 versions, exactly matching the versions linked from the release page. The release-page HTML fetch took 183 ms in the same probe. Both sources identified 17.7.1 as latest and nine newer releases than 17.4.2. These measurements are a single sample from the development machine, not a latency guarantee for deployed hosts.

A subsequent live probe of the actual C# service returned the same result in 1,001 ms on its first call and 0.013 ms on its cached call. The temporary live probe was removed after verification; the regression suite uses synthetic feeds and does not require internet access.

The God Mode metadata probe took 372 ms and identified 17.2.2 as the latest listed stable v17 package. The development checkout builds 17.3.0, so it correctly reports a version newer than the feed.

A live probe of both actual C# services running concurrently returned in 851 ms cold and 0.13 ms cached, preserving Umbraco's expected result and identifying God Mode's unpublished local version correctly. Isolated browser checks with mocked API responses verified that either card can finish while the other is loading and that a God Mode failure leaves Umbraco's status intact. These component checks do not constitute signed-in end-to-end verification.
