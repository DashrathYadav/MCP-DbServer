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

/// <summary>
/// Represents a query execution plan
/// </summary>
public class ExecutionPlan
{
    public string Query { get; init; } = string.Empty;
    public string PlanText { get; init; } = string.Empty;
    public string PlanXml { get; init; } = string.Empty;
    public double EstimatedCost { get; init; }
    public double EstimatedRows { get; init; }
    public TimeSpan EstimatedExecutionTime { get; init; }
    public List<ExecutionPlanStep> Steps { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a step in the execution plan
/// </summary>
public class ExecutionPlanStep
{
    public string Operation { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public double Cost { get; init; }
    public double Rows { get; init; }
    public string? TableName { get; init; }
    public string? IndexName { get; init; }
    public bool IsExpensive { get; init; }
    public List<string> Suggestions { get; init; } = new();
}

/// <summary>
/// Represents AI-powered query performance analysis
/// </summary>
public class QueryAnalysis
{
    public string OriginalQuery { get; init; } = string.Empty;
    public ExecutionPlan ExecutionPlan { get; init; } = new();
    public string PerformanceRating { get; init; } = string.Empty; // Excellent, Good, Fair, Poor
    public List<string> Issues { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public string? OptimizedQuery { get; init; }
    public List<string> IndexSuggestions { get; init; } = new();
    public List<string> RewriteSuggestions { get; init; } = new();
    public double PotentialImprovement { get; init; } // Percentage
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
}
