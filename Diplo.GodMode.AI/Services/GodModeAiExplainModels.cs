using System.Text.Json;

namespace Diplo.GodMode.AI.Services
{
    public class GodModeAiExplainRequest
    {
        public string SubjectType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public JsonElement Data { get; set; }
    }

    public class GodModeAiExplainResponse
    {
        public string Summary { get; set; } = string.Empty;

        public string WhatItIs { get; set; } = string.Empty;

        public string WhyItMatters { get; set; } = string.Empty;

        public string Risk { get; set; } = string.Empty;

        public List<string> SuggestedNextSteps { get; set; } = [];
    }
}
