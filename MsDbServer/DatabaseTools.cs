using System.ComponentModel;
using ModelContextProtocol.Server;
using System.Text.Json;

namespace MsDbServer;

/// <summary>
/// Database introspection tools for MCP
/// </summary>
[McpServerToolType]
public static class DatabaseTools
{
    /// <summary>
    /// Get detailed description of a table including column names and types
    /// </summary>
    /// <param name="databaseService">Injected database service</param>
    /// <param name="tableName">Name of the table to describe</param>
    /// <param name="schemaName">Schema name (optional, defaults to 'default')</param>
    /// <returns>Formatted table description</returns>
    [McpServerTool, Description("Get detailed description of a table including column names and types.")]
    public static async Task<string> DescribeTable(
        IDatabaseService databaseService,
        [Description("Name of the table to describe")] string tableName,
        [Description("Schema name (optional, defaults to 'default')")] string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return "Error: Table name cannot be empty";
        }

        try
        {
            var tableDescription = await databaseService.DescribeTableAsync(tableName, schemaName);
            return FormatTableDescription(tableDescription);
        }
        catch (Exception ex)
        {
            return $"Error describing table '{tableName}': {ex.Message}";
        }
    }

    /// <summary>
    /// Get list of all tables in the database
    /// </summary>
    /// <param name="databaseService">Injected database service</param>
    /// <returns>List of table names</returns>
    [McpServerTool, Description("Get a list of all tables in the database.")]
    public static async Task<string> ListTables(IDatabaseService databaseService)
    {
        try
        {
            var tables = await databaseService.GetTablesAsync();
            if (!tables.Any())
            {
                return "No tables found in the database.";
            }

            var result = new System.Text.StringBuilder();
            result.AppendLine("Database Tables:");
            result.AppendLine("================");
            
            foreach (var table in tables.OrderBy(t => t))
            {
                result.AppendLine($"  {table}");
            }

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error listing tables: {ex.Message}";
        }
    }

    /// <summary>
    /// Execute a SELECT query on the database
    /// </summary>
    /// <param name="databaseService">Injected database service</param>
    /// <param name="query">SQL SELECT query to execute</param>
    /// <param name="maxRows">Maximum number of rows to return (default: 100)</param>
    /// <returns>Query results in formatted table</returns>
    [McpServerTool, Description("Execute a SELECT query on the database. Returns results in a formatted table.")]
    public static async Task<string> ExecuteQuery(
        IDatabaseService databaseService,
        [Description("SQL SELECT query to execute (SELECT statements only)")] string query,
        [Description("Maximum number of rows to return (default: 100, max: 1000)")] int maxRows = 100)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: Query cannot be empty";
        }

        // Basic security check - only allow SELECT statements
        var trimmedQuery = query.Trim();
        if (!trimmedQuery.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !trimmedQuery.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return "Error: Only SELECT and WITH statements are allowed";
        }

        // Limit max rows for safety
        maxRows = Math.Min(Math.Max(1, maxRows), 1000);

        try
        {
            var results = await databaseService.ExecuteQueryAsync(query, maxRows);
            return FormatQueryResults(results);
        }
        catch (Exception ex)
        {
            return $"Error executing query: {ex.Message}";
        }
    }

    /// <summary>
    /// Get database statistics and general information
    /// </summary>
    /// <param name="databaseService">Injected database service</param>
    /// <returns>Database statistics and information</returns>
    [McpServerTool, Description("Get database statistics including table counts, size information, and general database info.")]
    public static async Task<string> GetDatabaseStats(IDatabaseService databaseService)
    {
        try
        {
            var stats = await databaseService.GetDatabaseStatsAsync();
            return FormatDatabaseStats(stats);
        }
        catch (Exception ex)
        {
            return $"Error getting database statistics: {ex.Message}";
        }
    }

    /// <summary>
    /// Get schema information for all tables or a specific table pattern
    /// </summary>
    /// <param name="databaseService">Injected database service</param>
    /// <param name="tablePattern">Optional table name pattern (supports % wildcards)</param>
    /// <returns>Schema information for matching tables</returns>
    [McpServerTool, Description("Get schema information for all tables or tables matching a pattern. Use % as wildcard.")]
    public static async Task<string> GetSchemaInfo(
        IDatabaseService databaseService,
        [Description("Optional table name pattern (supports % wildcards, e.g., 'user%' or '%_log')")] string? tablePattern = null)
    {
        try
        {
            var schemaInfo = await databaseService.GetSchemaInfoAsync(tablePattern);
            return FormatSchemaInfo(schemaInfo);
        }
        catch (Exception ex)
        {
            return $"Error getting schema information: {ex.Message}";
        }
    }

    /// <summary>
    /// Get table relationships showing foreign key dependencies
    /// </summary>
    /// <param name="databaseService">Injected database service</param>
    /// <param name="tableName">Optional specific table to show relationships for</param>
    /// <returns>Table relationship information</returns>
    [McpServerTool, Description("Get table relationships showing foreign key dependencies between tables.")]
    public static async Task<string> GetTableRelationships(
        IDatabaseService databaseService,
        [Description("Optional specific table name to show relationships for")] string? tableName = null)
    {
        try
        {
            var relationships = await databaseService.GetTableRelationshipsAsync(tableName);
            return FormatTableRelationships(relationships);
        }
        catch (Exception ex)
        {
            return $"Error getting table relationships: {ex.Message}";
        }
    }

    private static string FormatTableDescription(TableDescription tableDescription)
    {
        var result = new System.Text.StringBuilder();

        result.AppendLine($"Table: {tableDescription.Schema}.{tableDescription.TableName}");
        result.AppendLine();

        // Columns
        result.AppendLine("Columns:");
        result.AppendLine("--------");

        foreach (var column in tableDescription.Columns)
        {
            var nullable = column.IsNullable ? "NULL" : "NOT NULL";
            var identity = column.IsIdentity ? " IDENTITY" : "";
            var primaryKey = column.IsPrimaryKey ? " PRIMARY KEY" : "";
            
            var dataType = column.DataType;
            if (column.MaxLength.HasValue && column.MaxLength > 0)
            {
                dataType += $"({column.MaxLength})";
            }
            else if (column.Precision.HasValue && column.Scale.HasValue)
            {
                dataType += $"({column.Precision},{column.Scale})";
            }
            else if (column.Precision.HasValue)
            {
                dataType += $"({column.Precision})";
            }

            result.AppendLine($"  {column.ColumnName} {dataType} {nullable}{identity}{primaryKey}");
            
            if (!string.IsNullOrEmpty(column.DefaultValue))
            {
                result.AppendLine($"    Default: {column.DefaultValue}");
            }
        }

        // Primary Keys
        if (tableDescription.PrimaryKeys.Any())
        {
            result.AppendLine();
            result.AppendLine("Primary Keys:");
            result.AppendLine("-------------");
            result.AppendLine($"  {string.Join(", ", tableDescription.PrimaryKeys)}");
        }

        // Foreign Keys
        if (tableDescription.ForeignKeys.Any())
        {
            result.AppendLine();
            result.AppendLine("Foreign Keys:");
            result.AppendLine("-------------");
            foreach (var fk in tableDescription.ForeignKeys)
            {
                result.AppendLine($"  {fk.ForeignKeyName}: {fk.ColumnName} -> {fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}");
            }
        }

        // Indexes
        if (tableDescription.Indexes.Any())
        {
            result.AppendLine();
            result.AppendLine("Indexes:");
            result.AppendLine("--------");
            foreach (var index in tableDescription.Indexes)
            {
                var unique = index.IsUnique ? "UNIQUE " : "";
                result.AppendLine($"  {unique}{index.IndexName}: {string.Join(", ", index.Columns)}");
            }
        }

        return result.ToString();
    }

    private static string FormatQueryResults(QueryResults results)
    {
        var result = new System.Text.StringBuilder();

        result.AppendLine($"Query: {results.Query}");
        result.AppendLine($"Execution Time: {results.ExecutionTime.TotalMilliseconds:F2}ms");
        result.AppendLine($"Rows: {results.TotalRows}{(results.HasMoreRows ? " (more available)" : "")}");
        result.AppendLine();

        if (!results.Rows.Any())
        {
            result.AppendLine("No data returned.");
            return result.ToString();
        }

        // Calculate column widths
        var columnWidths = new List<int>();
        for (int i = 0; i < results.ColumnNames.Count; i++)
        {
            var maxWidth = results.ColumnNames[i].Length;
            foreach (var row in results.Rows)
            {
                var cellValue = row[i]?.ToString() ?? "NULL";
                maxWidth = Math.Max(maxWidth, cellValue.Length);
            }
            columnWidths.Add(Math.Min(maxWidth, 50)); // Limit column width to 50 chars
        }

        // Header
        var separator = new string('-', columnWidths.Sum() + (columnWidths.Count - 1) * 3 + 4);
        result.AppendLine(separator);
        
        for (int i = 0; i < results.ColumnNames.Count; i++)
        {
            if (i > 0) result.Append(" | ");
            result.Append(results.ColumnNames[i].PadRight(columnWidths[i]));
        }
        result.AppendLine();
        result.AppendLine(separator);

        // Data rows
        foreach (var row in results.Rows)
        {
            for (int i = 0; i < row.Count; i++)
            {
                if (i > 0) result.Append(" | ");
                var cellValue = row[i]?.ToString() ?? "NULL";
                if (cellValue.Length > 50) cellValue = cellValue.Substring(0, 47) + "...";
                result.Append(cellValue.PadRight(columnWidths[i]));
            }
            result.AppendLine();
        }

        result.AppendLine(separator);
        return result.ToString();
    }

    private static string FormatDatabaseStats(DatabaseStats stats)
    {
        var result = new System.Text.StringBuilder();

        result.AppendLine($"Database Statistics: {stats.DatabaseName}");
        result.AppendLine("================================");
        result.AppendLine($"Database Version: {stats.DatabaseVersion}");
        result.AppendLine($"Generated At: {stats.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        result.AppendLine();

        result.AppendLine("Overview:");
        result.AppendLine("---------");
        result.AppendLine($"Total Tables: {stats.TableCount}");
        result.AppendLine($"Database Size: {FormatBytes(stats.DatabaseSizeBytes)}");
        result.AppendLine();

        if (stats.TableStats.Any())
        {
            result.AppendLine("Table Statistics:");
            result.AppendLine("-----------------");
            result.AppendLine("Table Name".PadRight(25) + "Rows".PadLeft(12) + "Data Size".PadLeft(12) + "Index Size".PadLeft(12) + "Columns".PadLeft(10) + "Indexes".PadLeft(10));
            result.AppendLine(new string('-', 25 + 12 + 12 + 12 + 10 + 10));

            foreach (var table in stats.TableStats.OrderByDescending(t => t.DataSizeBytes))
            {
                var tableName = table.TableName.Length > 24 ? table.TableName.Substring(0, 21) + "..." : table.TableName;
                result.AppendLine($"{tableName.PadRight(25)}{table.RowCount.ToString("N0").PadLeft(12)}{FormatBytes(table.DataSizeBytes).PadLeft(12)}{FormatBytes(table.IndexSizeBytes).PadLeft(12)}{table.ColumnCount.ToString().PadLeft(10)}{table.IndexCount.ToString().PadLeft(10)}");
            }
        }

        return result.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1}{suffixes[counter]}";
    }

    private static string FormatSchemaInfo(List<TableDescription> schemaInfo)
    {
        var result = new System.Text.StringBuilder();

        result.AppendLine($"Schema Information ({schemaInfo.Count} tables)");
        result.AppendLine("==================================");
        result.AppendLine();

        foreach (var table in schemaInfo)
        {
            result.AppendLine($"Table: {table.Schema}.{table.TableName}");
            result.AppendLine($"Columns: {table.Columns.Count}, Primary Keys: {table.PrimaryKeys.Count}, Foreign Keys: {table.ForeignKeys.Count}, Indexes: {table.Indexes.Count}");
            
            // Show just column names and types for overview
            result.AppendLine("Columns:");
            foreach (var column in table.Columns)
            {
                var keyInfo = "";
                if (column.IsPrimaryKey) keyInfo += " PK";
                if (column.IsIdentity) keyInfo += " AI";
                if (!column.IsNullable) keyInfo += " NOT NULL";
                
                result.AppendLine($"  {column.ColumnName} {column.DataType}{keyInfo}");
            }
            result.AppendLine();
        }

        return result.ToString();
    }

    private static string FormatTableRelationships(List<TableRelationship> relationships)
    {
        var result = new System.Text.StringBuilder();

        result.AppendLine($"Table Relationships ({relationships.Count} found)");
        result.AppendLine("=========================");
        result.AppendLine();

        if (!relationships.Any())
        {
            result.AppendLine("No foreign key relationships found.");
            return result.ToString();
        }

        // Group by parent table
        var groupedByParent = relationships.GroupBy(r => r.ParentTable).OrderBy(g => g.Key);
        
        foreach (var parentGroup in groupedByParent)
        {
            result.AppendLine($"Parent Table: {parentGroup.Key}");
            result.AppendLine("Children:");
            
            foreach (var rel in parentGroup.OrderBy(r => r.ChildTable))
            {
                result.AppendLine($"  {rel.ChildTable}.{rel.ChildColumn} -> {rel.ParentTable}.{rel.ParentColumn}");
                result.AppendLine($"    Constraint: {rel.ConstraintName}");
            }
            result.AppendLine();
        }

        // Also show reverse view - children pointing to parents
        result.AppendLine("Dependency Summary:");
        result.AppendLine("------------------");
        var groupedByChild = relationships.GroupBy(r => r.ChildTable).OrderBy(g => g.Key);
        
        foreach (var childGroup in groupedByChild)
        {
            var dependencies = string.Join(", ", childGroup.Select(r => r.ParentTable).Distinct());
            result.AppendLine($"{childGroup.Key} depends on: {dependencies}");
        }

        return result.ToString();
    }
}
