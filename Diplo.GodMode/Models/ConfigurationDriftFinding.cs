namespace Diplo.GodMode.Models
{
    public class ConfigurationDriftFinding
    {
        public string Severity { get; set; }

        public int Score { get; set; }

        public string Category { get; set; }

        public string EntityType { get; set; }

        public string EntityName { get; set; }

        public string EntityAlias { get; set; }

        public string EntityKey { get; set; }

        public IEnumerable<string> ComparedWith { get; set; } = [];

        public string Summary { get; set; }

        public IEnumerable<string> DifferingFields { get; set; } = [];

        public string Recommendation { get; set; }
    }
}
