namespace Diplo.GodMode.Models
{
    public class GodModeAnalysisFinding
    {
        public string Severity { get; set; }

        public int Score { get; set; }

        public string Category { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public string EntityType { get; set; }

        public string EntityName { get; set; }

        public string EntityAlias { get; set; }

        public IEnumerable<GodModeAffectedEntity> AffectedEntities { get; set; } = [];

        public string Recommendation { get; set; }
    }
}
