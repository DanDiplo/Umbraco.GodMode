# Diplo.GodMode 17.4.1

This patch targets Umbraco 17 and .NET 10. User-facing notes are in the [package README](../Diplo.GodMode/NuGet.md#1741).

## Changes

- Fixes Property Search failing on larger or multilingual schemas when all properties are selected. Removes God Mode's 256-field guard and groups field clauses within one Lucene query, preserving filters, totals and paging without increasing the global clause limit.
- Preserves actionable server validation messages and directs unexpected search failures to the site logs.
- Adds a Marketplace link to the optional Diplo God Mode AI extension on About God Mode. Core remains independent of the AI companion package.
- Updates the core NuGet version, client package and lockfile, manifest version and entry-point cache key to `17.4.1`.

## Validation on 15 November 2026

- Both client type-checks and production builds passed.
- Release solution build passed with zero errors and 248 dependency warnings, including vulnerability advisories. This is not a clean dependency security audit.
- All 99 Release tests passed. Regression tests cover 1,500 fields, word/phrase/prefix matching, document-type filtering and requiring all words in the same property.
- The Property Search fix was tested as `17.4.1-propertysearch.1` on the local TranslateDemo site: searching `careers` across all properties and languages returned 16 items. That runtime check preceded the About link and stable version updates.
- Core archive inspection confirmed the DLL, `17.4.1` manifest, versioned entry point, packaged release notes, and the new AI Marketplace link in the built About asset. The manifest is under `staticwebassets/App_Plugins/DiploGodMode/`.
- Both package builds passed. The validation-only AI archive contains its DLL and manifest under `staticwebassets/App_Plugins/DiploGodModeAI/`, and declares core `17.4.1` as its minimum dependency.

## Manual upload

Upload only `artifacts/17.4.1/Diplo.GodMode.17.4.1.nupkg`. Release notes, upload instructions and a SHA-256 checksum accompany the package. No publication is performed by this preparation step.

The AI companion remains at the already-published version `1.0.0`. Any rebuilt AI package is for validation only and must not be uploaded. Its project reference now resolves to core `17.4.1`; this does not alter the dependency declared by the published AI package.

The local TranslateDemo site still references the preview package; building this stable release does not update its installed version.
