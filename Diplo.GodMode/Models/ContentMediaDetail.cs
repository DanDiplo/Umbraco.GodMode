using System;
using System.Collections.Generic;

namespace Diplo.GodMode.Models
{
    public class ContentMediaDetail
    {
        public string Kind { get; set; } = string.Empty;

        public int Id { get; set; }

        public Guid Key { get; set; }

        public string Name { get; set; } = string.Empty;

        public string ContentTypeName { get; set; } = string.Empty;

        public string ContentTypeAlias { get; set; } = string.Empty;

        public string Path { get; set; } = string.Empty;

        public int ParentId { get; set; }

        public int Level { get; set; }

        public bool Trashed { get; set; }

        public DateTime CreateDate { get; set; }

        public DateTime UpdateDate { get; set; }

        public ContentStateDetail State { get; set; } = new();

        public MediaFileDetail? MediaFile { get; set; }

        public IEnumerable<PropertyValueSummary> Properties { get; set; } = [];

        public IEnumerable<RelationSummary> IncomingRelations { get; set; } = [];

        public IEnumerable<RelationSummary> OutgoingRelations { get; set; } = [];

        public IEnumerable<ReferenceEdge> UsedBy { get; set; } = [];

        public IEnumerable<ReferenceEdge> Uses { get; set; } = [];

        public IEnumerable<AuditSummary> AuditTrail { get; set; } = [];
    }

    public class ContentStateDetail
    {
        public bool? Published { get; set; }

        public bool? Edited { get; set; }

        public int? TemplateId { get; set; }

        public int? PublishedVersionId { get; set; }

        public DateTime? PublishDate { get; set; }

        public IEnumerable<string> AvailableCultures { get; set; } = [];

        public IEnumerable<string> PublishedCultures { get; set; } = [];

        public IEnumerable<string> EditedCultures { get; set; } = [];
    }

    public class MediaFileDetail
    {
        public string Extension { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;

        public int Size { get; set; }
    }

    public class PropertyValueSummary
    {
        public string Alias { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string EditorAlias { get; set; } = string.Empty;

        public string StorageType { get; set; } = string.Empty;

        public bool Mandatory { get; set; }

        public string Variations { get; set; } = string.Empty;

        public int ValueCount { get; set; }

        public bool HasEditedValue { get; set; }

        public bool HasPublishedValue { get; set; }

        public IEnumerable<string> Cultures { get; set; } = [];
    }

    public class RelationSummary
    {
        public string Direction { get; set; } = string.Empty;

        public string RelationTypeAlias { get; set; } = string.Empty;

        public string RelationTypeName { get; set; } = string.Empty;

        public int RelatedId { get; set; }

        public Guid RelatedKey { get; set; }

        public string RelatedName { get; set; } = string.Empty;

        public string RelatedPath { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;
    }

    public class AuditSummary
    {
        public string AuditType { get; set; } = string.Empty;

        public string EntityType { get; set; } = string.Empty;

        public int UserId { get; set; }

        public string Comment { get; set; } = string.Empty;

        public string Parameters { get; set; } = string.Empty;
    }
}
