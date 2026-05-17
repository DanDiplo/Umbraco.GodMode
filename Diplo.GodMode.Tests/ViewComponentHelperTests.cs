using Diplo.GodMode.Controllers;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class ViewComponentHelperTests
{
    [TestMethod]
    public void GetViewComponentInfo_ParsesQuotedComponentNameAndParameters()
    {
        const string content = """
            @await Component.InvokeAsync("Navigation", new { rootId = Model.Id })
            """;

        var component = AssertSingleComponent(content);

        Assert.AreEqual("Navigation", component.Name);
        Assert.AreEqual("new { rootId = Model.Id }", component.Parameters);
        Assert.IsFalse(component.TagHelper);
    }

    [TestMethod]
    public void GetViewComponentInfo_ParsesNameofComponentName()
    {
        const string content = """
            @await Component.InvokeAsync(nameof(Navigation), new { currentPage = Model })
            """;

        var component = AssertSingleComponent(content);

        Assert.AreEqual("Navigation", component.Name);
        Assert.AreEqual("new { currentPage = Model }", component.Parameters);
    }

    [TestMethod]
    public void GetViewComponentInfo_ParsesTypeofComponentNameWithoutViewComponentSuffix()
    {
        const string content = """
            @await Component.InvokeAsync(typeof(NavigationViewComponent), new { currentPage = Model })
            """;

        var component = AssertSingleComponent(content);

        Assert.AreEqual("Navigation", component.Name);
        Assert.AreEqual("new { currentPage = Model }", component.Parameters);
    }

    [TestMethod]
    public void GetViewComponentInfo_ParsesMultilineParametersWithNestedCommas()
    {
        const string content = """
            @await Component.InvokeAsync(
                "Navigation",
                new
                {
                    title = "One, Two",
                    items = new[] { "A", "B" },
                    route = Url.Action("Index", "Home")
                })
            """;

        var component = AssertSingleComponent(content);

        Assert.AreEqual("Navigation", component.Name);
        StringAssert.Contains(component.Parameters, "title = \"One, Two\"");
        StringAssert.Contains(component.Parameters, "items = new[] { \"A\", \"B\" }");
        StringAssert.Contains(component.Parameters, "Url.Action(\"Index\", \"Home\")");
    }

    [TestMethod]
    public void GetViewComponentInfo_DoesNotMergeSeparateInvokeAsyncCalls()
    {
        const string content = """
            @await Component.InvokeAsync("First", new { value = "A" })
            @await Component.InvokeAsync("Second", new { value = "B" })
            """;

        var components = ViewComponentHelper.GetViewComponentInfo(content, 123, "home").ToArray();

        Assert.AreEqual(2, components.Length);
        Assert.AreEqual("First", components[0].Name);
        Assert.AreEqual("new { value = \"A\" }", components[0].Parameters);
        Assert.AreEqual("Second", components[1].Name);
        Assert.AreEqual("new { value = \"B\" }", components[1].Parameters);
    }

    [TestMethod]
    public void GetViewComponentInfo_ParsesViewComponentTagHelper()
    {
        const string content = """
            <vc:latest-articles count="3" root-id="@Model.Id"></vc:latest-articles>
            """;

        var component = AssertSingleComponent(content);

        Assert.AreEqual("latest-articles", component.Name);
        Assert.AreEqual("count=\"3\" root-id=\"@Model.Id\"", component.Parameters);
        Assert.IsTrue(component.TagHelper);
    }

    private static Models.ComponentMap AssertSingleComponent(string content)
    {
        var components = ViewComponentHelper.GetViewComponentInfo(content, 123, "home").ToArray();

        Assert.AreEqual(1, components.Length);
        Assert.AreEqual(123, components[0].TemplateId);
        Assert.AreEqual("home", components[0].TemplateAlias);

        return components[0];
    }
}
