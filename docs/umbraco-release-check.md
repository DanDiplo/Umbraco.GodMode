# Overview release checks

The Overview's App card checks the running Umbraco version against listed stable versions of `Umbraco.Cms` on NuGet within the same major. The GodMode card performs the same check for the installed `Diplo.GodMode` assembly version, using the same version helper as the overview diagnostics. Both checks share the lookup, comparison, cache and rendering helpers.

Each card shows the latest version and counts distinct newer versions; minor and patch releases each count as one release. Preview releases and unlisted packages are excluded. Commit/build metadata does not affect comparison. Preview installations are labelled as previews, and versions newer than the feed are shown explicitly, including unpublished local builds.

The check loads independently of the overview diagnostics. It requires outbound HTTPS access to `api.nuget.org` from the server; the browser uses God Mode's authenticated Management API. The service discovers NuGet's SemVer 2 registration resource and reads only metadata pages overlapping the running major.

Results are cached in memory for six hours per package per application process. Concurrent requests for the same package share one lookup. Each package's discovery and metadata requests share a three-second deadline. Failures are cached for ten minutes to avoid repeated requests; the previous successful result remains visible as **Last known**, with its check time. With no successful result, the card says **Release check unavailable**. Each card loads independently and one failed check does not affect the other. The caches reset when the application restarts.

The count describes newer listed stable package versions, so it may differ from historical release counts if packages are unlisted. It is an upgrade awareness indicator, not a security audit or an upgrade compatibility assessment. Umbraco links to its release notes; God Mode links to the version-specific NuGet package page.

## Initial live probe

On 10 October 2026, a cold NuGet lookup (service index, registration index and one selected metadata page) took 899 ms. It returned 25 listed stable v17 versions, exactly matching the versions linked from the release page. The release-page HTML fetch took 183 ms in the same probe. Both sources identified 17.7.1 as latest and nine newer releases than 17.4.2. These measurements are a single sample from the development machine, not a latency guarantee for deployed hosts.

A subsequent live probe of the actual C# service returned the same result in 1,001 ms on its first call and 0.013 ms on its cached call. The temporary live probe was removed after verification; the regression suite uses synthetic feeds and does not require internet access.

The God Mode metadata probe took 372 ms and identified 17.2.2 as the latest listed stable v17 package. The development checkout builds 17.3.0, so it correctly reports a version newer than the feed.

A live probe of both actual C# services running concurrently returned in 851 ms cold and 0.13 ms cached, preserving Umbraco's expected result and identifying God Mode's unpublished local version correctly. Isolated browser checks with mocked API responses verified that either card can finish while the other is loading and that a God Mode failure leaves Umbraco's status intact. These component checks do not constitute signed-in end-to-end verification.
