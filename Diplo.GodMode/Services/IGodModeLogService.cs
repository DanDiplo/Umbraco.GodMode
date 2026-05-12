using Diplo.GodMode.Models;
using NPoco;

namespace Diplo.GodMode.Services;

public interface IGodModeLogService
{
    GodModeLogOverview GetOverview();

    Page<GodModeLogEvent> GetLogs(long page, long pageSize, DateTimeOffset? from, DateTimeOffset? to, string? level, string? search);
}
