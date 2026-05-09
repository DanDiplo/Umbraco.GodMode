namespace Diplo.GodMode.Models
{
    public class GodModeAnalysisResult
    {
        public string ProviderAlias { get; set; }

        public string ProviderName { get; set; }

        public string Summary { get; set; }

        public IEnumerable<GodModeAnalysisFinding> Findings { get; set; } = [];

        public IEnumerable<string> SuggestedNextSteps { get; set; } = [];

        public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
