# Overview release checks

The Overview's App card checks the running Umbraco version against listed stable versions of `Umbraco.Cms` on NuGet within the same major. It shows the latest version and counts distinct newer versions; minor and patch releases each count as one release. Preview releases and unlisted packages are excluded. Commit/build metadata does not affect comparison. Preview installations are labelled as previews, and versions newer than the feed are shown explicitly.

The check loads independently of the overview diagnostics. It requires outbound HTTPS access to `api.nuget.org` from the server; the browser uses God Mode's authenticated Management API. The service discovers NuGet's SemVer 2 registration resource and reads only metadata pages overlapping the running major.

Results are cached in memory for six hours per application process. Concurrent requests share one lookup. Discovery and metadata requests share a three-second deadline. Failures are cached for ten minutes to avoid repeated requests; the previous successful result remains visible as **Last known**, with its check time. With no successful result, the card says **Release check unavailable**. The cache resets when the application restarts.

The count describes newer listed stable package versions, so it may differ from historical release counts if packages are unlisted. It is an upgrade awareness indicator, not a security audit or an upgrade compatibility assessment. The latest version links to Umbraco's release notes.

## Initial live probe

On 10 October 2026, a cold NuGet lookup (service index, registration index and one selected metadata page) took 899 ms. It returned 25 listed stable v17 versions, exactly matching the versions linked from the release page. The release-page HTML fetch took 183 ms in the same probe. Both sources identified 17.7.1 as latest and nine newer releases than 17.4.2. These measurements are a single sample from the development machine, not a latency guarantee for deployed hosts.

A subsequent live probe of the actual C# service returned the same result in 1,001 ms on its first call and 0.013 ms on its cached call. The temporary live probe was removed after verification; the regression suite uses synthetic feeds and does not require internet access.
