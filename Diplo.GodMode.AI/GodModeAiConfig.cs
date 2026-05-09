namespace Diplo.GodMode.AI
{
    public class GodModeAiConfig
    {
        public const string ConfigSectionName = "Diplo:GodMode:AI";

        public string ChatProfileAlias { get; set; }

        public int? MaxOutputTokens { get; set; }

        public float? Temperature { get; set; }
    }
}
