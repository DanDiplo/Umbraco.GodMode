using Diplo.GodMode.Services;
using Examine.Lucene.Analyzers;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class PropertySearchTests
{
    [TestMethod]
    public void Search_FindsPropertyTextWhileExcludingMediaAndRecycleBin()
        => Assert.AreEqual(2, Count("embedded", "words"));

    [TestMethod]
    public void Search_PhraseMatchesEmbeddedTextAcrossLineBreaks()
        => Assert.AreEqual(1, Count("embedded rich text", "phrase"));

    [TestMethod]
    public void Search_PrefixFindsWholeWordBeginnings()
        => Assert.AreEqual(2, Count("embed", "prefix"));

    [TestMethod]
    public void Search_DoesNotClaimArbitrarySubstringMatching()
        => Assert.AreEqual(0, Count("mbedd", "words"));

    [TestMethod]
    public void Search_AllWordsMustOccurInTheSameProperty()
        => Assert.AreEqual(0, Count("embedded separate", "words"));

    [TestMethod]
    public void Search_DocumentTypeFilterNarrowsResults()
        => Assert.AreEqual(1, Count("embedded", "words", "article"));

    [TestMethod]
    public void Search_ReservedBooleanWordIsLiteralText()
        => Assert.AreEqual(1, Count("OR", "words"));

    [TestMethod]
    public void Search_QuerySyntaxCannotBroadenResults()
        => Assert.AreEqual(0, Count("missing OR *:*", "words"));

    [TestMethod]
    [DataRow("embedded", "words", 2)]
    [DataRow("embedded rich text", "phrase", 1)]
    [DataRow("embed", "prefix", 2)]
    [DataRow("embedded separate", "words", 0)]
    [DataRow("hello embedded rich text missing other words here", "words", 0)]
    public void Search_LargeMultilingualSchemaPreservesMatches(string text, string mode, int expected)
    {
        var fields = Enumerable.Range(0, 1500).Select(i => "property" + i + "_en-us")
            .Append("bodyText_en-us").Append("title").ToArray();
        Assert.AreEqual(expected, Count(text, mode, fields: fields));
    }

    [TestMethod]
    public void Search_LargeSchemaStillAppliesDocumentTypeFilter()
    {
        var fields = Enumerable.Range(0, 1500).Select(i => "property" + i).Append("bodyText_en-us").ToArray();
        Assert.AreEqual(1, Count("embedded", "words", "article", fields));
    }

    [TestMethod]
    public void Snippet_FindsTextNearEndWithoutReturningEntireProperty()
    {
        var value = new string('x', 1500) + " embedded value " + new string('z', 1500);
        var snippet = PropertySearchService.MakeSnippet(value, ["embedded"]);
        StringAssert.Contains(snippet, "embedded");
        Assert.IsLessThanOrEqualTo(422, snippet.Length);
        Assert.IsTrue(snippet.StartsWith('…') && snippet.EndsWith('…'));
    }

    private static int Count(string text, string mode, string? documentType = null, string[]? fields = null)
    {
        using var directory = new RAMDirectory();
        using var analyzer = new CultureInvariantWhitespaceAnalyzer();
        using (var writer = new IndexWriter(directory, new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)))
        {
            void Add(string category, string path, string type, string body, string title = "")
            {
                writer.AddDocument(new Document {
                    new StringField("__IndexType", category, Field.Store.YES),
                    new StringField("path", path, Field.Store.YES),
                    new StringField("__NodeTypeAlias", type, Field.Store.YES),
                    new TextField("bodyText_en-us", body, Field.Store.YES),
                    new TextField("title", title, Field.Store.YES)
                });
            }
            Add("content", "-1,1120,1121", "article", "hello embedded\r\nrich text", "separate");
            Add("content", "-1,1120,1122", "content", "embedded code snippet");
            Add("content", "-1,1120,1123", "content", "choose this or that");
            Add("media", "-1,1145", "image", "embedded rich text");
            Add("content", "-1,-20,1191", "article", "embedded rich text");
            writer.Commit();
        }
        using var reader = DirectoryReader.Open(directory);
        var query = new QueryParser(LuceneVersion.LUCENE_48, "bodyText_en-us", analyzer)
            .Parse(PropertySearchService.BuildQuery(text, mode, fields ?? ["bodyText_en-us", "title"], documentType));
        return new IndexSearcher(reader).Search(query, 10).TotalHits;
    }
}
