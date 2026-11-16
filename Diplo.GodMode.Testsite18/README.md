# GodMode Umbraco 18 test host

This host uses its own copied SQLite database and media. It is pinned separately
from the v17 package projects and does not reference their source projects.

Run `dotnet run --project Diplo.GodMode.Testsite18/Diplo.GodMode.Testsite18.csproj`
from the repository root, or select this project as the Visual Studio startup
project. The backoffice is at https://localhost:44358/umbraco/ (HTTP: 43138).
Use the existing copied database login; startup does not reset its password.

Without a local package selection this is a standalone CMS 18 host with
Umbraco.AI, Clean and Contentment. GodMode is installed through the local v18
package build described below.
Do not install the v17 GodMode artifacts or add v17 project references here.

For stable release packages, use `scripts/build-release.ps1` from the repository
root. It builds the public v17/v18 core and AI pairs without changing this host's
local selection. The [release guide](../docs/release-17.5.0-18.0.0.md) records
compatibility, final artifact paths, validation and manual upload steps. The
experimental workflow below remains useful during development.

## Selecting local GodMode packages

From the repository root, run:

```powershell
pwsh -NoProfile -File scripts/build-v18-local.ps1
dotnet run --project Diplo.GodMode.Testsite18/Diplo.GodMode.Testsite18.csproj
```

The helper stages shared source under ignored `artifacts/v18-build`, builds both
clients against backoffice 18.2.0, runs the backend tests, packs core and AI, and
inspects their assets and dependency ranges. It then selects unique local
versions in `GodModePackage.local.props`. It does not start or stop the host,
publish packages or change the source projects' v17 dependency defaults.
Stop a running host before rebuilding it. Repeat the helper after source changes.

`GodModePackage.local.props` is an optional, ignored machine-local selection file.
It is not a NuGet package and must not contain the copied Omni package version.
After creating and inspecting v18 packages in `artifacts/v18-feed`, populate it:

```xml
<Project>
  <PropertyGroup>
    <UsePackedGodModePackage>true</UsePackedGodModePackage>
    <GodModePackageVersion>ACTUAL-CORE-PACKAGE-VERSION</GodModePackageVersion>
    <GodModeAiPackageVersion>ACTUAL-AI-PACKAGE-VERSION</GodModeAiPackageVersion>
  </PropertyGroup>
</Project>
```

Omit `GodModeAiPackageVersion` for core-only testing. Leave packed mode disabled
until the packages exist. Restore/build and restart after selecting a new version.
The default experimental package range is `[18.2.0,19.0.0)`; this does not claim
compatibility with CMS 18.0 or 18.1. The AI build uses Umbraco.AI 18.3.5. These are
local prerelease packages for acceptance, not a declaration of public v18 support.

The local host allows 4096 AI output tokens to accommodate structured explanations
with its configured profile. A smaller response budget produced incomplete JSON
during testing. Re-enter provider connection credentials after cloning if requests
fail authentication. See `../docs/v18-local-verification.md` for tested scope.

SQLite lives under `umbraco/Data`; database, media, logs, temporary files and local
settings are ignored by Git. Stop the site before copying its database. Any
private development settings belong in `appsettings.Development.local.json`.
That file is loaded only in Development; environment variables and command-line
arguments can override it. AI connection credentials in the copied database may
need re-entering.
