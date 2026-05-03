# Umbraco GodMode

**Diplo God Mode makes Umbraco developers invincible!**

This package adds a **God Mode** tree to the **Settings** section of Umbraco 17. It gives developers fast access to site structure, diagnostics, configuration, content references, services, templates, partials, media, members, tags, and other implementation details that are useful while building or supporting an Umbraco site.

## Features

- Search document types, templates, editors, media, content, members, and tags.
- See document type inheritance, composition usage, data type usage, and property editor usage.
- Browse templates, partials, controllers, generated models, registered services, Content Finders, and URL providers.
- View content and media in searchable/filterable tables.
- Inspect diagnostics and configuration values, with optional redaction for sensitive settings.
- Clear Umbraco caches and restart the app where supported.

## Download & Installation

This branch targets **Umbraco 17 / .NET 10**.

```powershell
dotnet add package Diplo.GodMode
```

NuGet: https://www.nuget.org/packages/Diplo.GodMode/

After installation, restart the site and open the Umbraco backoffice. The **God Mode** tree should appear in **Settings** under third-party/package extensions. If it does not appear immediately, clear the browser cache and confirm `/App_Plugins/DiploGodMode/umbraco-package.json` is being served.

## Configuration

Features and diagnostic values can be hidden via `appsettings.json`:

```json
{
  "GodMode": {
    "FeaturesToHide": [
      "Services",
      "Content Browser"
    ],
    "Diagnostics": {
      "GroupsToHide": [
        "Server Configuration",
        "Umbraco Configuration"
      ],
      "SectionsToHide": [
        "MVC Version"
      ],
      "KeysToRedact": [
        "Database Settings:ConnectionString",
        "ConnectionStrings:umbracoDbDSN",
        "Server Settings:Current Directory",
        "Environment Settings:LocalTempPath"
      ]
    }
  }
}
```

`FeaturesToHide` hides complete God Mode sections by name or alias.

`GroupsToHide`, `SectionsToHide`, and `KeysToRedact` hide or redact diagnostic output that may reveal sensitive environment details. Restart the site after changing these settings.

## Building / Developing

The v17 branch contains two projects in one solution:

- `Diplo.GodMode/Diplo.GodMode.csproj` - the Umbraco 17 package source.
- `Diplo.GodMode.Testsite/Diplo.GodMode.Testsite.csproj` - a local Umbraco 17 demo/test site that references the package project.

Build everything from the repository root:

```powershell
dotnet build Diplo.GodMode.slnx
```

Build the package project only:

```powershell
dotnet build Diplo.GodMode/Diplo.GodMode.csproj
```

Run the demo site:

```powershell
dotnet run --project Diplo.GodMode.Testsite/Diplo.GodMode.Testsite.csproj
```

Then open the Umbraco backoffice at the URL shown by `dotnet run` and verify the God Mode package loads from `/App_Plugins/DiploGodMode/`.

The package backoffice is built with Lit, TypeScript, Vite, and Umbraco UI/backoffice packages from `Diplo.GodMode/Client`. The package project runs the client build during MSBuild. If `Client/node_modules` is missing, the build restores it with `npm ci`.

## Creating A NuGet Package

From the repository root:

```powershell
dotnet pack Diplo.GodMode/Diplo.GodMode.csproj -c Release
```

The package ships the compiled assembly and static web assets under `App_Plugins/DiploGodMode`, including `umbraco-package.json`.

## Thanks

This code is indebted to a lot of people in the Umbraco community. Particular thanks to Soren Kottal for his help, to Sebastiaan "Cultiv" Janssen for diagnostic code borrowed in earlier versions, and to everyone who maintains Umbraco docs and package examples.
