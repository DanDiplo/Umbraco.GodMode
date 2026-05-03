# Diplo Umbraco GodMode

**Diplo God Mode makes Umbraco developers invincible!**

[![NuGet](https://img.shields.io/nuget/v/Diplo.GodMode?color=004880&logo=nuget&label=NuGet)](https://www.nuget.org/packages/Diplo.GodMode/)
[![NuGet downloads](https://img.shields.io/nuget/dt/Diplo.GodMode?color=cc9900&label=downloads)](https://www.nuget.org/packages/Diplo.GodMode/)
[![Umbraco](https://img.shields.io/badge/Umbraco-17-3544b1?logo=umbraco)](https://umbraco.com/)
[![.NET](https://img.shields.io/badge/.NET-10-512bd4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](https://licenses.nuget.org/MIT)
[![GitHub issues](https://img.shields.io/github/issues/DanDiplo/Umbraco.GodMode?logo=github)](https://github.com/DanDiplo/Umbraco.GodMode/issues)

## What this does

This package adds a **God Mode** tree to the **Settings** section of **Umbraco 17**. 

It gives developers fast access to site structure, diagnostics, configuration, content references, services, templates, partials, media, members, tags, and other implementation details that are useful while building or supporting an Umbraco 17 site.

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

God Mode reads its options from the `GodMode` section of `appsettings.json`. All settings are optional; this example shows the current defaults:

```json
{
  "GodMode": {
    "FeaturesToHide": [],
    "Diagnostics": {
      "GroupsToHide": [],
      "SectionsToHide": [],
      "KeysToRedact": [
        "ConnectionStrings:umbracoDbDSN",
        "godmode_password",
        "ConnectionString"
      ],
      "KeyMatchesToRedact": [
        "password",
        "pwd",
        "secret",
        "key"
      ],
      "RedactRevealPasswordEnv": "godmode_password"
    }
  }
}
```

`FeaturesToHide` hides complete God Mode sections by name or alias, for example `"Services"` or `"Content Browser"`.

`GroupsToHide` hides complete diagnostic groups by title, such as `"Server Configuration"` or `"Umbraco Configuration"`.

`SectionsToHide` hides individual diagnostic sections by heading, such as `"MVC Version"`.

`KeysToRedact` redacts exact diagnostic keys. Non-environment diagnostics can also be redacted with scoped keys in the form `"Section:Key"` or `"Group:Section:Key"`. Environment config values use the raw configuration key, for example `"ConnectionStrings:umbracoDbDSN"`.

`KeyMatchesToRedact` redacts any diagnostic whose key contains one of the configured words, case-insensitively. By default this catches keys containing `password`, `pwd`, `secret`, or `key`.

Restart the site after changing these settings.

### Revealing Redacted Diagnostics

Redacted diagnostics stay hidden by default. To enable the **Reveal** control in the diagnostics browser, set an environment variable whose name matches `Diagnostics:RedactRevealPasswordEnv`. The default variable name is `godmode_password`.

For local PowerShell development:

```powershell
$env:godmode_password = "use-a-strong-local-password"
dotnet run --project Diplo.GodMode.Testsite/Diplo.GodMode.Testsite.csproj
```

For a persistent Windows user environment variable:

```powershell
[Environment]::SetEnvironmentVariable("godmode_password", "use-a-strong-local-password", "User")
```

For a deployment environment, set `godmode_password` in the host's environment variable settings. If you change `RedactRevealPasswordEnv`, set the environment variable using that custom name instead.

When the variable is present, the diagnostics screen shows a password field. Enter the value of the environment variable to reload diagnostics with redacted values revealed for the current browser session. If the variable is missing, empty, or the configured variable name is blank, reveal is disabled.

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

This code is indebted to a lot of people in the Umbraco community. Particular thanks to Soren Kottal for his help, to Sebastiaan "Cultiv" Janssen for diagnostic code borrowed in earlier versions, Andy Butler for his cleverness and to everyone who maintains Umbraco docs and package examples.
