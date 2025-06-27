using System.ComponentModel;

namespace MsDbServer;

/// <summary>
/// Interface for database services that provide database introspection capabilities
/// </summary>
public interface IDatabaseService
{
    /// <summary>
    /// Get detailed description of a table including columns, constraints, and relationships
    /// </summary>
    /// <param name="tableName">Name of the table to describe</param>
    /// <param name="schemaName">Optional schema name</param>
    /// <returns>Detailed table description</returns>
    Task<TableDescription> DescribeTableAsync(string tableName, string? schemaName = null);

    /// <summary>
    /// Get a list of all tables in the database
    /// </summary>
    /// <returns>List of table names</returns>
    Task<List<string>> GetTablesAsync();

    /// <summary>
    /// Test database connection
    /// </summary>
    /// <returns>True if connection is successful</returns>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Execute a SELECT query and return results
    /// </summary>
    /// <param name="query">SQL SELECT query to execute</param>
    /// <param name="maxRows">Maximum number of rows to return</param>
    /// <returns>Query results</returns>
    Task<QueryResults> ExecuteQueryAsync(string query, int maxRows = 100);

    /// <summary>
    /// Get database statistics and information
    /// </summary>
    /// <returns>Database statistics</returns>
    Task<DatabaseStats> GetDatabaseStatsAsync();

    /// <summary>
    /// Get schema information for tables matching a pattern
    /// </summary>
    /// <param name="tablePattern">Optional table name pattern with % wildcards</param>
    /// <returns>Schema information</returns>
    Task<List<TableDescription>> GetSchemaInfoAsync(string? tablePattern = null);

    /// <summary>
    /// Get table relationships and foreign key dependencies
    /// </summary>
    /// <param name="tableName">Optional specific table name</param>
    /// <returns>Table relationships</returns>
    Task<List<TableRelationship>> GetTableRelationshipsAsync(string? tableName = null);
}

/// <summary>
/// Represents detailed information about a database table
/// </summary>
public class TableDescription
{
    public string TableName { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public List<ColumnInfo> Columns { get; set; } = new();
    public List<string> PrimaryKeys { get; set; } = new();
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();
    public List<IndexInfo> Indexes { get; set; } = new();
}

/// <summary>
/// Represents information about a database column
/// </summary>
public class ColumnInfo
{
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public string? DefaultValue { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
}

/// <summary>
/// Represents information about a foreign key relationship
/// </summary>
public class ForeignKeyInfo
{
    public string ForeignKeyName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string ReferencedTable { get; set; } = string.Empty;
    public string ReferencedColumn { get; set; } = string.Empty;
    public string ReferencedSchema { get; set; } = string.Empty;
}

/// <summary>
/// Represents information about a database index
/// </summary>
public class IndexInfo
{
    public string IndexName { get; set; } = string.Empty;
    public bool IsUnique { get; set; }
    public List<string> Columns { get; set; } = new();
}

/// <summary>
/// Represents the results of a database query
/// </summary>
public class QueryResults
{
    public List<string> ColumnNames { get; set; } = new();
    public List<List<object?>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public bool HasMoreRows { get; set; }
    public string Query { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Represents database statistics and information
/// </summary>
public class DatabaseStats
{
    public string DatabaseName { get; set; } = string.Empty;
    public string DatabaseVersion { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public long DatabaseSizeBytes { get; set; }
    public List<TableStats> TableStats { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Represents statistics for a specific table
/// </summary>
public class TableStats
{
    public string TableName { get; set; } = string.Empty;
    public long RowCount { get; set; }
    public long DataSizeBytes { get; set; }
    public long IndexSizeBytes { get; set; }
    public int ColumnCount { get; set; }
    public int IndexCount { get; set; }
}

/// <summary>
/// Represents table relationship information
/// </summary>
public class TableRelationship
{
    public string ParentTable { get; set; } = string.Empty;
    public string ParentColumn { get; set; } = string.Empty;
    public string ChildTable { get; set; } = string.Empty;
    public string ChildColumn { get; set; } = string.Empty;
    public string ConstraintName { get; set; } = string.Empty;
    public string ParentSchema { get; set; } = string.Empty;
    public string ChildSchema { get; set; } = string.Empty;
}
