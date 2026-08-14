using Diplo.GodMode.Services;
using Umbraco.Cms.Core.PropertyEditors;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class ElementTypeUsageTests
{
    [TestMethod]
    public void GetConfiguredElementTypeReferences_ReturnsContentAndSettingsReferences()
    {
        var contentKey = Guid.NewGuid();
        var settingsKey = Guid.NewGuid();
        var configuration = new BlockListConfiguration
        {
            Blocks =
            [
                new BlockListConfiguration.BlockConfiguration
                {
                    ContentElementTypeKey = contentKey,
                    SettingsElementTypeKey = settingsKey
                }
            ]
        };

        var references = UmbracoDatabaseService.GetConfiguredElementTypeReferences(configuration).ToArray();

        Assert.AreEqual(2, references.Length);
        Assert.IsTrue(references.Any(x => x.ElementTypeKey == contentKey && x.SourceType == "ConfigurationContent"));
        Assert.IsTrue(references.Any(x => x.ElementTypeKey == settingsKey && x.SourceType == "ConfigurationSettings"));
    }

    [TestMethod]
    public void GetConfiguredElementTypeReferences_PreservesRepeatedConfigurationOccurrences()
    {
        var contentKey = Guid.NewGuid();
        var configuration = new BlockGridConfiguration
        {
            Blocks =
            [
                new BlockGridConfiguration.BlockGridBlockConfiguration { ContentElementTypeKey = contentKey },
                new BlockGridConfiguration.BlockGridBlockConfiguration { ContentElementTypeKey = contentKey }
            ]
        };

        var references = UmbracoDatabaseService.GetConfiguredElementTypeReferences(configuration).ToArray();

        Assert.AreEqual(2, references.Length);
        Assert.IsTrue(references.All(x => x.ElementTypeKey == contentKey));
    }

    [TestMethod]
    public void GetConfiguredElementTypeReferences_IgnoresUnsupportedConfiguration()
    {
        var references = UmbracoDatabaseService.GetConfiguredElementTypeReferences(new object()).ToArray();

        Assert.AreEqual(0, references.Length);
    }

    [TestMethod]
    public void BuildElementTypeUsageCte_SqlServerPreservesOccurrencesAndReadsRichTextBlocks()
    {
        var sql = UmbracoDatabaseService.BuildElementTypeUsageCte(includeLibrarySources: false, isSqlite: false);

        StringAssert.Contains(sql, "$.blocks.contentData");
        StringAssert.Contains(sql, "$.blocks.settingsData");
        StringAssert.Contains(sql, "ISJSON(upd.textValue)");
        Assert.IsFalse(sql.Contains("SELECT DISTINCT", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildElementTypeUsageCte_SqlitePreservesOccurrencesAndReadsRichTextBlocks()
    {
        var sql = UmbracoDatabaseService.BuildElementTypeUsageCte(includeLibrarySources: false, isSqlite: true);

        StringAssert.Contains(sql, "$.blocks.contentData");
        StringAssert.Contains(sql, "$.blocks.settingsData");
        StringAssert.Contains(sql, "json_valid(upd.textValue)");
        Assert.IsFalse(sql.Contains("SELECT DISTINCT", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildElementTypeUsageCte_OmitsV18SourcesWhenElementsSchemaIsUnavailable()
    {
        var sql = UmbracoDatabaseService.BuildElementTypeUsageCte(includeLibrarySources: false, isSqlite: false);

        Assert.IsFalse(sql.Contains("umbracoElement", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse(sql.Contains("'ElementPicker'", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildElementTypeUsageCte_IncludesV18SourcesWhenElementsSchemaIsAvailable()
    {
        var sql = UmbracoDatabaseService.BuildElementTypeUsageCte(includeLibrarySources: true, isSqlite: false);

        StringAssert.Contains(sql, "FROM umbracoElement ue");
        StringAssert.Contains(sql, "rt.alias = 'umbElement'");
        StringAssert.Contains(sql, "'ElementPicker'");
        Assert.IsFalse(sql.Contains("SELECT DISTINCT", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void BuildElementTypeUsageKeyPredicate_UsesCaseInsensitiveComparisonForSqlite()
    {
        Assert.AreEqual(
            "ElementTypeKey = @4 COLLATE NOCASE",
            UmbracoDatabaseService.BuildElementTypeUsageKeyPredicate(isSqlite: true));
        Assert.AreEqual(
            "ElementTypeKey = @4",
            UmbracoDatabaseService.BuildElementTypeUsageKeyPredicate(isSqlite: false));
    }
}
