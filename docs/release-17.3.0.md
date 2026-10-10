# Diplo.GodMode 17.3.0

This release targets Umbraco 17 and .NET 10. Full user-facing release notes are in the [NuGet package README](../Diplo.GodMode/NuGet.md#1730).

The release adds asynchronous, cached Overview checks for the latest stable Umbraco and God Mode versions within the running major; Scheduled Publishing; server time zone and UTC diagnostics; a Local / Server / UTC date display switch; and scheduling and date migration Health & Risk checks. It also corrects uptime and feature hiding.

## Package and metadata

- Core project, client package and lockfile, source manifest and built manifest use `17.3.0`.
- The NuGet release summary and packaged README include the release checks and their configuration opt-out.
- The main marketplace description and tags include release checks. Both marketplace JSON files parse, and the AI listing retains `IsSubPackageOf: Diplo.GodMode`.
- Core depends on Umbraco packages in `[17.0.0,18.0.0)` and has no dependency on the optional AI package.
- Published AI `1.0.0` requires core `17.1.0` or later. A local AI rebuild has a minimum core dependency of `17.3.0`; it is a validation artifact only because AI `1.0.0` is already published.

## Validation on 10 October 2026

- Both client type-checks passed.
- The Release solution build passed with zero errors and 98 existing dependency warnings, including vulnerability advisories. This is not a clean dependency security audit.
- All 84 tests passed.
- Both package builds passed. Archive inspection confirmed their assemblies, manifests and static web assets; the core archive contains the updated README, release summary, icon and MIT license metadata.
- An isolated consumer restored the generated core package with Umbraco `17.4.2`, built successfully, and loaded the `17.3.0.0` assembly. The installed DLL hash matched the generated archive.
- The consumer served `/App_Plugins/DiploGodMode/umbraco-package.json` with version `17.3.0`, and all 49 JavaScript files returned successfully with matching SHA256 hashes.
- Signed-in Umbraco backoffice end-to-end verification was not performed as part of this release preparation.

## Manual upload

The local output directory is `artifacts/17.3.0/`. Upload only `Diplo.GodMode.17.3.0.nupkg` to NuGet. The directory also contains `RELEASE-NOTES.md`, `UPLOAD.md` and `SHA256SUMS.txt`. The `validation-only` directory contains the AI package and must not be uploaded as another AI `1.0.0` release.

Core `17.3.0` was absent from NuGet when checked during preparation. No package was published automatically. Merge the release metadata into `v17` so the marketplace can read the updated JSON and linked documentation.
