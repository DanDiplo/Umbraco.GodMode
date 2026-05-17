namespace Diplo.GodMode.Models
{
    public class ViewFormMap
    {
        public int TemplateId { get; set; }

        public string TemplateAlias { get; set; }

        public string Kind { get; set; }

        public string Method { get; set; }

        public string Action { get; set; }

        public string Controller { get; set; }

        public bool HasAntiForgeryToken { get; set; }
    }
}
