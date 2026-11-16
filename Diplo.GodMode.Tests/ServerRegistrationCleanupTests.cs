using System.Reflection;
using Diplo.GodMode.Controllers;
using Diplo.GodMode.Models;
using Diplo.GodMode.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Diplo.GodMode.Tests;

[TestClass]
public sealed class ServerRegistrationCleanupTests
{
    private static readonly DateTime Now = new(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc);
    private static string CurrentIdentity => Environment.MachineName + "/cleanup-tests";

    [TestMethod]
    public void Preview_ExcludesActivePublisherCurrentRecentUnknownAndBoundaryRows()
    {
        var rows = new List<IServerRegistration> { Row(1), Row(2), Row(3), Row(4), Row(5), Row(6), Row(7), Row(8), Row(9) };
        rows[1].IsActive = true;
        rows[2].IsSchedulingPublisher = true;
        rows[3].ServerIdentity = CurrentIdentity.ToUpperInvariant();
        rows[4].UpdateDate = Now.AddDays(-2);
        rows[5].CreateDate = Now.AddDays(-2);
        rows[6].UpdateDate = Now.AddDays(-30);
        rows[7].ServerIdentity = "";
        rows[8].UpdateDate = DateTime.MinValue;
        var fixture = new Fixture(rows);

        var preview = fixture.Service.Preview();

        Assert.AreEqual(1, preview.TotalEligible);
        CollectionAssert.AreEqual(new[] { 1 }, preview.Servers.Select(x => x.Id).ToArray());
        Assert.AreEqual(Now.AddDays(-30), preview.CutoffUtc);
        Assert.AreEqual(0, fixture.Deleted.Count);
    }

    [TestMethod]
    public void Preview_BoundsTheBatchButReportsTheFullEligibleCount()
    {
        var fixture = new Fixture(Enumerable.Range(1, 101).Select(Row).ToList());
        var preview = fixture.Service.Preview();
        Assert.AreEqual(101, preview.TotalEligible);
        Assert.AreEqual(100, preview.Servers.Count);
        Assert.AreEqual(100, preview.MaximumBatchSize);
    }

