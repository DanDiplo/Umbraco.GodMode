using Diplo.GodMode.Models;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;

namespace Diplo.GodMode.Services;

/// <summary>Conservative, bounded removal of old inactive server registrations.</summary>
public sealed class ServerRegistrationCleanupService(
    ICoreScopeProvider scopes,
    IServerRegistrationRepository repository,
    IHostingEnvironment hosting,
    TimeProvider timeProvider,
    ILogger<ServerRegistrationCleanupService> logger)
{
    public const int MinimumAgeDays = 30;
    public const int MaximumBatchSize = 100;

    public ServerRegistrationCleanupPreview Preview()
    {
        var identity = CurrentIdentity();
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-MinimumAgeDays);
        using var scope = scopes.CreateCoreScope(autoComplete: true);
        repository.ClearCache();
        var candidates = repository.GetMany().Where(x => IsEligible(x, identity, cutoff))
            .OrderBy(x => x.UpdateDate).ThenBy(x => x.Id).ToList();
        return new()
        {
            MinimumAgeDays = MinimumAgeDays,
            MaximumBatchSize = MaximumBatchSize,
            CutoffUtc = cutoff,
            TotalEligible = candidates.Count,
            Servers = candidates.Take(MaximumBatchSize).Select(x => new ServerRegistrationCleanupItem
            {
                Id = x.Id, Identity = x.ServerIdentity ?? string.Empty, Address = x.ServerAddress ?? string.Empty,
                RegisteredDate = x.CreateDate, LastNotifiedDate = x.UpdateDate
            }).ToList()
        };
    }

    public ServerRegistrationCleanupResult Delete(ServerRegistrationCleanupRequest request, string actor)
    {
        if (request is null || !request.Confirmed || request.Ids is null || request.Ids.Length is < 1 or > MaximumBatchSize
            || request.Ids.Any(x => x <= 0) || request.Ids.Distinct().Count() != request.Ids.Length)
            throw new ArgumentException($"Confirm between 1 and {MaximumBatchSize} distinct registration IDs.");

        var identity = CurrentIdentity();
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-MinimumAgeDays);
        var result = new ServerRegistrationCleanupResult();
        using (var scope = scopes.CreateCoreScope())
        {
            // Serialize with TouchServer/election. Read fresh state only after acquiring this lock.
            scope.WriteLock(Constants.Locks.Servers);
            repository.ClearCache();
            try
            {
                var registrations = repository.GetMany().ToDictionary(x => x.Id);
                foreach (var id in request.Ids)
                {
                    if (!registrations.TryGetValue(id, out var registration) || !IsEligible(registration, identity, cutoff))
                    {
                        result.SkippedIds.Add(id);
                        continue;
                    }
                    repository.Delete(registration);
                    result.DeletedIds.Add(id);
                }
                scope.Complete();
            }
            finally { repository.ClearCache(); }
        }

        logger.LogInformation("Server registration cleanup by {Actor}: deleted {DeletedIds}; skipped {SkippedIds}. Minimum age {MinimumAgeDays} days.",
            actor, string.Join(",", result.DeletedIds), string.Join(",", result.SkippedIds), MinimumAgeDays);
        return result;
    }

    private string CurrentIdentity()
    {
        if (string.IsNullOrWhiteSpace(hosting.ApplicationId))
            throw new InvalidOperationException("The current application identity is unavailable; cleanup is disabled.");
        // Same identity format used by Umbraco's ServerRegistrationService.
        return Environment.MachineName + "/" + hosting.ApplicationId;
    }

    internal static bool IsEligible(IServerRegistration server, string currentIdentity, DateTime cutoffUtc)
        => server.Id > 0 && !server.IsActive && !server.IsSchedulingPublisher
            && !string.IsNullOrWhiteSpace(server.ServerIdentity)
            && !string.Equals(server.ServerIdentity, currentIdentity, StringComparison.OrdinalIgnoreCase)
            && server.UpdateDate > DateTime.MinValue && server.CreateDate > DateTime.MinValue
            && server.UpdateDate < cutoffUtc && server.CreateDate < cutoffUtc;
}
