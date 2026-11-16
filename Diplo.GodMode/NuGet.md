# Diplo God Mode

**Diplo God Mode makes Umbraco developers invincible!**

Diplo **God Mode** adds a developer-focused tree to the **Settings** section of **Umbraco 17 and 18**. It helps you inspect, search, and understand the structure of an Umbraco site from inside the backoffice, including document types, compositions, templates, partial views, data types, property editors, media, members, tags, custom controllers, registered services, generated models, diagnostics, and configuration.

If [Umbraco](https://umbraco.com/) is an Allen key then **God Mode** is the swiss army knife that helps you get under the hood and understand how things are working, find issues, and speed up development.

Now with optional [God Mode AI add-on](https://www.nuget.org/packages/Diplo.GodMode.AI) for contextual explanations powered by [Umbraco.AI](https://umbraco.com/ai/).

---

Choose `17.x` for Umbraco 17 and `18.x` for Umbraco 18.2+. Both target .NET 10 and the modern Lit backoffice. Release packages identify their specific target in the header.

---

## Release Notes

### 17.5.0 / 18.5.0

* Adds Umbraco 18.2+ packages built from shared source.
* Scheduled Publishing shows active servers first, then newest check-ins.
* Administrators can preview and select inactive, non-publisher registrations older than 30 days for cleanup. Current/recent instances are protected. Confirmation, a 100-entry limit and locked eligibility rechecks protect active registrations. Returning instances can register again.
* Adds **Explain scheduling health** with the matching AI add-on, using lazy, bounded snapshot evidence. No AI fixes or deletion actions are executed.
* Fixes Property Search button submission, tree paging compatibility and a false generated-JavaScript asset warning.
* AI versions align with CMS: use AI `17.0.0` with core `17.5.0`, or AI `18.0.0` with core `18.5.0`.


### 17.4.1

* **Search across larger sites:** fixes Property Search failing when searching all properties on sites with many properties or languages. Searches no longer hit God Mode's 256-field restriction, and field queries are grouped to stay within Lucene's per-query clause limit.
* **Clearer search errors:** shows actionable server validation messages instead of replacing every failure with a generic InternalIndex warning.
* **Find the optional AI extension:** adds a [Diplo God Mode AI Marketplace link](https://marketplace.umbraco.com/package/diplo.godmode.ai) to About God Mode, with a description of its contextual AI explanations.

### 17.4.0

* Adds **About God Mode**, with author information, contribution and issue links, and related Umbraco packages.

#### Property Search

* Adds **Property Search** under **Content & schema**, using Umbraco's existing `InternalIndex` to find text in content properties, including supported blocks and rich text. Choose all words within one property, a phrase, or a single word prefix, and filter by property, document type or language. Language filters include shared properties; `AliasesToIgnore` is respected.
* Shows highlighted indexed snippets and paged results, excluding media and recycle-bin content. Select a result title to edit in a side panel without losing the search context. **View** opens the published page in a new tab when its URL can be resolved, using the selected language where applicable or preferring a matching published language when all languages are selected.
* Emptying the input or selecting **Clear** removes the results and ignores pending search responses while preserving filters.
* Searches indexed text rather than every saved value. Index updates can lag behind edits, and some editor values and hyperlink destinations may be omitted. Block snippets identify the containing property rather than the exact nested block; custom analyzers may match an item without producing a display snippet.

#### Dictionary Browser

* Adds **Dictionary Browser** under **Content & schema**, with searches across keys and translated values, side-by-side language columns, and filters for missing or complete translations. Selecting a language scopes value searches and status checks to that language; **All languages** flags an item if any configured language has a blank or absent value.
* Hides empty parent items by default because they may be used for grouping, with an option to include them. **Complete** indicates non-empty values in the selected languages, rather than a translation-quality assessment.
* Opens keys and parent links in Umbraco's dictionary side editor, where normal language permissions apply. Closing the editor refreshes results while preserving filters, page and scroll position. Results are paged on the server, with translations fetched only for the current page.

Both features can be hidden through `GodMode:FeaturesToHide` using `propertySearch` or `dictionaryBrowser`, or their displayed names.

### 17.3.0
* Adds asynchronous Overview release checks for Umbraco and God Mode, showing the latest stable versions in each running major and the number of newer listed releases, with shared helpers, independent caches and graceful handling of unavailable NuGet access. Set `GodMode:EnableReleaseCheck` to `false` to turn off the outbound check.
* Adds a Scheduled Publishing view listing pending publish and unpublish schedules, overdue items, publishing suspension, Umbraco 17 background jobs and registered servers (thanks @suedeapple).
* Adds server time zone information to the Overview, including a Time Zone card, live server, UTC and browser clocks, clock drift, the next daylight saving change and the Umbraco 17 system-date migration settings with on-demand evidence.
* Adds a Local / Server / UTC date display switch, remembered per browser, across the content, media, member, data type, key/value, NuGet and log views. God Mode API dates are now sent as UTC.
* Adds Date & Time and UTC System Date Migration sections to Diagnostics, and Health & Risk rules for time zone, date migration, future-dated rows, overdue schedules, stalled background jobs and stale servers.
* Uptime now uses the real process start time.
* Fixes `FeaturesToHide` so hidden features are removed from the God Mode tree as well as the Welcome page. Matching is now case-insensitive, and folders are hidden when listed or when all their features are hidden (#59).
* Documents the `AliasesToIgnore`, `DeliveryApi:SensitiveAliasTerms` and God Mode AI settings.

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
* Find where Element Types are used across Block List, Block Grid, Rich Text, and single-block editors, including nested usage.
* Trace relationships between types, templates, data types, and block elements with the Reference Graph.
* Browse templates, partials, controllers, generated models, registered services, Content Finders, and URL providers.
* View content, media, and members in searchable/filterable tables.
* Find indexed text in content properties, including supported blocks and rich text, with Property Search.
* Search dictionary keys and translations, compare language values and find missing translations with the Dictionary Browser.
* List tags and the content associated with each tag, including orphaned tags.
* See pending scheduled publishing, overdue schedules, and when the scheduled publishing job last ran.
* Check the server time zone and UTC date handling, and switch dates between Local, Server, and UTC display.
* Surface broken references, configuration drift, and cleanup opportunities with Health & Risk checks.
* Inspect diagnostics, server details, and configuration values, with optional redaction for sensitive settings.
* See the latest stable Umbraco and God Mode versions in each running major and how many listed releases behind they are, loaded asynchronously in the Overview.
* Filter and search Umbraco JSON logs, and inspect database tables, extension manifests, and NuGet packages.
* Clear Umbraco caches and restart the app where supported.
* Hide any God Mode feature you don't need with configuration.
* Add contextual AI explanations by installing the separate [Diplo.GodMode.AI](https://www.nuget.org/packages/Diplo.GodMode.AI) companion package.

## Requirements and Dependencies

This version targets:

* **.NET 10** (`net10.0`)
* **Umbraco 17** for `17.x`, or **Umbraco 18.2+** for `18.x`

The CMS range is `[17.0.0,18.0.0)` for core `17.x` and `[18.2.0,19.0.0)` for core `18.x`. CMS 18.0 and 18.1 are outside this release range. Client-side build tooling such as Vite, TypeScript, and `@umbraco-cms/backoffice` is used only when building this repository and is not required by consuming Umbraco sites.

The core package does not depend on `Diplo.GodMode.AI`. Without the AI add-on installed, God Mode renders no AI controls.
Version guide:

* `18.x` is for Umbraco 18.2+ / .NET 10.
* `17.x` is for Umbraco 17 / .NET 10.
* `13.x` is for Umbraco 13 / .NET 8.
* `10.x` is for Umbraco 10, 11, and 12 / .NET 6.
* `9.x` is for Umbraco 9.
* `2.x` is for Umbraco 8.
* `1.x` is for Umbraco 7.

## Installation

Install from NuGet:

```powershell
dotnet add package Diplo.GodMode --version 17.5.0
```

For CMS 18.2+, use core `18.5.0` and optional AI `18.0.0` instead.

After installation, restart the site and open the Umbraco backoffice. God Mode appears in the **Settings** section. The package manifest is served from:

```text
/App_Plugins/DiploGodMode/umbraco-package.json
```

## Optional God Mode AI Add-On

The *optional* [God Mode AI add-on](https://www.nuget.org/packages/Diplo.GodMode.AI) adds contextual **Explain** buttons powered by Umbraco.AI.

Install from NuGet:

```powershell
dotnet add package Diplo.GodMode.AI --version 17.0.0
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

Overview release checks are enabled by default and use server-side HTTPS requests to NuGet. Set `GodMode:EnableReleaseCheck` to `false` before starting servers that must never contact NuGet. Disabling the setting prevents new lookups; an existing lookup can finish within its three-second deadline. Reload an already-open Overview to hide its release status.

https://github.com/DanDiplo/Umbraco.GodMode/tree/v17

## Links

* GitHub: https://github.com/DanDiplo/Umbraco.GodMode
* Umbraco Marketplace: https://marketplace.umbraco.com/package/diplo.godmode
* NuGet: https://www.nuget.org/packages/Diplo.GodMode
* God Mode AI NuGet: https://www.nuget.org/packages/Diplo.GodMode.AI
* Author: https://www.diplo.co.uk
