# Umbraco.GodMode
**Diplo God Mode makes Umbraco 8 developers invincible!**

This custom tree in the **Settings** section of **Umbraco 8** allows you to browse, query and search your document types and compositions; your templates and partials; your datatypes and property editors; your media library; your custom controllers and models. IT also provides diagnostics about your Umbraco set-up and the server it is running on.

For instance, you can:

* Quickly search doc types, templates, editors, media etc.
* Easily see which doc types inherit from any of your compositions
* See which document types use which property editor or data type instance
* See which partials are used by all your templates
* Find out which data types are being used (or not)
* View all content pages in a searchable and filterable table (using fast, server-side pagination)
* View all Umbraco members and filter them by assigned group
* Browse all media in the Media Library and sort it by file type, size or media type
* See which controllers (Surface, API and RenderMvc) are being used and in what namespaces and DLLs
* View all generated models (that inherit from PublishedContentModel)
* Clear internal Umbraco caches and even restart App Pool
* View diagnostics and configuration settings about your site and server
* Warm up compilation of all templates ("views") in one bound... erm, click.
* Plus lots more!

## Screenshots

![Doc Type Browser](https://www.diplo.co.uk/media/1189/doctypebrowser.png)

![Doc Type Browser](https://www.diplo.co.uk/media/1190/doctypedetail.png)


### Demo

**YouTube:** https://www.youtube.com/watch?v=tkupJOMnOlw

**Blog Post:** http://www.diplo.co.uk/blog/web-development/god-mode-umbraco-7-package/

## Install

*Important: This is for Umbraco 8.1 and above. Use the 1.x version in the v7 branch for Umbraco 7.

**NuGet:** https://www.nuget.org/packages/Diplo.GodMode/

`PM> Install-Package Diplo.GodMode`

**Our Umbraco:** https://our.umbraco.org/projects/developer-tools/diplo-god-mode/
