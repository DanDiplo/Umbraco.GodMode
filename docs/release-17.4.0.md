# Diplo.GodMode 17.4.0

This release targets Umbraco 17 and .NET 10. User-facing release notes are in the [NuGet package README](../Diplo.GodMode/NuGet.md#1740).

The release adds Dictionary Browser, indexed Property Search and About God Mode. Dictionary and content editing use native side panels. Property Search includes highlighted snippets, word/phrase/prefix matching, property/type/language filters, a Clear action that invalidates pending responses, and published-page View links resolved through Umbraco's URL provider.

## Package and metadata

- The core project, client package and lockfile, source manifest and built manifest use `17.4.0`.
- The NuGet release summary and packaged README describe the new features. The main README retains one-line feature summaries and links to the release notes.
- Marketplace metadata includes Property Search and Dictionary Browser, and both listings include Diplo Translator Omni among the author's packages. The AI listing retains `IsSubPackageOf: Diplo.GodMode`.
- Core retains its Umbraco dependency ranges of `[17.0.0,18.0.0)` and has no dependency on the optional AI package.
- Published AI `1.0.0` permits core `17.1.0` or later. A local AI rebuild requires core `17.4.0`; it is a validation artifact because AI `1.0.0` is already published.

## Validation on 14 November 2026

- Both client type-checks and client builds passed.
- The Release solution build passed with zero errors and 248 dependency warnings, including moderate and high vulnerability advisories. This is not a clean dependency security audit.
- All 93 Release tests passed, including Lucene word, phrase, prefix, document-type, query-escaping and recycle-bin exclusion checks.
- Core and AI package builds passed. Archive checks confirmed their assemblies, manifests, static assets, versions and dependencies. Core includes 53 JavaScript files and the updated README, icon, MIT license metadata and release summary.
- Signed-in local backoffice checks covered Dictionary Browser filtering and side-panel editing; Property Search filters, paging, highlighted snippets and side-panel editing; input alignment and autocomplete; empty-query and in-flight resets; and published View links opening in a new tab. View links respected the selected culture and were absent for unavailable published variants.
- Dictionary runtime checks used the SQLite demo site. SQL Server runtime behavior was not independently verified for this release.

## Manual upload

The output directory is `artifacts/17.4.0/`. Upload only `Diplo.GodMode.17.4.0.nupkg` to NuGet. Supporting release notes, upload instructions and checksums are stored alongside it. Files under `validation-only` and any consumer smoke-test directories are not upload packages.

NuGet did not list core `17.4.0` when checked during preparation. No NuGet package is published automatically. AI `1.0.0` must not be uploaded again.

The release source and marketplace documentation belong on `v17`. NuGet publication and marketplace ingestion are separate from local package validation.
