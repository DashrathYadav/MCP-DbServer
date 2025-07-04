namespace MsDbServer.Domain.Models;

/// <summary>
/// Represents a database connection configuration
/// </summary>
public class DatabaseConnection
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public int MaxPoolSize { get; init; } = 20;
    public int CommandTimeout { get; init; } = 30;
    public bool EnableRetryOnFailure { get; init; } = true;
    public int MaxRetryCount { get; init; } = 3;
}

/// <summary>
/// Represents the result of a database query
/// </summary>
public class QueryResult
{
    public bool Success { get; init; }
    public string[]? ColumnNames { get; init; }
    public object[][]? Rows { get; init; }
    public int RowCount { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
}

/// <summary>
/// Represents database schema information
/// </summary>
public class SchemaInfo
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
}

/// <summary>
/// Represents a database table structure
/// </summary>
public class TableInfo
{
    public string Name { get; init; } = string.Empty;
    public string Schema { get; init; } = string.Empty;
    public List<ColumnInfo> Columns { get; init; } = new();
    public List<string> PrimaryKeys { get; init; } = new();
    public List<ForeignKeyInfo> ForeignKeys { get; init; } = new();
    public long RowCount { get; init; }
}

/// <summary>
/// Represents a database column structure
/// </summary>
public class ColumnInfo
{
    public string Name { get; init; } = string.Empty;
    public string DataType { get; init; } = string.Empty;
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public string? DefaultValue { get; init; }
    public int? MaxLength { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
}

/// <summary>
/// Represents a foreign key relationship
/// </summary>
public class ForeignKeyInfo
{
    public string Name { get; init; } = string.Empty;
    public string Column { get; init; } = string.Empty;
    public string ReferencedTable { get; init; } = string.Empty;
    public string ReferencedColumn { get; init; } = string.Empty;
    public string ReferencedSchema { get; init; } = string.Empty;
}
