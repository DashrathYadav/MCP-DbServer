using MsDbServer.Domain.Interfaces;
using MsDbServer.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Diagnostics;

namespace MsDbServer.Infrastructure.Repositories;

/// <summary>
/// SQL Server implementation of the database repository
/// </summary>
public class SqlServerDatabaseRepository : IDatabaseRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerDatabaseRepository> _logger;

    public SqlServerDatabaseRepository(string connectionString, ILogger<SqlServerDatabaseRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection.State == ConnectionState.Open;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test SQL Server connection");
            return false;
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            var rows = new List<object[]>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new object[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                }
                rows.Add(row);
            }

            stopwatch.Stop();

            return new QueryResult
            {
                Success = true,
                ColumnNames = columns.ToArray(),
                Rows = rows.ToArray(),
                RowCount = rows.Count,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute SQL Server query: {Query}", query);

            return new QueryResult
            {
                Success = false,
                ColumnNames = null,
                Rows = null,
                RowCount = 0,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string command, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var sqlCommand = new SqlCommand(command, connection);
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute SQL Server non-query: {Command}", command);
            throw;
        }
    }

    public async Task<IEnumerable<SchemaInfo>> GetSchemasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    name as SchemaName,
                    'SQL Server Database: ' + name as Description
                FROM sys.databases 
                WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')
                ORDER BY name";

            var result = await ExecuteQueryAsync(query, cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to list schemas: {result.ErrorMessage}");
            }

            var schemas = new List<SchemaInfo>();
            if (result.Rows != null)
            {
                foreach (var row in result.Rows)
                {
                    schemas.Add(new SchemaInfo
                    {
                        Name = row[0]?.ToString() ?? "",
                        Type = "DATABASE",
                        Description = row[1]?.ToString() ?? ""
                    });
                }
            }

            return schemas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list SQL Server schemas");
            throw;
        }
    }

    public async Task<IEnumerable<TableInfo>> GetTablesAsync(string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = @"
                SELECT 
                    TABLE_SCHEMA as SchemaName,
                    TABLE_NAME as TableName
                FROM INFORMATION_SCHEMA.TABLES 
                WHERE TABLE_TYPE = 'BASE TABLE'";

            if (!string.IsNullOrEmpty(schema))
            {
                query += $" AND TABLE_SCHEMA = '{schema.Replace("'", "''")}'";
            }

            query += " ORDER BY TABLE_SCHEMA, TABLE_NAME";

            var result = await ExecuteQueryAsync(query, cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to list tables: {result.ErrorMessage}");
            }

            var tables = new List<TableInfo>();

            if (result.Rows != null)
            {
                foreach (var row in result.Rows)
                {
                    var tableName = row[1]?.ToString() ?? "";
                    var schemaName = row[0]?.ToString() ?? "";
                    var rowCount = await GetTableRowCountAsync(schemaName, tableName, cancellationToken);

                    tables.Add(new TableInfo
                    {
                        Schema = schemaName,
                        Name = tableName,
                        RowCount = rowCount
                    });
                }
            }

            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list SQL Server tables");
            throw;
        }
    }

    private async Task<long> GetTableRowCountAsync(string schema, string tableName, CancellationToken cancellationToken)
    {
        try
        {
            var countQuery = $"SELECT COUNT(*) as [RowCount] FROM [{schema}].[{tableName}]";
            var result = await ExecuteQueryAsync(countQuery, cancellationToken);

            if (result.Success && result.Rows != null && result.Rows.Length > 0)
            {
                var countValue = result.Rows[0][0];
                return Convert.ToInt64(countValue);
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get row count for table {Schema}.{TableName}", schema, tableName);
            return 0;
        }
    }

    public async Task<TableInfo?> GetTableInfoAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveSchema = schema ?? "dbo";
            var columns = await GetColumnsAsync(tableName, effectiveSchema, cancellationToken);
            var foreignKeys = await GetForeignKeysAsync(tableName, effectiveSchema, cancellationToken);
            var rowCount = await GetTableRowCountAsync(effectiveSchema, tableName, cancellationToken);

            return new TableInfo
            {
                Name = tableName,
                Schema = effectiveSchema,
                Columns = columns.ToList(),
                ForeignKeys = foreignKeys.ToList(),
                RowCount = rowCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SQL Server table info for {TableName}", tableName);
            return null;
        }
    }

    public async Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveSchema = schema ?? "dbo";

            var query = @"
                SELECT 
                    c.COLUMN_NAME as ColumnName,
                    c.DATA_TYPE as DataType,
                    c.IS_NULLABLE as IsNullable,
                    c.COLUMN_DEFAULT as DefaultValue,
                    CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END as IsPrimaryKey,
                    c.CHARACTER_MAXIMUM_LENGTH as MaxLength,
                    c.NUMERIC_PRECISION as NumericPrecision,
                    c.NUMERIC_SCALE as NumericScale
                FROM INFORMATION_SCHEMA.COLUMNS c
                LEFT JOIN (
                    SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                    INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku
                        ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY' 
                        AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
                        AND tc.TABLE_SCHEMA = ku.TABLE_SCHEMA
                        AND tc.TABLE_NAME = ku.TABLE_NAME
                ) pk ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA 
                    AND c.TABLE_NAME = pk.TABLE_NAME 
                    AND c.COLUMN_NAME = pk.COLUMN_NAME
                WHERE c.TABLE_NAME = @TableName AND c.TABLE_SCHEMA = @SchemaName
                ORDER BY c.ORDINAL_POSITION";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@SchemaName", effectiveSchema);

            var columns = new List<ColumnInfo>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(new ColumnInfo
                {
                    Name = reader.GetString("ColumnName"),
                    DataType = reader.GetString("DataType"),
                    IsNullable = reader.GetString("IsNullable") == "YES",
                    DefaultValue = reader.IsDBNull("DefaultValue") ? null : reader.GetString("DefaultValue"),
                    IsPrimaryKey = reader.GetInt32("IsPrimaryKey") == 1,
                    MaxLength = reader.IsDBNull("MaxLength") ? null : reader.GetInt32("MaxLength"),
                    Precision = reader.IsDBNull("NumericPrecision") ? null : reader.GetByte("NumericPrecision"),
                    Scale = reader.IsDBNull("NumericScale") ? null : reader.GetInt32("NumericScale")
                });
            }

            return columns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SQL Server columns for table {TableName}", tableName);
            throw;
        }
    }

    public async Task<IEnumerable<ForeignKeyInfo>> GetForeignKeysAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveSchema = schema ?? "dbo";

            var query = @"
                SELECT 
                    fk.name as ForeignKeyName,
                    c1.name as ColumnName,
                    t2.name as ReferencedTable,
                    c2.name as ReferencedColumn,
                    s2.name as ReferencedSchema
                FROM sys.foreign_keys fk
                INNER JOIN sys.tables t1 ON fk.parent_object_id = t1.object_id
                INNER JOIN sys.schemas s1 ON t1.schema_id = s1.schema_id
                INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                INNER JOIN sys.columns c1 ON fkc.parent_object_id = c1.object_id AND fkc.parent_column_id = c1.column_id
                INNER JOIN sys.tables t2 ON fkc.referenced_object_id = t2.object_id
                INNER JOIN sys.schemas s2 ON t2.schema_id = s2.schema_id
                INNER JOIN sys.columns c2 ON fkc.referenced_object_id = c2.object_id AND fkc.referenced_column_id = c2.column_id
                WHERE t1.name = @TableName AND s1.name = @SchemaName";

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@TableName", tableName);
            command.Parameters.AddWithValue("@SchemaName", effectiveSchema);

            var foreignKeys = new List<ForeignKeyInfo>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                foreignKeys.Add(new ForeignKeyInfo
                {
                    Name = reader.GetString("ForeignKeyName"),
                    Column = reader.GetString("ColumnName"),
                    ReferencedTable = reader.GetString("ReferencedTable"),
                    ReferencedColumn = reader.GetString("ReferencedColumn"),
                    ReferencedSchema = reader.GetString("ReferencedSchema")
                });
            }

            return foreignKeys;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SQL Server foreign keys for table {TableName}", tableName);
            throw;
        }
    }

    public async Task<string> ExportToCsvAsync(string tableName, string? schema = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var effectiveSchema = schema ?? "dbo";
            var limitClause = limit.HasValue ? $"TOP {limit}" : "";
            var query = $"SELECT {limitClause} * FROM [{effectiveSchema}].[{tableName}]";

            var result = await ExecuteQueryAsync(query, cancellationToken);

            if (!result.Success)
            {
                throw new InvalidOperationException($"Failed to export table: {result.ErrorMessage}");
            }

            var csv = new StringBuilder();

            // Add headers
            if (result.ColumnNames != null)
            {
                csv.AppendLine(string.Join(",", result.ColumnNames));
            }

            // Add data rows
            if (result.Rows != null)
            {
                foreach (var row in result.Rows)
                {
                    var values = row.Select(value =>
                    {
                        var stringValue = value?.ToString() ?? "";
                        // Escape commas and quotes in CSV
                        if (stringValue.Contains(",") || stringValue.Contains("\"") || stringValue.Contains("\n"))
                        {
                            stringValue = "\"" + stringValue.Replace("\"", "\"\"") + "\"";
                        }
                        return stringValue;
                    });
                    csv.AppendLine(string.Join(",", values));
                }
            }

            return csv.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export SQL Server table {TableName} to CSV", tableName);
            throw;
        }
    }

    public async Task<ExecutionPlan> GetExecutionPlanAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            var plans = new List<string>();
            var steps = new List<ExecutionPlanStep>();
            var warnings = new List<string>();
            double totalCost = 0;
            double totalRows = 0;

            // Enable execution plan capture
            using (var enablePlanCommand = new SqlCommand("SET SHOWPLAN_ALL ON", connection))
            {
                await enablePlanCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            try
            {
                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var stepText = reader.GetString("StmtText");
                    var estimatedRows = reader.IsDBNull("EstimateRows") ? 0 : reader.GetFloat("EstimateRows");
                    var estimatedCost = reader.IsDBNull("TotalSubtreeCost") ? 0 : reader.GetFloat("TotalSubtreeCost");
                    var physicalOp = reader.IsDBNull("PhysicalOp") ? "" : reader.GetString("PhysicalOp");
                    var logicalOp = reader.IsDBNull("LogicalOp") ? "" : reader.GetString("LogicalOp");

                    plans.Add($"Operation: {physicalOp}, Cost: {estimatedCost:F4}, Rows: {estimatedRows:F0}");

                    var step = new ExecutionPlanStep
                    {
                        Operation = $"{physicalOp} ({logicalOp})",
                        Details = stepText,
                        Cost = estimatedCost,
                        Rows = estimatedRows,
                        IsExpensive = estimatedCost > 0.1 || estimatedRows > 1000
                    };

                    // Analyze step for suggestions
                    if (physicalOp.Contains("Scan") && !physicalOp.Contains("Index"))
                    {
                        step.Suggestions.Add("Table scan detected - consider adding appropriate indexes");
                        warnings.Add("Table scan operations found - performance may be impacted");
                    }

                    if (physicalOp.Contains("Sort") && estimatedRows > 1000)
                    {
                        step.Suggestions.Add("Large sort operation - consider adding index to support ORDER BY");
                        warnings.Add($"Sorting {estimatedRows:N0} rows - consider indexing strategy");
                    }

                    if (physicalOp.Contains("Hash Match") || physicalOp.Contains("Nested Loops"))
                    {
                        step.Suggestions.Add("Join operation - ensure proper indexes on join columns");
                    }

                    if (estimatedRows > 10000)
                    {
                        step.Suggestions.Add("Large number of rows - consider adding WHERE clause filters");
                        warnings.Add($"Operation processes {estimatedRows:N0} rows");
                    }

                    steps.Add(step);
                    totalCost += estimatedCost;
                    totalRows += estimatedRows;
                }
            }
            finally
            {
                // Disable execution plan capture
                using var disablePlanCommand = new SqlCommand("SET SHOWPLAN_ALL OFF", connection);
                await disablePlanCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            // Also get XML plan for more detailed analysis
            string xmlPlan = "";
            try
            {
                using var xmlPlanCommand = new SqlCommand($"SET SHOWPLAN_XML ON; {query}; SET SHOWPLAN_XML OFF", connection);
                var xmlResult = await xmlPlanCommand.ExecuteScalarAsync(cancellationToken);
                xmlPlan = xmlResult?.ToString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get XML execution plan");
            }

            return new ExecutionPlan
            {
                Query = query,
                PlanText = string.Join(Environment.NewLine, plans),
                PlanXml = xmlPlan,
                EstimatedCost = totalCost,
                EstimatedRows = totalRows,
                EstimatedExecutionTime = TimeSpan.FromMilliseconds(totalCost * 100), // Rough estimation
                Steps = steps,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution plan for SQL Server");
            throw new InvalidOperationException($"Failed to get execution plan: {ex.Message}", ex);
        }
    }

    public async Task<QueryAnalysis> AnalyzeQueryPerformanceAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            var executionPlan = await GetExecutionPlanAsync(query, cancellationToken);

            var issues = new List<string>();
            var recommendations = new List<string>();
            var indexSuggestions = new List<string>();
            var rewriteSuggestions = new List<string>();
            var performanceRating = "Good";
            double potentialImprovement = 0;

            // Analyze execution plan for issues
            bool hasTableScans = false;
            bool hasHighCost = false;
            bool hasSorts = false;
            bool hasHashJoins = false;
            int expensiveOperations = 0;

            foreach (var step in executionPlan.Steps)
            {
                if (step.Operation.Contains("Scan") && !step.Operation.Contains("Index"))
                {
                    hasTableScans = true;
                    issues.Add($"Table scan detected in operation: {step.Operation}");
                    indexSuggestions.Add("CREATE NONCLUSTERED INDEX IX_<TableName>_<Column> ON <TableName>(<Column>)");
                    potentialImprovement += 60;
                }

                if (step.Cost > 0.5)
                {
                    hasHighCost = true;
                    issues.Add($"High-cost operation ({step.Cost:F2}): {step.Operation}");
                }

                if (step.Operation.Contains("Sort"))
                {
                    hasSorts = true;
                    issues.Add($"Sort operation with {step.Rows:N0} rows");
                    indexSuggestions.Add("Consider creating index to support ORDER BY clause");
                    potentialImprovement += 30;
                }

                if (step.Operation.Contains("Hash"))
                {
                    hasHashJoins = true;
                    issues.Add("Hash join detected - may indicate missing or inadequate indexes");
                    potentialImprovement += 25;
                }

                if (step.IsExpensive)
                {
                    expensiveOperations++;
                }
            }

            // Query pattern analysis
            var queryLower = query.ToLowerInvariant();

            if (queryLower.Contains("select *"))
            {
                issues.Add("SELECT * detected - specify only needed columns");
                rewriteSuggestions.Add("Replace 'SELECT *' with specific column names");
                potentialImprovement += 15;
            }

            if (queryLower.Contains("nolock"))
            {
                issues.Add("NOLOCK hint detected - may return dirty reads");
                rewriteSuggestions.Add("Consider using READ_COMMITTED_SNAPSHOT instead of NOLOCK");
            }

            if (!queryLower.Contains("top") && !queryLower.Contains("offset") && queryLower.Contains("select"))
            {
                issues.Add("No TOP or OFFSET clause - could return excessive data");
                rewriteSuggestions.Add("Add TOP clause or implement pagination with OFFSET/FETCH");
                potentialImprovement += 25;
            }

            if (queryLower.Contains("like '%") || queryLower.Contains("like N'%"))
            {
                issues.Add("Leading wildcard in LIKE pattern - cannot use indexes");
                rewriteSuggestions.Add("Avoid leading wildcards or consider full-text search");
                potentialImprovement += 35;
            }

            if (queryLower.Contains("or "))
            {
                issues.Add("OR conditions detected - may prevent index usage");
                rewriteSuggestions.Add("Consider rewriting OR conditions using UNION or separate queries");
                potentialImprovement += 20;
            }

            if (queryLower.Contains("function(") && queryLower.Contains("where"))
            {
                issues.Add("Functions in WHERE clause - prevents index usage");
                rewriteSuggestions.Add("Avoid functions on columns in WHERE clause");
                potentialImprovement += 40;
            }

            // Determine performance rating
            if (hasTableScans || executionPlan.EstimatedCost > 5 || expensiveOperations > 3)
            {
                performanceRating = "Poor";
            }
            else if (hasHighCost || hasSorts || hasHashJoins || expensiveOperations > 1)
            {
                performanceRating = "Fair";
            }
            else if (issues.Count > 0)
            {
                performanceRating = "Good";
            }
            else
            {
                performanceRating = "Excellent";
            }

            // SQL Server specific recommendations
            if (hasTableScans)
            {
                recommendations.Add("Create appropriate indexes to eliminate table scans");
                recommendations.Add("Use SQL Server Database Engine Tuning Advisor for index recommendations");
            }

            if (hasHashJoins)
            {
                recommendations.Add("Review join conditions and create indexes on join columns");
                recommendations.Add("Consider using MERGE JOIN by creating appropriate indexes");
            }

            if (hasSorts)
            {
                recommendations.Add("Create indexes that match ORDER BY columns to eliminate sort operations");
            }

            recommendations.Add("Update table statistics regularly with UPDATE STATISTICS");
            recommendations.Add("Monitor query performance with Query Store");
            recommendations.Add("Consider using columnstore indexes for analytical queries");

            return new QueryAnalysis
            {
                OriginalQuery = query,
                ExecutionPlan = executionPlan,
                PerformanceRating = performanceRating,
                Issues = issues,
                Recommendations = recommendations,
                OptimizedQuery = null, // Would require sophisticated query parsing and rewriting
                IndexSuggestions = indexSuggestions,
                RewriteSuggestions = rewriteSuggestions,
                PotentialImprovement = Math.Min(potentialImprovement, 95) // Cap at 95%
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze query performance for SQL Server");
            throw new InvalidOperationException($"Failed to analyze query performance: {ex.Message}", ex);
        }
    }
}
