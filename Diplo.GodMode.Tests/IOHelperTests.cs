using Diplo.GodMode.Helpers;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class IOHelperTests
{
    [TestMethod]
    public void ResolveConfiguredContentRootPath_ReturnsAbsolutePathUnchanged()
    {
        var absolutePath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "UmbLogs"));

        var result = IOHelper.ResolveConfiguredContentRootPath(CreateContentRoot(), absolutePath);

        Assert.AreEqual(absolutePath, result);
    }

    [TestMethod]
    public void ResolveConfiguredContentRootPath_ResolvesTildePathFromContentRoot()
    {
        var contentRoot = CreateContentRoot();

        var result = IOHelper.ResolveConfiguredContentRootPath(contentRoot, "~/umbraco/Logs");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(contentRoot, "umbraco", "Logs")), result);
    }

    [TestMethod]
    public void ResolveConfiguredContentRootPath_ResolvesRelativeSiblingPathFromContentRoot()
    {
        var contentRoot = CreateContentRoot();

        var result = IOHelper.ResolveConfiguredContentRootPath(contentRoot, "../MediaCache");

        Assert.AreEqual(Path.GetFullPath(Path.Combine(contentRoot, "..", "MediaCache")), result);
    }

    private static string CreateContentRoot()
        => Path.GetFullPath(Path.Combine(Path.GetTempPath(), "GodModeSite"));
}
