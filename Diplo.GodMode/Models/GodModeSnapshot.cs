namespace Diplo.GodMode.Models
{
    public class GodModeSnapshot
    {
        public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;

        public IEnumerable<ContentTypeMap> ContentTypes { get; set; } = [];

        public IEnumerable<DataTypeMap> DataTypes { get; set; } = [];

        public IEnumerable<ReferenceEdge> ReferenceGraph { get; set; } = [];

        public IEnumerable<TemplateModel> Templates { get; set; } = [];

        public IEnumerable<ConfigurationDriftFinding> ConfigurationDriftFindings { get; set; } = [];

        public IEnumerable<HealthRiskFinding> HealthRiskFindings { get; set; } = [];

        public IEnumerable<Tag> OrphanedTags { get; set; } = [];

        public IEnumerable<MediaMap> OrphanedMedia { get; set; } = [];
    }
}
