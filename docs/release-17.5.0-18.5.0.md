# God Mode 17.5.0 / 18.5.0 and AI 17.0.0 / 18.0.0

Prepared 16 November 2026 from shared source on `v17`. Publication is manual.

## Compatibility

| Package | Version | CMS range | Additional dependency |
| --- | --- | --- | --- |
| Diplo.GodMode | 17.5.0 | [17.0.0,18.0.0) | None on AI |
| Diplo.GodMode.AI | 17.0.0 | [17.3.0,18.0.0) | core [17.5.0,18.0.0), Umbraco.AI [1.10.1,2.0.0) |
| Diplo.GodMode | 18.5.0 | [18.2.0,19.0.0) | None on AI |
| Diplo.GodMode.AI | 18.0.0 | [18.2.0,19.0.0) | core [18.5.0,19.0.0), Umbraco.AI [18.3.5,19.0.0) |

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
it does not mean upgrading CMS to 18. On CMS 18.2+, use core `18.5.0` and AI `18.0.0`.
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

## Final artifacts and acceptance

The earlier core 18.0.0 release set is superseded by core 18.5.0, aligning its
feature version with core 17.5.0. AI remains 17.0.0 / 18.0.0; v18 AI now requires
core [18.5.0,19.0.0).

Final upload directory: `artifacts/releases/20261116171450415/packages/`.
Built from clean source commit `2c4435672869b9812f4448f339b05f0f35ad209f`.
Subsequent release-record documentation changes do not alter package inputs.
Previous release directories contain a SUPERSEDED notice; use only this final set.

| Package file | SHA-256 |
| --- | --- |
| Diplo.GodMode.17.5.0.nupkg | `AB2ECDE21982E269D1C4CAC4CDCBEAF2B49A50A61FE4A170BA12C6D51DCFD568` |
| Diplo.GodMode.AI.17.0.0.nupkg | `AAFDF95F46E84BDAE1D53D9A178FFAEAF161DD0A40638291D10DC7905225BF0B` |
| Diplo.GodMode.18.5.0.nupkg | `422D0FCF1AB5EFB03F6995CE2C5441F36129512829A5DDCFDAC102DAFEFBC77F` |
| Diplo.GodMode.AI.18.0.0.nupkg | `49009C52DD6EA436CA57BB628AF65C3FD70E15438F1B8554B5EA05FDEE9D1D6C` |

Final verification passed for the regenerated set:

* Four scheduling-context tests and 107 backend tests per CMS major.
* Both client type checks and production builds on each major.
* All four package identities, manifests, versioned entry-point assets, README
  target headers and dependency ranges passed inspection. AI 18.0.0 requires
  core [18.5.0,19.0.0); CMS support remains [18.2.0,19.0.0).
* Each stable core/AI pair restored, built and booted in an isolated host using a
  disposable copy of its corresponding SQLite database. Deployed core and AI DLL
  hashes matched the DLLs inside the final NuGets.
* Both backoffices, both plugin manifests and their entry-point JavaScript returned
  200. Served manifest versions matched the final stable versions. Anonymous God
  Mode API requests returned 401 on both hosts.
* Both temporary hosts were stopped; original databases and local package
  selections were unchanged. These final checks cover startup/assets/authentication;
  the earlier authenticated feature and real-provider acceptance used local
  prerelease builds of the same implementation.

The package directory contains SHA256SUMS.txt; its parent contains
release-manifest.json, per-major TRX results and host smoke-results.json records.
Logs remain under ignored artifacts/. Existing dependency advisories and the
validation limits above still apply. No packages, tags or commits were published.

## Manual upload

1. Review the final validation record and hashes. Upload only the four stable
   NuGets from this release run; exclude `artifacts/v18-feed` local prereleases.
2. Publish core `17.5.0`, then AI `17.0.0`; publish core `18.5.0`, then AI `18.0.0`.
   Wait until each core is available before uploading its AI package.
3. Push the release commit on `v17`; create core tags `17.5.0`, `18.5.0` and AI tags
   `ai-17.0.0`, `ai-18.0.0` at that same source commit, with the appropriate notes.
4. Verify NuGet dependency ranges and allow Marketplace ingestion. The core listing
   uses `umbraco-marketplace.json`; AI uses
   `umbraco-marketplace-diplo.godmode.ai.json`, retaining `IsSubPackageOf`.

No NuGet publication, remote push or release tags are performed by package preparation.
