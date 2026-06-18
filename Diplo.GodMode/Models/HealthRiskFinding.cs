namespace Diplo.GodMode.Models
{
    public class HealthRiskFinding
    {
        /// <summary>
        /// A stable identifier for the kind of check that produced this finding.
        /// Useful for de-duplicating, snoozing or deep-linking individual checks.
        /// </summary>
        public string CheckId { get; set; }

        public string Severity { get; set; }

        public int Score { get; set; }

        public string Category { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public string EntityType { get; set; }

        public string EntityName { get; set; }

        public string EntityAlias { get; set; }

        public string EntityKey { get; set; }

        public string Recommendation { get; set; }
    }
}
