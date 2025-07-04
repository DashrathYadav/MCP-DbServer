using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using Microsoft.Extensions.Logging;
using MsDbServer.Application.Services;

namespace MsDbServer.Presentation.Tools;

/// <summary>
/// Modern database introspection tools for MCP using latest SDK features and clean architecture
/// </summary>
[McpServerToolType]
public static class DatabaseTools
{
    /// <summary>
    /// Get detailed description of a table including column names and types
    /// </summary>
    [McpServerTool(Name = "describe_table", Title = "Describe Table Structure")]
    [Description("Get detailed description of a table including column names, types, constraints, and relationships. This is a read-only operation that analyzes database structure.")]
    public static async Task<string> DescribeTable(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        [Description("Name of the table to describe")] string tableName,
        [Description("Schema name (optional, defaults to 'default')")] string? schemaName = null)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be empty", nameof(tableName));
        }

        logger.LogInformation("Describing table: {Schema}.{Table}", schemaName ?? "default", tableName);
        
        var tableInfo = await databaseService.GetTableInfoAsync(tableName, schemaName);
        if (tableInfo == null)
        {
            throw new InvalidOperationException($"Table '{tableName}' not found");
        }

        return FormatTableDescription(tableInfo);
    }

    /// <summary>
    /// List all tables in the database
    /// </summary>
    [McpServerTool(Name = "list_tables", Title = "List Database Tables")]
    [Description("List all tables in the database or a specific schema. Shows table names, schemas, and row counts.")]
    public static async Task<string> ListTables(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        [Description("Schema name (optional, lists all if not specified)")] string? schemaName = null)
    {
        logger.LogInformation("Listing tables for schema: {Schema}", schemaName ?? "all");
        
        var tables = await databaseService.GetTablesAsync(schemaName);
        return FormatTableList(tables);
    }

    /// <summary>
    /// Execute a SQL query and return the results
    /// </summary>
    [McpServerTool(Name = "execute_query", Title = "Execute SQL Query")]
    [Description("Execute a SQL query and return the results. Use this for SELECT statements and other queries that return data.")]
    public static async Task<string> ExecuteQuery(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        [Description("SQL query to execute")] string query,
        [Description("Maximum number of rows to return (default: 100)")] int maxRows = 100)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty", nameof(query));
        }

        logger.LogInformation("Executing query with max rows: {MaxRows}", maxRows);
        
        var result = await databaseService.ExecuteQueryAsync(query);
        
        if (!result.Success)
        {
            throw new InvalidOperationException($"Query failed: {result.ErrorMessage}");
        }

        return FormatQueryResult(result, maxRows);
    }

    /// <summary>
    /// List all schemas in the database
    /// </summary>
    [McpServerTool(Name = "list_schemas", Title = "List Database Schemas")]
    [Description("List all schemas (databases) available in the database server.")]
    public static async Task<string> ListSchemas(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger)
    {
        logger.LogInformation("Listing database schemas");
        
        var schemas = await databaseService.GetSchemasAsync();
        return FormatSchemaList(schemas);
    }

    /// <summary>
    /// Export table data to CSV format
    /// </summary>
    [McpServerTool(Name = "export_csv", Title = "Export Table to CSV")]
    [Description("Export table data to CSV format. Useful for data analysis or backup purposes.")]
    public static async Task<string> ExportCsv(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        [Description("Name of the table to export")] string tableName,
        [Description("Schema name (optional)")] string? schemaName = null,
        [Description("Maximum number of rows to export (default: 1000)")] int limit = 1000)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new ArgumentException("Table name cannot be empty", nameof(tableName));
        }

        logger.LogInformation("Exporting table to CSV: {Schema}.{Table} (Limit: {Limit})", 
            schemaName ?? "default", tableName, limit);
        
        return await databaseService.ExportToCsvAsync(tableName, schemaName, limit);
    }

    /// <summary>
    /// Test database connection
    /// </summary>
    [McpServerTool(Name = "test_connection", Title = "Test Database Connection")]
    [Description("Test the database connection to ensure it's working properly.")]
    public static async Task<string> TestConnection(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger)
    {
        logger.LogInformation("Testing database connection");
        
        var isConnected = await databaseService.TestConnectionAsync();
        return isConnected ? "Database connection successful" : "Database connection failed";
    }

    // Helper methods for formatting results
    private static string FormatTableDescription(MsDbServer.Domain.Models.TableInfo tableInfo)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Table: {tableInfo.Schema}.{tableInfo.Name}");
        sb.AppendLine($"Row Count: {tableInfo.RowCount:N0}");
        sb.AppendLine();
        
        if (tableInfo.Columns.Any())
        {
            sb.AppendLine("Columns:");
            sb.AppendLine("--------");
            foreach (var column in tableInfo.Columns)
            {
                var nullable = column.IsNullable ? "NULL" : "NOT NULL";
                var primaryKey = column.IsPrimaryKey ? " (PK)" : "";
                sb.AppendLine($"  {column.Name}: {column.DataType} {nullable}{primaryKey}");
            }
        }

        if (tableInfo.ForeignKeys.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Foreign Keys:");
            sb.AppendLine("-------------");
            foreach (var fk in tableInfo.ForeignKeys)
            {
                sb.AppendLine($"  {fk.Column} → {fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}");
            }
        }

        return sb.ToString();
    }

    private static string FormatTableList(IEnumerable<MsDbServer.Domain.Models.TableInfo> tables)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Database Tables:");
        sb.AppendLine("================");
        
        foreach (var table in tables.OrderBy(t => t.Schema).ThenBy(t => t.Name))
        {
            sb.AppendLine($"  {table.Schema}.{table.Name} ({table.RowCount:N0} rows)");
        }

        return sb.ToString();
    }

    private static string FormatSchemaList(IEnumerable<MsDbServer.Domain.Models.SchemaInfo> schemas)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Database Schemas:");
        sb.AppendLine("=================");
        
        foreach (var schema in schemas.OrderBy(s => s.Name))
        {
            sb.AppendLine($"  {schema.Name} ({schema.Type})");
            if (!string.IsNullOrEmpty(schema.Description))
            {
                sb.AppendLine($"    Description: {schema.Description}");
            }
        }

        return sb.ToString();
    }

    private static string FormatQueryResult(MsDbServer.Domain.Models.QueryResult result, int maxRows)
    {
        var sb = new StringBuilder();
        
        if (result.ColumnNames == null || result.Rows == null)
        {
            return "No data returned";
        }

        sb.AppendLine($"Query Results ({result.RowCount:N0} rows, {result.ExecutionTime.TotalMilliseconds:F2}ms):");
        sb.AppendLine(new string('=', 50));
        
        // Headers
        sb.AppendLine(string.Join(" | ", result.ColumnNames));
        sb.AppendLine(new string('-', result.ColumnNames.Sum(c => c.Length) + (result.ColumnNames.Length - 1) * 3));
        
        // Data (limited to maxRows)
        var rowsToShow = Math.Min(result.Rows.Length, maxRows);
        for (int i = 0; i < rowsToShow; i++)
        {
            var row = result.Rows[i];
            sb.AppendLine(string.Join(" | ", row.Select(cell => cell?.ToString() ?? "NULL")));
        }

        if (result.Rows.Length > maxRows)
        {
            sb.AppendLine($"... ({result.Rows.Length - maxRows} more rows)");
        }

        return sb.ToString();
    }
}
