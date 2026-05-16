using System.Text.Json;

namespace Diplo.GodMode.AI.Services
{
    public class GodModeAiExplainRequest
    {
        public string SubjectType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public JsonElement Data { get; set; }

        public JsonElement? Context { get; set; }
    }

    public class GodModeAiExplainResponse
    {
        public string Summary { get; set; } = string.Empty;

        public string PrimaryDiagnosis { get; set; } = string.Empty;

        public string WhereToLook { get; set; } = string.Empty;

        public string LikelyCause { get; set; } = string.Empty;

        public List<string> HowToFix { get; set; } = [];

        public string WhatItIs { get; set; } = string.Empty;

        public string WhyItMatters { get; set; } = string.Empty;

        public List<string> ObservedDetails { get; set; } = [];

        public string Risk { get; set; } = string.Empty;

        public List<string> SuggestedNextSteps { get; set; } = [];
    }
}
