# God Mode 17.5.0 / 18.0.0 and AI 17.0.0 / 18.0.0

Prepared 16 November 2026 from shared source on `v17`. Publication is manual.

## Compatibility

| Package | Version | CMS range | Additional dependency |
| --- | --- | --- | --- |
| Diplo.GodMode | 17.5.0 | [17.0.0,18.0.0) | None on AI |
| Diplo.GodMode.AI | 17.0.0 | [17.3.0,18.0.0) | core [17.5.0,18.0.0), Umbraco.AI [1.10.1,2.0.0) |
| Diplo.GodMode | 18.0.0 | [18.2.0,19.0.0) | None on AI |
| Diplo.GodMode.AI | 18.0.0 | [18.2.0,19.0.0) | core [18.0.0,19.0.0), Umbraco.AI [18.3.5,19.0.0) |

All packages target .NET 10. Umbraco 18.0 and 18.1 are outside this release's
supported range. The branch name remains `v17`; no separate v18 source branch is
required. Normal development builds retain v17 dependencies.

## Release notes

* First separate Umbraco 18.2+ core and AI releases from the shared source.
* Scheduled Publishing places active registrations first, then newest check-ins.
* Administrators can review and select inactive, non-publisher registrations older
  than 30 days for deletion. The current instance and recent registrations are
  protected. Confirmation is required; batches are capped at 100. The server lock,
  fresh state and eligibility checks protect registrations that changed after
  preview. A returning instance can register again.
* Matching AI packages add **Explain scheduling health**. Counts and bounded
  samples are assembled only when requested, without content bodies or server
  addresses/machine identities. Guidance distinguishes failed checks and historic
  registrations from outages. No settings changes or cleanup can be executed.
* Fixes Property Search button submission, tree paging compatibility and a false
  warning caused by checking a generated JavaScript chunk name.
* Improves AI structured-output budget and provider authentication documentation.

## Upgrade

On CMS 17, update core to `17.5.0` and AI, if installed, to `17.0.0`.
The AI version jump from `1.0.0` aligns numbering with the supported CMS major;
it does not mean upgrading CMS to 18. On CMS 18.2+, use `18.0.0` for both packages.
Choose provider packages for your Umbraco.AI major and restart the site after
updating. Core still works without the AI add-on, with no AI controls or gaps.

Existing AI `1.0.0` should retain its earlier Explain behaviour with the new v17
core, but that mixed-version combination has not been runtime tested. Update both
for the new scheduling-health prompt and button label. Never republish AI `1.0.0`.

AI requests require a working provider connection/profile. If cloning a site,
re-enter provider credentials if authentication fails. Structured explanations may
need a larger output budget; `4096` worked with the tested profile. Keep
Temperature unset unless the model supports it. Settings remain opt-in.

## Build and inspection

```powershell
pwsh -NoProfile -File scripts/build-release.ps1
```

Requires PowerShell 7, the .NET 10+ SDK with MTP support, npm/Node compatible with
Vite 8, and Node's native TypeScript stripping for the focused context tests
(Node 25 used locally). The script stages each target under ignored
`artifacts/releases/<timestamp>/`, preserving normal source dependencies. It runs
four context tests, both client type checks/builds per target and backend tests
per major. It packs core first and builds AI against that inspected local NuGet.

The `packages` directory contains only the four stable NuGets and `SHA256SUMS.txt`.
The parent `release-manifest.json` records the source commit, dirty-tree state,
build time and package hashes. Each staging directory holds TRX results and
compiled clients. Build from a clean committed tree for final upload provenance.
The script never publishes packages or modifies test-site package selections.

Inspection checks package IDs/versions, net10.0 libraries, icons, embedded README
target headers, manifest versions, versioned entry-point URLs, entry-point assets,
CMS ranges, AI-to-core and Umbraco.AI ranges, and absence of an AI dependency in
core. v18 readmes identify the v18 artifact while preserving the shared feature,
configuration and compatibility documentation.

## Validation scope

The shared implementation passed 107 backend tests on each major and four
scheduling-context tests. Earlier v18 SQLite browser checks covered navigation,
content, Property Search, Dictionary editing, templates, media, data types,
database schema, Element Type usage, cleanup preview/cancel/approved disposable
deletion and a real provider AI round trip. See
[local acceptance evidence](v18-local-verification.md) for details.

SQL Server runtime testing on v18 and an independent dependency compatibility
audit were waived by the maintainer. Existing transitive vulnerability advisories
remain; this release does not claim to fix them. The build supports CMS 17 core
from 17.0 and AI from 17.3, but does not constitute runtime testing of every minor.
Final stable-artifact checks are recorded below after packaging.

## Manual upload

1. Review the final validation record and hashes. Upload only the four stable
   NuGets from this release run; exclude `artifacts/v18-feed` local prereleases.
2. Publish core `17.5.0`, then AI `17.0.0`; publish core `18.0.0`, then AI `18.0.0`.
   Wait until each core is available before uploading its AI package.
3. Push the release commit on `v17`; create core tags `17.5.0`, `18.0.0` and AI tags
   `ai-17.0.0`, `ai-18.0.0` at that same source commit, with the appropriate notes.
4. Verify NuGet dependency ranges and allow Marketplace ingestion. The core listing
   uses `umbraco-marketplace.json`; AI uses
   `umbraco-marketplace-diplo.godmode.ai.json`, retaining `IsSubPackageOf`.

No NuGet publication, remote push or release tags are performed by package preparation.
