namespace Diplo.GodMode.Models
{
    public class GodModeAnalysisRequest
    {
        public string ProviderAlias { get; set; }

        public string Prompt { get; set; }

        public GodModeSnapshot Snapshot { get; set; }
    }
}
