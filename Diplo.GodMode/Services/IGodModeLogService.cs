using Diplo.GodMode.Models;
using NPoco;

namespace Diplo.GodMode.Services;

public interface IGodModeLogService
{
    GodModeLogOverview GetOverview();

    Page<GodModeLogEvent> GetLogs(long page, long pageSize, DateTimeOffset? from, DateTimeOffset? to, string? level, string? search, string? queryExpression);

    IEnumerable<GodModeLogInsight> GetInsights(DateTimeOffset? from, DateTimeOffset? to, int take);

    IEnumerable<GodModeLogLevelCount> GetLevelCounts(DateTimeOffset? from, DateTimeOffset? to, string? search, string? queryExpression);

    IEnumerable<GodModeSavedLogQuery> GetSavedQueries();
}
