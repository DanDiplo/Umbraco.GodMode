# Umbraco GodMode
**Diplo God Mode makes Umbraco 8 developers invincible!**

This custom tree in the **Settings** section of **Umbraco 8** allows you to browse, query and search your document types and compositions; your templates and partials; your datatypes and property editors; your media library; your custom controllers and models. It also provides diagnostics about your Umbraco set-up and the server it is running on.

For instance, you can:

* Quickly search doc types, templates, editors, media etc.
* Easily see which doc types inherit from any of your compositions
* See which document types use which property editor or data type instance
* See which partials are used by all your templates (and which are cached)
* Find out which data types are being used (or not)
* View all content pages in a searchable and filterable table (using fast, server-side pagination)
* View all Umbraco members and filter them by assigned group
* Browse all media in the Media Library and filter by type
* See which controllers (Surface, API and RenderMvc) are being used and in what namespaces and DLLs
* View all generated models (that inherit from `PublishedContentModel`)
* Clear internal Umbraco caches and even restart App Pool
* View diagnostics and configuration settings about your Umbraco site and hosting environment
* Warm up compilation of all templates ("views") in one bound... erm, click.
* Plus lots more!

## Screenshots

![Doc Type Browser](https://www.diplo.co.uk/media/1189/doctypebrowser.png)

![Doc Type Browser](https://www.diplo.co.uk/media/1190/doctypedetail.png)

### Demo

**YouTube:** https://www.youtube.com/watch?v=xLjTV5LMp44&t=7s

**Blog Post:** https://www.diplo.co.uk/blog/web-development/god-mode-umbraco-8-package/

## Download & Installation

***Important!**: This is for Umbraco 8.1 and above. Use the 1.x version in the v7 branch for Umbraco 7. For more info on the v7 version please [read this post](https://www.diplo.co.uk/blog/web-development/god-mode-umbraco-7-package/).

**NuGet:** https://www.nuget.org/packages/Diplo.GodMode/

`PM> Install-Package Diplo.GodMode`

**Our Umbraco:** https://our.umbraco.org/projects/developer-tools/diplo-god-mode/

### Usage

After installation you should see an new **God Mode** tree in the **Settings** (Third Party) section within Umbraco. 

If you have any issues with this then try restarting your site and bumping the `ClientDependency.config` version in the `/config/` folder of your Umbraco installation. Clearing your browser cache can also help with cached script files. Check your browser developer console for issues.

Further information can be found in [this blog post](https://www.diplo.co.uk/blog/web-development/god-mode-umbraco-8-package/).
