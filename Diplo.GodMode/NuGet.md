# Diplo God Mode

**Diplo God Mode makes Umbraco developers invincible!**

Diplo God Mode adds a developer-focused tree to the **Settings** section of **Umbraco 17**. It helps you inspect, search, and understand the structure of an Umbraco site from inside the backoffice, including document types, compositions, templates, partial views, data types, property editors, media, members, tags, custom controllers, registered services, generated models, diagnostics, and configuration.

This `17.x` package is the Umbraco 17 / .NET 10 version and has been rebuilt for the modern Umbraco backoffice with Lit, TypeScript, Vite, and Umbraco extension manifests.

## Features

* Quickly search document types, templates, editors, media, content, members, and tags.
* See document type inheritance, composition usage, data type usage, and property editor usage.
* Distinguish between element types and types that vary by culture or segment.
* Browse templates, partials, controllers, generated models, registered services, Content Finders, and URL providers.
* View content, media, and members in searchable/filterable tables.
* List tags and the content associated with each tag, including orphaned tags.
* Inspect diagnostics, server details, and configuration values, with optional redaction for sensitive settings.
* Clear Umbraco caches and restart the app where supported.

## Requirements and Dependencies

This version targets:

* **.NET 10** (`net10.0`)
* **Umbraco 17**

The NuGet package declares dependencies on the Umbraco 17 packages used by God Mode:

* `Umbraco.Cms.Api.Common`
* `Umbraco.Cms.Api.Management`
* `Umbraco.Cms.Core`
* `Umbraco.Cms.Infrastructure`
* `Umbraco.Cms.Web.Common`
* `Umbraco.Cms.Web.Website`

The package project currently builds against Umbraco `17.3.5`. Client-side build tooling such as Vite, TypeScript, and `@umbraco-cms/backoffice` is used only when building this repository and is not required by consuming Umbraco sites.

Version guide:

* `17.x` is for Umbraco 17 / .NET 10.
* `13.x` is for Umbraco 13 / .NET 8.
* `10.x` is for Umbraco 10, 11, and 12 / .NET 6.
* `9.x` is for Umbraco 9.
* `2.x` is for Umbraco 8.
* `1.x` is for Umbraco 7.

## Installation

Install from NuGet:

```powershell
dotnet add package Diplo.GodMode
```

After installation, restart the site and open the Umbraco backoffice. God Mode appears in the **Settings** section. The package manifest is served from:

```text
/App_Plugins/DiploGodMode/umbraco-package.json
```

## Screenshots

![God Mode welcome](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/welcome.png)

![Document Type Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/doc-type-browser.png)

![Data Type Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/datatypes.png)

![Data Type usage](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/datatype-usedby.png)

![Reference graph](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/reference-graph.png)

![Diagnostics](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/diagnostics.png)

![Health checks](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/health.png)

![Extensions](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/extensions.png)

![Services](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/services.png)

![Members](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/members.png)

![Tags](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/tags.png)

![Types](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/types.png)

![Key value editor](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/key-value-editor.png)

## Configuration

God Mode reads its options from the `GodMode` section of `appsettings.json`. Configuration is optional and documented in the project README:

https://github.com/DanDiplo/Umbraco.GodMode/tree/v17

## Links

* GitHub: https://github.com/DanDiplo/Umbraco.GodMode
* Umbraco Marketplace: https://marketplace.umbraco.com/package/diplo.godmode
* NuGet: https://www.nuget.org/packages/Diplo.GodMode
