# GodMode Umbraco 18 test host

This host uses its own copied SQLite database and media. It is pinned separately
from the v17 package projects and does not reference their source projects.

Run `dotnet run --project Diplo.GodMode.Testsite18/Diplo.GodMode.Testsite18.csproj`
from the repository root, or select this project as the Visual Studio startup
project. The backoffice is at https://localhost:44358/umbraco/ (HTTP: 43138).
Use the existing copied database login; startup does not reset its password.

Initially this is a standalone CMS 18 host with Umbraco.AI, Clean and Contentment.
GodMode is not installed until separately built v18 packages are available.
Do not install the v17 GodMode artifacts or add v17 project references here.

## Selecting local GodMode packages

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
The shared-source v18 build/package helper has not been implemented yet.

SQLite lives under `umbraco/Data`; database, media, logs, temporary files and local
settings are ignored by Git. Stop the site before copying its database. Any
private development settings belong in `appsettings.Development.local.json`.
That file is loaded only in Development; environment variables and command-line
arguments can override it. AI connection credentials in the copied database may
need re-entering.
