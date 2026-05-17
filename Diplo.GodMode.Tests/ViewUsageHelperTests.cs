using Diplo.GodMode.Controllers;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class ViewUsageHelperTests
{
    [TestMethod]
    public void GetAssetInfo_ParsesScriptsStylesImagesAndCssUrls()
    {
        const string content = """
            <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
            <link rel="preload" as="style" href="https://cdn.example.com/print.css" />
            <script src="/scripts/site.js" defer></script>
            <script type="module">console.log("inline");</script>
            <style>.hero { background-image: url('/media/hero.jpg'); }</style>
            <img src="//cdn.example.com/logo.png" asp-append-version="true" />
            """;

        var assets = ViewUsageHelper.GetAssetInfo(content, 123, "home").ToArray();

        Assert.AreEqual(7, assets.Length);
        Assert.IsTrue(assets.Any(asset => asset.Kind == "Stylesheet" && asset.Url == "~/css/site.css" && !asset.IsExternal));
        Assert.IsTrue(assets.Any(asset => asset.Kind == "Stylesheet" && asset.Host == "cdn.example.com" && asset.IsExternal));
        Assert.IsTrue(assets.Any(asset => asset.Kind == "Script" && asset.Url == "/scripts/site.js" && !asset.IsInline));
        Assert.IsTrue(assets.Any(asset => asset.Kind == "Script" && asset.IsInline));
        Assert.IsTrue(assets.Any(asset => asset.Kind == "Style" && asset.IsInline));
        Assert.IsTrue(assets.Any(asset => asset.Kind == "CssUrl" && asset.Url == "/media/hero.jpg"));
        Assert.IsTrue(assets.Any(asset => asset.Kind == "Image" && asset.Host == "cdn.example.com" && asset.IsExternal));
    }

    [TestMethod]
    public void GetAssetInfo_ResolvesLocalAssetsAgainstWebRoot()
    {
        var webRoot = Directory.CreateTempSubdirectory("godmode-assets-").FullName;

        try
        {
            Directory.CreateDirectory(Path.Combine(webRoot, "scripts"));
            File.WriteAllText(Path.Combine(webRoot, "scripts", "site.js"), string.Empty);

            const string content = """
                <script src="/scripts/site.js?v=1"></script>
                <link rel="stylesheet" href="~/css/missing.css" />
                <script src="https://cdn.example.com/site.js"></script>
                <script src="@Url.Content("~/scripts/dynamic.js")"></script>
                """;

            var assets = ViewUsageHelper.GetAssetInfo(content, 123, "home", webRoot).ToArray();
            var foundScript = assets.Single(asset => asset.Url == "/scripts/site.js?v=1");
            var missingCss = assets.Single(asset => asset.Url == "~/css/missing.css");
            var cdnScript = assets.Single(asset => asset.Url == "https://cdn.example.com/site.js");
            var dynamicScript = assets.Single(asset => asset.Url.Contains("@Url.Content"));

            Assert.IsTrue(foundScript.IsResolved);
            Assert.IsTrue(foundScript.Exists);
            Assert.AreEqual(string.Empty, foundScript.Warning);

            Assert.IsTrue(missingCss.IsResolved);
            Assert.IsFalse(missingCss.Exists);
            Assert.AreEqual("Asset file was not found under the web root.", missingCss.Warning);

            Assert.IsFalse(cdnScript.IsResolved);
            Assert.IsFalse(dynamicScript.IsResolved);
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [TestMethod]
    public void GetAssetInfo_DoesNotTreatRazorGetCropUrlAsCssUrl()
    {
        const string content = """
            <img src="@Model.MainImage.GetCropUrl(400)" alt="@Model.Name" />
            """;

        var assets = ViewUsageHelper.GetAssetInfo(content, 123, "author").ToArray();

        Assert.AreEqual(1, assets.Length);
        Assert.AreEqual("Image", assets[0].Kind);
        Assert.AreEqual("@Model.MainImage.GetCropUrl(400)", assets[0].Url);
    }

    [TestMethod]
    public void GetSectionInfo_ParsesDeclaredSections()
    {
        const string content = """
            @section Styles { <link rel="stylesheet" href="~/css/page.css" /> }
            @section Scripts { <script src="~/js/page.js"></script> }
            """;

        var sections = ViewUsageHelper.GetSectionInfo(content, 123, "home").ToArray();

        Assert.AreEqual(2, sections.Length);
        Assert.IsTrue(sections.Any(section => section.Name == "Styles"));
        Assert.IsTrue(sections.Any(section => section.Name == "Scripts"));
    }

    [TestMethod]
    public void GetFormInfo_ParsesFormTagAndBeginForm()
    {
        const string content = """
            <form method="post" asp-controller="Contact" asp-action="Send">
                @Html.AntiForgeryToken()
            </form>
            @using (Html.BeginForm("Search", "Results", FormMethod.Get)) { }
            """;

        var forms = ViewUsageHelper.GetFormInfo(content, 123, "home").ToArray();

        Assert.AreEqual(2, forms.Length);
        Assert.AreEqual("Tag", forms[0].Kind);
        Assert.AreEqual("post", forms[0].Method);
        Assert.AreEqual("Send", forms[0].Action);
        Assert.AreEqual("Contact", forms[0].Controller);
        Assert.IsTrue(forms[0].HasAntiForgeryToken);
        Assert.AreEqual("HtmlHelper", forms[1].Kind);
        Assert.AreEqual("Search", forms[1].Action);
        Assert.AreEqual("Results", forms[1].Controller);
    }

    [TestMethod]
    public void GetTagHelperInfo_ParsesAspNetAndEnvironmentTagHelpers()
    {
        const string content = """
            <a asp-controller="Blog" asp-action="Index" asp-route-page="2">Blog</a>
            <img src="~/images/logo.png" asp-append-version="true" />
            <environment include="Development"><script src="~/js/debug.js"></script></environment>
            """;

        var tagHelpers = ViewUsageHelper.GetTagHelperInfo(content, 123, "home").ToArray();

        Assert.AreEqual(3, tagHelpers.Length);
        Assert.IsTrue(tagHelpers.Any(tag => tag.TagName == "a" && tag.Kind == "Anchor"));
        Assert.IsTrue(tagHelpers.Any(tag => tag.TagName == "img" && tag.Kind == "Image"));
        Assert.IsTrue(tagHelpers.Any(tag => tag.TagName == "environment" && tag.Kind == "Environment"));
    }

    [TestMethod]
    public void GetUmbracoUsageInfo_ParsesCommonUmbracoCalls()
    {
        const string content = """
            @Model.Value<string>("pageTitle")
            @Model.HasValue("summary")
            @item.Value("cardTitle")
            @Umbraco.Content(Guid.Parse("11111111-1111-1111-1111-111111111111"))
            @Umbraco.Media("22222222-2222-2222-2222-222222222222")
            @await Html.GetBlockGridHtmlAsync(Model, "contentBlocks")
            @await Html.GetBlockListHtmlAsync("relatedLinks")
            """;

        var usages = ViewUsageHelper.GetUmbracoUsageInfo(content, 123, "home").ToArray();

        Assert.IsTrue(usages.Any(usage => usage.Kind == "Property Alias" && usage.Name == "pageTitle"));
        Assert.IsTrue(usages.Any(usage => usage.Kind == "Property Alias" && usage.Name == "summary"));
        Assert.IsTrue(usages.Any(usage => usage.Kind == "Property Alias" && usage.Name == "cardTitle"));
        Assert.IsTrue(usages.Any(usage => usage.Kind == "Umbraco Helper" && usage.Expression == "Umbraco.Content"));
        Assert.IsTrue(usages.Any(usage => usage.Kind == "Umbraco Helper" && usage.Expression == "Umbraco.Media"));
        Assert.IsTrue(usages.Any(usage => usage.Kind == "Block Rendering" && usage.Name == "contentBlocks"));
        Assert.IsTrue(usages.Any(usage => usage.Kind == "Block Rendering" && usage.Name == "relatedLinks"));
    }
}
