using Diplo.GodMode.Models;

namespace Diplo.GodMode.Services.Interfaces
{
    /// <summary>
    /// Server clock, time zone and UTC date diagnostics
    /// </summary>
    public interface IServerTimeService
    {
        bool AutomaticServerRegistration { get; }

        /// <summary>
        /// Gets the server time zone and clock details. Cheap enough to call for clock drift checks.
        /// </summary>
        ServerTimeInfo GetServerTime();

        /// <summary>
        /// Gathers evidence of how the Umbraco 17 UTC system date migration ran (log entries and future dated rows).
        /// </summary>
        SystemDateEvidence GetSystemDateEvidence();

        /// <summary>
        /// Gets pending scheduled publishing plus the state of the jobs and servers that process it.
        /// </summary>
        ContentScheduleOverview GetContentSchedules();
    }
}
