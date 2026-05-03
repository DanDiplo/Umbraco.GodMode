namespace Diplo.GodMode.Models
{
    public class DiagnosticGroup
    {
        public DiagnosticGroup(string title)
        {
            Title = title;
        }

        public DiagnosticGroup(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public int Id { get; private set; }

        public string Title { get; }

        public List<DiagnosticSection> Sections { get; } = [];

        public void SetId(int id) => Id = id;

        public DiagnosticGroup Add(params DiagnosticSection[] sections)
        {
            Sections.AddRange(sections);
            return this;
        }

        public DiagnosticGroup Add(IEnumerable<DiagnosticSection> sections)
        {
            Sections.AddRange(sections);
            return this;
        }

        public DiagnosticGroup AddIfNotNull(DiagnosticSection section)
        {
            if (section != null)
            {
                Sections.Add(section);
            }

            return this;
        }
    }
}