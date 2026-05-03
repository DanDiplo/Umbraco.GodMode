namespace Diplo.GodMode.Models
{
    public class HealthRiskFinding
    {
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
