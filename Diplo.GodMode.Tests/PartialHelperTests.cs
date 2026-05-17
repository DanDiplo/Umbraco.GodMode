using Diplo.GodMode.Controllers;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class PartialHelperTests
{
    [TestMethod]
    public void GetPartialInfo_ParsesHtmlPartialAsyncAndNormalizesPath()
    {
        const string content = """
            @await Html.PartialAsync("~/Views/Partials/Shared/Card.cshtml", Model)
            """;

        var partial = AssertSinglePartial(content);

        Assert.AreEqual("Shared/Card", partial.Name);
        Assert.AreEqual("Shared/Card.cshtml", partial.Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesCachedPartialWithNestedArguments()
    {
        const string content = """
            @await Html.CachedPartialAsync(
                "Partials/LatestArticles",
                Model,
                TimeSpan.FromMinutes(10),
                cacheByPage: true,
                viewData: new ViewDataDictionary(ViewData)
                {
                    { "Title", "One, Two" }
                })
            """;

        var partial = AssertSinglePartial(content);

        Assert.AreEqual("LatestArticles", partial.Name);
        Assert.AreEqual("LatestArticles.cshtml", partial.Path);
    }

    [TestMethod]
    public void GetPartialInfo_DoesNotMergeSeparatePartialCalls()
    {
        const string content = """
            @Html.Partial("First", new { value = "A" })
            @Html.Partial("Second", new { value = "B" })
            """;

        var partials = PartialHelper.GetPartialInfo(content, 123, "home").ToArray();

        Assert.AreEqual(2, partials.Length);
        Assert.AreEqual("First", partials[0].Name);
        Assert.AreEqual("First.cshtml", partials[0].Path);
        Assert.AreEqual("Second", partials[1].Name);
        Assert.AreEqual("Second.cshtml", partials[1].Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesRenderPartialAsyncInCodeBlock()
    {
        const string content = """
            @{
                await Html.RenderPartialAsync("_AuthorPartial");
            }
            """;

        var partial = AssertSinglePartial(content);

        Assert.AreEqual("_AuthorPartial", partial.Name);
        Assert.AreEqual("_AuthorPartial.cshtml", partial.Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesSynchronousRenderPartial()
    {
        const string content = """
            @{ Html.RenderPartial("_AuthorPartial", Model.AuthorName); }
            """;

        var partial = AssertSinglePartial(content);

        Assert.AreEqual("_AuthorPartial", partial.Name);
        Assert.AreEqual("_AuthorPartial.cshtml", partial.Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesDocumentedRootAndRelativePaths()
    {
        const string content = """
            @await Html.PartialAsync("~/Views/Folder/_PartialName.cshtml")
            @await Html.PartialAsync("/Views/Folder/_OtherPartial.cshtml")
            @await Html.PartialAsync("../Account/_LoginPartial.cshtml")
            """;

        var partials = PartialHelper.GetPartialInfo(content, 123, "home").ToArray();

        Assert.AreEqual(3, partials.Length);
        Assert.AreEqual("~/Views/Folder/_PartialName", partials[0].Name);
        Assert.AreEqual("~/Views/Folder/_PartialName.cshtml", partials[0].Path);
        Assert.AreEqual("/Views/Folder/_OtherPartial", partials[1].Name);
        Assert.AreEqual("/Views/Folder/_OtherPartial.cshtml", partials[1].Path);
        Assert.AreEqual("../Account/_LoginPartial", partials[2].Name);
        Assert.AreEqual("../Account/_LoginPartial.cshtml", partials[2].Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesPartialTagNameAttribute()
    {
        const string content = """
            <partial name="/Views/Partials/Shared/Card.cshtml" model="Model" />
            """;

        var partial = AssertSinglePartial(content);

        Assert.AreEqual("Shared/Card", partial.Name);
        Assert.AreEqual("Shared/Card.cshtml", partial.Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesPartialTagWithSingleQuotesAndNameAfterOtherAttributes()
    {
        const string content = """
            <partial model="Model" view-data="ViewData" name='Partials/LatestArticles'></partial>
            """;

        var partial = AssertSinglePartial(content);

        Assert.AreEqual("LatestArticles", partial.Name);
        Assert.AreEqual("LatestArticles.cshtml", partial.Path);
    }

    [TestMethod]
    public void GetPartialInfo_ParsesDocumentedPartialTagPaths()
    {
        const string content = """
            <partial name="~/Views/Folder/_PartialName.cshtml" />
            <partial name="/Views/Folder/_OtherPartial.cshtml" />
            <partial name="../Account/_LoginPartial.cshtml" />
            """;

        var partials = PartialHelper.GetPartialInfo(content, 123, "home").ToArray();

        Assert.AreEqual(3, partials.Length);
        Assert.AreEqual("~/Views/Folder/_PartialName", partials[0].Name);
        Assert.AreEqual("~/Views/Folder/_PartialName.cshtml", partials[0].Path);
        Assert.AreEqual("/Views/Folder/_OtherPartial", partials[1].Name);
        Assert.AreEqual("/Views/Folder/_OtherPartial.cshtml", partials[1].Path);
        Assert.AreEqual("../Account/_LoginPartial", partials[2].Name);
        Assert.AreEqual("../Account/_LoginPartial.cshtml", partials[2].Path);
    }

    [TestMethod]
    public void GetPartialInfo_IgnoresDynamicPartialNames()
    {
        const string content = """
            @await Html.PartialAsync(partialName, Model)
            """;

        var partials = PartialHelper.GetPartialInfo(content, 123, "home").ToArray();

        Assert.AreEqual(0, partials.Length);
    }

    private static Models.PartialMap AssertSinglePartial(string content)
    {
        var partials = PartialHelper.GetPartialInfo(content, 123, "home").ToArray();

        Assert.AreEqual(1, partials.Length);
        Assert.AreEqual(123, partials[0].TemplateId);
        Assert.AreEqual("home", partials[0].TemplateAlias);

        return partials[0];
    }
}
