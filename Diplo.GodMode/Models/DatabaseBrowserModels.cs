using System.Collections.Generic;

namespace Diplo.GodMode.Models;

public class DatabaseTableInfo
{
    public string Name { get; set; } = string.Empty;

    public string Schema { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Purpose { get; set; } = string.Empty;

    public long RowCount { get; set; }

    public bool CountSucceeded { get; set; } = true;

    public string Warning { get; set; } = string.Empty;
}

public class DatabaseTableDetail : DatabaseTableInfo
{
    public IEnumerable<DatabaseColumnInfo> Columns { get; set; } = [];

    public IEnumerable<DatabaseRelationshipInfo> OutgoingRelationships { get; set; } = [];

    public IEnumerable<DatabaseRelationshipInfo> IncomingRelationships { get; set; } = [];
}

public class DatabaseColumnInfo
{
    public string Name { get; set; } = string.Empty;

    public string DataType { get; set; } = string.Empty;

    public int? MaxLength { get; set; }

    public bool Nullable { get; set; }

    public bool PrimaryKey { get; set; }

    public int Ordinal { get; set; }
}

public class DatabaseRelationshipInfo
{
    public string ConstraintName { get; set; } = string.Empty;

    public string FromTable { get; set; } = string.Empty;

    public string FromColumn { get; set; } = string.Empty;

    public string ToTable { get; set; } = string.Empty;

    public string ToColumn { get; set; } = string.Empty;
}
