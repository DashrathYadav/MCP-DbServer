using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using ModelContextProtocol; // Add this for McpException
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
        try
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new McpException("Table name cannot be empty", McpErrorCode.InvalidParams);
            }

            logger.LogInformation("Describing table: {Schema}.{Table}", schemaName ?? "default", tableName);

            var tableInfo = await databaseService.GetTableInfoAsync(tableName, schemaName);
            if (tableInfo == null)
            {
                throw new McpException($"Table '{tableName}' not found in schema '{schemaName ?? "default"}'", McpErrorCode.InvalidParams);
            }

            return FormatTableDescription(tableInfo);
        }
        catch (McpException)
        {
            throw; // Re-throw MCP exceptions as-is
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error describing table {Table}", tableName);
            throw new McpException($"Database error: {ex.Message}", McpErrorCode.InternalError);
        }
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
        try
        {
            logger.LogInformation("Listing tables for schema: {Schema}", schemaName ?? "all");

            var tables = await databaseService.GetTablesAsync(schemaName);
            return FormatTableList(tables);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing tables for schema {Schema}", schemaName);
            throw new McpException($"Database error: {ex.Message}", McpErrorCode.InternalError);
        }
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
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new McpException("Query cannot be empty", McpErrorCode.InvalidParams);
            }

            if (maxRows <= 0 || maxRows > 10000)
            {
                throw new McpException("Max rows must be between 1 and 10000", McpErrorCode.InvalidParams);
            }

            logger.LogInformation("Executing query with max rows: {MaxRows}", maxRows);

            var result = await databaseService.ExecuteQueryAsync(query);

            if (!result.Success)
            {
                throw new McpException($"Query failed: {result.ErrorMessage}", McpErrorCode.InvalidRequest);
            }

            return FormatQueryResult(result, maxRows);
        }
        catch (McpException)
        {
            throw; // Re-throw MCP exceptions as-is
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing query");
            throw new McpException($"Database error: {ex.Message}", McpErrorCode.InternalError);
        }
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
        try
        {
            logger.LogInformation("Listing database schemas");

            var schemas = await databaseService.GetSchemasAsync();
            return FormatSchemaList(schemas);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing schemas");
            throw new McpException($"Database error: {ex.Message}", McpErrorCode.InternalError);
        }
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

    /// <summary>
    /// Analyze database structure and generate comprehensive report with progress tracking
    /// </summary>
    [McpServerTool(Name = "analyze_database", Title = "Analyze Database Structure")]
    [Description("Perform comprehensive analysis of database structure including table relationships, data distribution, and schema overview. Shows progress during analysis.")]
    public static async Task<string> AnalyzeDatabaseWithProgress(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        IProgress<ProgressNotificationValue> progress,
        [Description("Include detailed statistics (default: false)")] bool includeStats = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Database Structure Analysis ===");
            sb.AppendLine($"Analysis started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();

            // Step 1: Get schemas
            progress?.Report(new ProgressNotificationValue
            {
                Progress = 10,
                Message = "Analyzing schemas..."
            });
            await Task.Delay(500, cancellationToken); // Simulate work

            var schemas = await databaseService.GetSchemasAsync();
            sb.AppendLine($"Found {schemas.Count()} schema(s):");
            foreach (var schema in schemas)
            {
                sb.AppendLine($"  - {schema.Name}");
            }
            sb.AppendLine();

            // Step 2: Get all tables
            progress?.Report(new ProgressNotificationValue
            {
                Progress = 30,
                Message = "Discovering tables..."
            });
            await Task.Delay(500, cancellationToken);

            var allTables = await databaseService.GetTablesAsync();
            sb.AppendLine($"Found {allTables.Count()} table(s) total:");

            var tablesBySchema = allTables.GroupBy(t => t.Schema).OrderBy(g => g.Key);
            foreach (var schemaGroup in tablesBySchema)
            {
                sb.AppendLine($"  {schemaGroup.Key}: {schemaGroup.Count()} tables");
            }
            sb.AppendLine();

            // Step 3: Analyze table relationships
            progress?.Report(new ProgressNotificationValue
            {
                Progress = 60,
                Message = "Analyzing relationships..."
            });
            await Task.Delay(500, cancellationToken);

            var tablesWithForeignKeys = allTables.Where(t => t.ForeignKeys.Any()).ToList();
            sb.AppendLine($"Tables with foreign key relationships: {tablesWithForeignKeys.Count}");

            if (tablesWithForeignKeys.Any())
            {
                sb.AppendLine("Key relationships:");
                foreach (var table in tablesWithForeignKeys.Take(10)) // Limit for readability
                {
                    foreach (var fk in table.ForeignKeys)
                    {
                        sb.AppendLine($"  {table.Schema}.{table.Name}.{fk.Column} → {fk.ReferencedSchema}.{fk.ReferencedTable}.{fk.ReferencedColumn}");
                    }
                }
                if (tablesWithForeignKeys.Count > 10)
                {
                    sb.AppendLine($"  ... and {tablesWithForeignKeys.Count - 10} more relationships");
                }
            }
            sb.AppendLine();

            // Step 4: Optional detailed statistics
            if (includeStats)
            {
                progress?.Report(new ProgressNotificationValue
                {
                    Progress = 80,
                    Message = "Calculating statistics..."
                });
                await Task.Delay(500, cancellationToken);

                sb.AppendLine("=== Table Statistics ===");
                var totalRows = allTables.Sum(t => t.RowCount);
                sb.AppendLine($"Total rows across all tables: {totalRows:N0}");

                var largestTables = allTables.OrderByDescending(t => t.RowCount).Take(5);
                sb.AppendLine("Largest tables:");
                foreach (var table in largestTables)
                {
                    sb.AppendLine($"  {table.Schema}.{table.Name}: {table.RowCount:N0} rows");
                }
                sb.AppendLine();
            }

            progress?.Report(new ProgressNotificationValue
            {
                Progress = 100,
                Message = "Analysis complete"
            });

            sb.AppendLine($"Analysis completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            logger.LogInformation("Database analysis completed with {TableCount} tables", allTables.Count());

            return sb.ToString();
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Database analysis was cancelled");
            throw new McpException("Database analysis was cancelled", McpErrorCode.InvalidRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database analysis");
            throw new McpException($"Analysis failed: {ex.Message}", McpErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Get query execution plan
    /// </summary>
    [McpServerTool(Name = "get_query_execution_plan", Title = "Get Query Execution Plan")]
    [Description("Get the execution plan for a SQL query to understand how the database will execute it. Provides cost estimates, row counts, and performance warnings.")]
    public static async Task<string> GetQueryExecutionPlan(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        [Description("SQL query to get the execution plan for")] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new McpException("Query cannot be empty", McpErrorCode.InvalidParams);
            }

            logger.LogInformation("Getting execution plan for query");

            var plan = await databaseService.GetExecutionPlanAsync(query);
            return FormatExecutionPlan(plan);
        }
        catch (McpException)
        {
            throw; // Re-throw MCP exceptions as-is
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting execution plan");
            throw new McpException($"Database error: {ex.Message}", McpErrorCode.InternalError);
        }
    }

    /// <summary>
    /// Analyze query performance with AI-powered optimization suggestions
    /// </summary>
    [McpServerTool(Name = "analyze_query_performance", Title = "Analyze Query Performance")]
    [Description("AI-powered analysis of SQL query performance with optimization suggestions, index recommendations, and query rewrite suggestions.")]
    public static async Task<string> AnalyzeQueryPerformance(
        DatabaseService databaseService,
        ILogger<DatabaseService> logger,
        [Description("SQL query to analyze for performance optimization")] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new McpException("Query cannot be empty", McpErrorCode.InvalidParams);
            }

            logger.LogInformation("Analyzing query performance with AI suggestions");

            var analysis = await databaseService.AnalyzeQueryPerformanceAsync(query);
            return FormatQueryAnalysis(analysis);
        }
        catch (McpException)
        {
            throw; // Re-throw MCP exceptions as-is
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error analyzing query performance");
            throw new McpException($"Database error: {ex.Message}", McpErrorCode.InternalError);
        }
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

    private static string FormatExecutionPlan(MsDbServer.Domain.Models.ExecutionPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🔍 Query Execution Plan Analysis");
        sb.AppendLine("=====================================");
        sb.AppendLine();

        sb.AppendLine("📊 Overall Statistics:");
        sb.AppendLine($"   Estimated Cost: {plan.EstimatedCost:F4}");
        sb.AppendLine($"   Estimated Rows: {plan.EstimatedRows:N0}");
        sb.AppendLine($"   Estimated Time: {plan.EstimatedExecutionTime.TotalMilliseconds:F2} ms");
        sb.AppendLine($"   Generated: {plan.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        if (plan.Warnings.Any())
        {
            sb.AppendLine("⚠️ Performance Warnings:");
            foreach (var warning in plan.Warnings)
            {
                sb.AppendLine($"   • {warning}");
            }
            sb.AppendLine();
        }

        if (plan.Steps.Any())
        {
            sb.AppendLine("📋 Execution Steps:");
            sb.AppendLine("-------------------");
            for (int i = 0; i < plan.Steps.Count; i++)
            {
                var step = plan.Steps[i];
                var expensiveIndicator = step.IsExpensive ? "🔥" : "  ";
                sb.AppendLine($"{expensiveIndicator} Step {i + 1}: {step.Operation}");
                sb.AppendLine($"     Cost: {step.Cost:F4} | Rows: {step.Rows:N0}");
                if (!string.IsNullOrEmpty(step.TableName))
                    sb.AppendLine($"     Table: {step.TableName}");
                if (!string.IsNullOrEmpty(step.IndexName))
                    sb.AppendLine($"     Index: {step.IndexName}");

                if (step.Suggestions.Any())
                {
                    sb.AppendLine("     💡 Suggestions:");
                    foreach (var suggestion in step.Suggestions)
                    {
                        sb.AppendLine($"        • {suggestion}");
                    }
                }
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrEmpty(plan.PlanText))
        {
            sb.AppendLine("📄 Detailed Plan:");
            sb.AppendLine("-----------------");
            sb.AppendLine(plan.PlanText);
        }

        return sb.ToString();
    }

    private static string FormatQueryAnalysis(MsDbServer.Domain.Models.QueryAnalysis analysis)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🤖 AI-Powered Query Performance Analysis");
        sb.AppendLine("========================================");
        sb.AppendLine();

        // Performance rating with emoji
        var ratingEmoji = analysis.PerformanceRating switch
        {
            "Excellent" => "🟢",
            "Good" => "🟡",
            "Fair" => "🟠",
            "Poor" => "🔴",
            _ => "⚪"
        };

        sb.AppendLine($"📈 Performance Rating: {ratingEmoji} {analysis.PerformanceRating}");
        if (analysis.PotentialImprovement > 0)
        {
            sb.AppendLine($"🚀 Potential Improvement: {analysis.PotentialImprovement:F1}%");
        }
        sb.AppendLine($"🕐 Analyzed: {analysis.AnalyzedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        if (analysis.Issues.Any())
        {
            sb.AppendLine("❌ Issues Identified:");
            foreach (var issue in analysis.Issues)
            {
                sb.AppendLine($"   • {issue}");
            }
            sb.AppendLine();
        }

        if (analysis.Recommendations.Any())
        {
            sb.AppendLine("💡 General Recommendations:");
            foreach (var recommendation in analysis.Recommendations)
            {
                sb.AppendLine($"   • {recommendation}");
            }
            sb.AppendLine();
        }

        if (analysis.IndexSuggestions.Any())
        {
            sb.AppendLine("🗂️ Index Suggestions:");
            foreach (var indexSuggestion in analysis.IndexSuggestions)
            {
                sb.AppendLine($"   • {indexSuggestion}");
            }
            sb.AppendLine();
        }

        if (analysis.RewriteSuggestions.Any())
        {
            sb.AppendLine("✏️ Query Rewrite Suggestions:");
            foreach (var rewriteSuggestion in analysis.RewriteSuggestions)
            {
                sb.AppendLine($"   • {rewriteSuggestion}");
            }
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(analysis.OptimizedQuery))
        {
            sb.AppendLine("🔧 Optimized Query Suggestion:");
            sb.AppendLine("------------------------------");
            sb.AppendLine(analysis.OptimizedQuery);
            sb.AppendLine();
        }

        // Include execution plan summary
        sb.AppendLine("📊 Execution Plan Summary:");
        sb.AppendLine($"   Cost: {analysis.ExecutionPlan.EstimatedCost:F4}");
        sb.AppendLine($"   Rows: {analysis.ExecutionPlan.EstimatedRows:N0}");
        sb.AppendLine($"   Steps: {analysis.ExecutionPlan.Steps.Count}");
        sb.AppendLine($"   Warnings: {analysis.ExecutionPlan.Warnings.Count}");

        return sb.ToString();
    }
}
