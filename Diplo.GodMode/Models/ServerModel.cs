using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diplo.GodMode.Models
{
    /// <summary>
    /// Represents registered server information
    /// </summary>
    public class ServerModel
    {
        public int Id { get; set; }

        public string Address { get; set; }

        public string ComputerName { get; set; }

        public DateTime RegisteredDate { get; set; }

        public DateTime LastNotifiedDate { get; set; }

        public bool IsActive { get; set; }

        public bool IsSchedulingPublisher { get; set; }

        /// <summary>
        /// Servers check in every minute or so; this long without one suggests the server has gone.
        /// </summary>
        public static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(10);

        public static bool IsStaleAt(DateTime lastNotifiedUtc, DateTime utcNow)
            => DateTime.SpecifyKind(lastNotifiedUtc, DateTimeKind.Utc) < utcNow - StaleAfter;

        public string ToDiagnostic(bool automaticRegistration = true)
        {
            var stale = !automaticRegistration ? " (historical registration: automatic check-ins disabled)"
                : IsStaleAt(this.LastNotifiedDate, DateTime.UtcNow) ? " (stale: no check-in for over " + StaleAfter.TotalMinutes + " minutes)" : String.Empty;
            return String.Format("{0}{1} - Registered: {2:yyyy-MM-dd HH:mm:ss} UTC, Last check-in: {3:yyyy-MM-dd HH:mm:ss} UTC{4}, Active: {5}, Is Scheduling Publisher (Master)?: {6}", this.IsSchedulingPublisher ? "* " : String.Empty, this.Address, this.RegisteredDate, this.LastNotifiedDate, stale, this.IsActive, this.IsSchedulingPublisher);
        }
    }
}
