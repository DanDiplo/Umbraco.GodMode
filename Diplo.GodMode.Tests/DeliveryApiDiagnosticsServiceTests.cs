using Diplo.GodMode.Services;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class DeliveryApiDiagnosticsServiceTests
{
    [TestMethod]
    [DataRow("author", "auth", false)]
    [DataRow("authorList", "auth", false)]
    [DataRow("authToken", "auth", true)]
    [DataRow("memberAuth", "auth", true)]
    [DataRow("member-auth", "auth", true)]
    [DataRow("secureSettings", "setting", false)]
    [DataRow("secureSettings", "secure", true)]
    public void AliasContainsTerm_MatchesAliasTermsOnly(string alias, string term, bool expected)
        => Assert.AreEqual(expected, DeliveryApiDiagnosticsService.AliasContainsTerm(alias, term));
}
