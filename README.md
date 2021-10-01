# Umbraco GodMode
**Diplo God Mode makes Umbraco 9 developers invincible!**

This custom tree in the **Settings** section of **Umbraco 9** allows you to browse, query and search your document types and compositions; your templates and partials; your datatypes and property editors; your media library; your custom controllers and models. It also provides diagnostics about your Umbraco set-up and the server it is running on.

For instance, you can:

* Quickly search doc types, templates, editors, media etc.
* Easily see which doc types inherit from any of your compositions
* See which document types use which property editor or data type instance
* Distinguish between element types and types that vary by culture or segment
* See which partials are used by all your templates (and which are cached)
* Find out which data types are being used (or not)
* View all content pages in a searchable and filterable table (using fast, server-side pagination) and view nuCache data
* View all Umbraco members and filter them by assigned group
* Browse all media in the Media Library and filter by type
* List all tags and the content associated with the tag. Find orphaned tags
* See which controllers (Surface, API and RenderMvc) are being used and in what namespaces and DLLs
* View all generated models (that inherit from `PublishedContentModel`)
* Clear internal Umbraco caches and even restart App Pool
* View diagnostics and configuration settings about your Umbraco site and hosting environment
* Warm up compilation of all templates ("views") in a single bound... erm, click.
* Plus lots more!

## Screenshots

![Doc Type Browser](https://www.diplo.co.uk/media/1189/doctypebrowser.png)

![Doc Type Browser](https://www.diplo.co.uk/media/1190/doctypedetail.png)

### Demo

**YouTube:** https://www.youtube.com/watch?v=xLjTV5LMp44&t=7s (v7)

**Blog Post:** https://www.diplo.co.uk/blog/web-development/god-mode-umbraco-8-package/ (v8 version)

## Download & Installation

***Important!**: This is for Umbraco 9 and above. Use v8 branch for the Umbraco 8 version or the v7 branch for the Umbraco 7 branch.

**NuGet:** https://www.nuget.org/packages/Diplo.GodMode/

`PM> Install-Package Diplo.GodMode`

**Our Umbraco:** https://our.umbraco.org/projects/developer-tools/diplo-god-mode/

### Usage

After installation you should see an new **God Mode** tree in the **Settings** (Third Party) section within Umbraco.

If you don't, try clearing your browser cache.

### Building / Developing

The v9 repository comes with two solutions:

`Diplo.GodMode` - this is the GodMode plugin source code.
`Diplo.GodMode.TestSite` - this is a demo Umbraco 9 site that can be used to view and test the plugin.

**Note:** You'll need to type `dotnet restore` within the `Diplo.GodMode.TestSite` folder to restore.

Once you have restored then when you build the test site it will import changes from the plugin.

See the [Umbraco 9 docs on packages](https://our.umbraco.com/documentation/UmbracoNetCoreUpdates?_ga=2.99408024.785525998.1632846711-370550528.1632846711#package-development) for more info on how the test site is linked to the plugin.
