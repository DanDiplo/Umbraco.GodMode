namespace Diplo.GodMode.Models
{
    public class ViewAssetMap
    {
        public int TemplateId { get; set; }

        public string TemplateAlias { get; set; }

        public string Kind { get; set; }

        public string Url { get; set; }

        public string Host { get; set; }

        public bool IsExternal { get; set; }

        public bool IsInline { get; set; }

        public bool IsResolved { get; set; }

        public bool Exists { get; set; }

        public string ResolvedPath { get; set; }

        public string Warning { get; set; }

        public string Attributes { get; set; }
    }
}
