# Diplo God Mode

**Diplo God Mode makes Umbraco developers invincible!**

Diplo **God Mode** adds a developer-focused tree to the **Settings** section of **Umbraco 17**. It helps you inspect, search, and understand the structure of an Umbraco site from inside the backoffice, including document types, compositions, templates, partial views, data types, property editors, media, members, tags, custom controllers, registered services, generated models, diagnostics, and configuration.

If [Umbraco](https://umbraco.com/) is an Allen key then **God Mode** is the swiss army knife that helps you get under the hood and understand how things are working, find issues, and speed up development.

Now with optional [God Mode AI add-on](https://www.nuget.org/packages/Diplo.GodMode.AI) for contextual explanations powered by [Umbraco.AI](https://umbraco.com/ai/).

---

This `17.x` package is the Umbraco 17 / .NET 10 version and has been rebuilt for the modern Umbraco backoffice with Lit, TypeScript, Vite, and Umbraco extension manifests.

---

## Release Notes

### 17.2.2
* Fixes the Element Type Usage browser on SQL Server by mapping `nodeObjectType` values to their native GUID type during nested inline-block scans.
* Adds the required accessible label to the Element Type search input.

### 17.2.1
* Adds an Element Type Usage browser for stored block occurrences and configuration references across Block List, Block Grid, Rich Text, and single-block editors (thanks Marc Goodson).
* Detects Element Types nested inside Rich Text properties at any nesting depth, while keeping top-level, configured, and nested usage counts distinct.
* Includes forward-compatible Umbraco 18 Library Item and Element Picker usage when those features are available.
* Improves reliability across SQL Server and SQLite, correctly counts repeated occurrences, tolerates malformed JSON, adds detailed configuration links, and forces a fresh scan on reload.
* Scopes nested-usage deduplication to the exact property and optimises detail scans to query only the requested Element Type.

### 17.1.5
* Aligns the GodMode backoffice navigation with core Umbraco conventions, replacing the bespoke root menu element with the standard tree stack (thanks Rick Butterfield),
* Adds more health check coverage and improves the health check browser,
* Log viewer improvements, including better filtering and log level selection,

### 17.1.4
* Added NuGet package advisory coverage and package list improvements.
* Fixed configuration path handling and added culture hostname warnings.
* Improved pagination, performance, and caching across God Mode views.

### 17.1.3
* Fixed modal opening feedback for detail and evidence buttons that fetch data before opening their modal.
* Ensures slow Content, Media, Database, Data Type, Health Risk, Configuration Drift, and Type detail actions show progress immediately after click.

### 17.1.2
UI/UX improvements, including:
* Added visible opening feedback for God Mode modal actions so slow sites no longer appear unresponsive after a click.
* Unified the custom God Mode modal chrome for Used By, Evidence, and AI Explain dialogs.
* Updated paged browsers to use Umbraco's pagination control consistently.
* Moved pagination to the bottom of result lists and fixed spacing between filters and tables in Content, Media, and Member browsers.

---

## Features

* Quickly search document types, templates, editors, media, content, members, and tags.
* See document type inheritance, composition usage, data type usage, and property editor usage.
* Distinguish between element types and types that vary by culture or segment.
* Browse templates, partials, controllers, generated models, registered services, Content Finders, and URL providers.
* View content, media, and members in searchable/filterable tables.
* List tags and the content associated with each tag, including orphaned tags.
* Inspect diagnostics, server details, and configuration values, with optional redaction for sensitive settings.
* Clear Umbraco caches and restart the app where supported.
* Add contextual AI explanations by installing the separate [Diplo.GodMode.AI](https://www.nuget.org/packages/Diplo.GodMode.AI) companion package.

## Requirements and Dependencies

This version targets:

* **.NET 10** (`net10.0`)
* **Umbraco 17**

The package supports Umbraco `17.x` and declares its Umbraco CMS dependencies as `[17.0.0,18.0.0)`. Client-side build tooling such as Vite, TypeScript, and `@umbraco-cms/backoffice` is used only when building this repository and is not required by consuming Umbraco sites.

The core package does not depend on `Diplo.GodMode.AI`. Without the AI add-on installed, God Mode renders no AI controls.
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

## Optional God Mode AI Add-On

The *optional* [God Mode AI add-on](https://www.nuget.org/packages/Diplo.GodMode.AI) adds contextual **Explain** buttons powered by Umbraco.AI.

Install from NuGet:

```powershell
dotnet add package Diplo.GodMode.AI
```

To use it, configure the free [Umbraco.AI](https://marketplace.umbraco.com/package/umbraco.ai) with a [provider connection](https://marketplace.umbraco.com/category/artificial-intelligence?supportsUmbracoVersionNumber=17.4&maintainedBy=hq&packageType=Package) and profile first. For example, install the [OpenAI provider](https://marketplace.umbraco.com/package/umbraco.ai.openai), create an OpenAI connection in the Umbraco backoffice, then create a profile that uses a model such as `gpt-4.1-nano`.

Useful Umbraco.AI docs:

* Getting started: https://docs.umbraco.com/ai-in-umbraco/getting-started/getting-started
* Installation: https://docs.umbraco.com/ai-in-umbraco/getting-started/installation
* First connection: https://docs.umbraco.com/ai-in-umbraco/getting-started/first-connection
* First profile: https://docs.umbraco.com/ai-in-umbraco/getting-started/first-profile

## Screenshots

![God Mode welcome](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/welcome.png)

![Document Type Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/doc-type-browser.png)

![Document Type visual browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/doc-type-browser-visual.png)

![Data Type Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/datatypes.png)

![Data Type usage](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/datatype-usedby.png)

![Reference graph](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/reference-graph.png)

![Database Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/database-browser.png)

![Database Table Schema](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/database-table-schema.png)

![Diagnostics](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/diagnostics.png)

![Health checks](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/health.png)

![Extensions](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/extensions.png)

![Services](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/services.png)

![Log Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/log-browser.png)

![Media Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/media-browser.png)

![Members](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/members.png)

![Tags](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/tags.png)

![Types](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/types.png)

![Key value editor](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/key-value-editor.png)

![Template Browser](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/templates.png)

![Reverse Lookup](https://raw.githubusercontent.com/DanDiplo/Umbraco.GodMode/v17/Screenshots/reverse-lookup.png)

## Configuration

God Mode reads its options from the `GodMode` section of `appsettings.json`. Configuration is optional and documented in the project README:

https://github.com/DanDiplo/Umbraco.GodMode/tree/v17

## Links

* GitHub: https://github.com/DanDiplo/Umbraco.GodMode
* Umbraco Marketplace: https://marketplace.umbraco.com/package/diplo.godmode
* NuGet: https://www.nuget.org/packages/Diplo.GodMode
* God Mode AI NuGet: https://www.nuget.org/packages/Diplo.GodMode.AI