    [TestMethod]
    public void Delete_RechecksEligibilityUnderLockAndOnlyDeletesRequestedEligibleRows()
    {
        var rows = Enumerable.Range(1, 7).Select(Row).ToList();
        var fixture = new Fixture(rows);
        fixture.Service.Preview();
        // State changed after the administrator reviewed the candidates.
        rows[1].IsActive = true;
        rows[2].IsSchedulingPublisher = true;
        rows[3].ServerIdentity = CurrentIdentity;
        rows[4].UpdateDate = Now;
        rows.RemoveAt(5);
        fixture.Calls.Clear();

        var result = fixture.Service.Delete(new() { Confirmed = true, Ids = [1, 2, 3, 4, 5, 6] }, "test-admin");

        CollectionAssert.AreEqual(new[] { 1 }, result.DeletedIds);
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6 }, result.SkippedIds);
        CollectionAssert.AreEqual(new[] { 1 }, fixture.Deleted);
        Assert.IsTrue(fixture.Calls.IndexOf("WriteLock") < fixture.Calls.IndexOf("ClearCache"));
        Assert.IsTrue(fixture.Calls.IndexOf("ClearCache") < fixture.Calls.IndexOf("GetMany"));
        Assert.IsTrue(fixture.Calls.Contains("Complete"));
        Assert.AreEqual("Dispose", fixture.Calls.Last());
    }

    [TestMethod]
    public void Delete_RejectsUnconfirmedEmptyOversizedDuplicateAndInvalidIdsBeforeOpeningScope()
    {
        var fixture = new Fixture([Row(1)]);
        ServerRegistrationCleanupRequest[] requests =
        [
            new() { Ids = [1] },
            new() { Confirmed = true },
            new() { Confirmed = true, Ids = Enumerable.Range(1, 101).ToArray() },
            new() { Confirmed = true, Ids = [1, 1] },
            new() { Confirmed = true, Ids = [0] }
        ];
        foreach (var request in requests)
            Assert.ThrowsExactly<ArgumentException>(() => fixture.Service.Delete(request, "test-admin"));
        Assert.AreEqual(0, fixture.Calls.Count);
        Assert.AreEqual(0, fixture.Deleted.Count);
    }

    [TestMethod]
    public void Delete_FailureDoesNotCompleteTransactionAndClearsRegistrationCache()
    {
        var fixture = new Fixture([Row(1), Row(2)]) { FailDelete = true };
        Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Service.Delete(new() { Confirmed = true, Ids = [1, 2] }, "test-admin"));
        Assert.IsFalse(fixture.Calls.Contains("Complete"));
        Assert.AreEqual("ClearCache", fixture.Calls[^2]);
        Assert.AreEqual("Dispose", fixture.Calls[^1]);
    }

    [TestMethod]
    public void UnknownApplicationIdentity_DisablesCleanup()
    {
        var fixture = new Fixture([Row(1)], applicationId: "");
        Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Service.Preview());
        Assert.ThrowsExactly<InvalidOperationException>(() => fixture.Service.Delete(new() { Confirmed = true, Ids = [1] }, "test-admin"));
        Assert.AreEqual(0, fixture.Calls.Count);
    }

    [TestMethod]
    public void BothCleanupEndpoints_RequireAdministratorPolicyInAdditionToControllerPolicy()
    {
        foreach (var method in new[] { nameof(GodModeApiController.PreviewServerCleanup), nameof(GodModeApiController.DeleteServerRegistrations) })
            Assert.IsTrue(typeof(GodModeApiController).GetMethod(method)!.GetCustomAttributes<AuthorizeAttribute>()
                .Any(x => x.Policy == AuthorizationPolicies.RequireAdminAccess));
        Assert.IsTrue(typeof(GodModeApiController).GetCustomAttributes<AuthorizeAttribute>()
            .Any(x => x.Policy == AuthorizationPolicies.SectionAccessSettings));
    }

    private static IServerRegistration Row(int id) => new ServerRegistration("https://old.example/", "old-" + id, Now.AddDays(-60))
    {
        Id = id, CreateDate = Now.AddDays(-60), UpdateDate = Now.AddDays(-60), IsActive = false, IsSchedulingPublisher = false
    };

    private sealed class Fixture
    {
        public List<string> Calls { get; } = [];
        public List<int> Deleted { get; } = [];
        public bool FailDelete { get; init; }
        public ServerRegistrationCleanupService Service { get; }

        public Fixture(List<IServerRegistration> rows, string applicationId = "cleanup-tests")
        {
            var scope = Stub<ICoreScope>((name, args) =>
            {
                Calls.Add(name);
                if (name == "WriteLock") CollectionAssert.AreEqual(new[] { Constants.Locks.Servers }, (int[])args![0]!);
                return name == "Complete" ? true : null;
            });
            var scopes = Stub<ICoreScopeProvider>((name, _) => { Calls.Add(name); return scope; });
            var repository = Stub<IServerRegistrationRepository>((name, args) =>
            {
                Calls.Add(name);
                if (name == "GetMany") return rows.ToArray();
                if (name == "Delete")
                {
                    if (FailDelete) throw new InvalidOperationException("database failure");
                    Deleted.Add(((IServerRegistration)args![0]!).Id);
                }
                return null;
            });
            var hosting = Stub<IHostingEnvironment>((_, _) => applicationId);
            Service = new(scopes, repository, hosting, new FixedTime(), NullLogger<ServerRegistrationCleanupService>.Instance);
        }
    }

    private sealed class FixedTime : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }

    private static T Stub<T>(Func<string, object?[]?, object?> call) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).Call = call;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<string, object?[]?, object?> Call { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Call(method!.Name, args);
    }
}
