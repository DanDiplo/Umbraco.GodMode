using System;
using System.Collections.Generic;

namespace Diplo.GodMode.Models
{
    public class DiagnosticGroup
    {
        public DiagnosticGroup()
        {
            this.Sections = new List<DiagnosticSection>();
        }

        public DiagnosticGroup(int id, string title) : this()
        {
            this.Id = id;
            this.Title = title;
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public List<DiagnosticSection> Sections { get; set; }

        public void Add(List<DiagnosticSection> sections)
        {
            this.Sections.AddRange(sections);
        }
    }
}