using MsDbServer.Domain.Interfaces;
using MsDbServer.Domain.Models;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text;
using System.Diagnostics;

namespace MsDbServer.Infrastructure.Repositories;

/// <summary>
/// MySQL implementation of the database repository
/// </summary>
public class MySqlDatabaseRepository : IDatabaseRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlDatabaseRepository> _logger;

    public MySqlDatabaseRepository(string connectionString, ILogger<MySqlDatabaseRepository> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection.State == ConnectionState.Open;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test MySQL connection");
            return false;
        }
    }

    public async Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = new MySqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var columnNames = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames[i] = reader.GetName(i);
            }

            var rows = new List<object[]>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new object[reader.FieldCount];
                reader.GetValues(row);
                rows.Add(row);
            }

            stopwatch.Stop();

            return new QueryResult
            {
                Success = true,
                ColumnNames = columnNames,
                Rows = rows.ToArray(),
                RowCount = rows.Count,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error executing query: {Query}", query);

            return new QueryResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ExecutionTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string command, CancellationToken cancellationToken = default)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var cmd = new MySqlCommand(command, connection);
        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<SchemaInfo>> GetSchemasAsync(CancellationToken cancellationToken = default)
    {
        const string query = @"
            SELECT 
                SCHEMA_NAME as Name,
                'DATABASE' as Type,
                CONCAT('MySQL Database: ', SCHEMA_NAME) as Description
            FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE SCHEMA_NAME NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys')
            ORDER BY SCHEMA_NAME";

        var result = await ExecuteQueryAsync(query, cancellationToken);

        if (!result.Success || result.Rows == null)
        {
            return Enumerable.Empty<SchemaInfo>();
        }

        return result.Rows.Select(row => new SchemaInfo
        {
            Name = row[0]?.ToString() ?? string.Empty,
            Type = row[1]?.ToString() ?? string.Empty,
            Description = row[2]?.ToString()
        });
    }

    public async Task<IEnumerable<TableInfo>> GetTablesAsync(string? schema = null, CancellationToken cancellationToken = default)
    {
        var query = @"
            SELECT 
                TABLE_SCHEMA as SchemaName,
                TABLE_NAME as TableName,
                TABLE_ROWS as RowCount
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            AND TABLE_SCHEMA NOT IN ('information_schema', 'mysql', 'performance_schema', 'sys')";

        if (!string.IsNullOrEmpty(schema))
        {
            query += " AND TABLE_SCHEMA = @schema";
        }

        query += " ORDER BY TABLE_SCHEMA, TABLE_NAME";

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new MySqlCommand(query, connection);
        if (!string.IsNullOrEmpty(schema))
        {
            command.Parameters.AddWithValue("@schema", schema);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var tables = new List<TableInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(new TableInfo
            {
                Schema = reader.GetString("SchemaName"),
                Name = reader.GetString("TableName"),
                RowCount = reader.IsDBNull("RowCount") ? 0 : reader.GetInt64("RowCount")
            });
        }

        return tables;
    }

    public async Task<TableInfo?> GetTableInfoAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        var columns = await GetColumnsAsync(tableName, schema, cancellationToken);
        var foreignKeys = await GetForeignKeysAsync(tableName, schema, cancellationToken);

        var tables = await GetTablesAsync(schema, cancellationToken);
        var table = tables.FirstOrDefault(t => t.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase));

        if (table == null)
        {
            return null;
        }

        return new TableInfo
        {
            Name = table.Name,
            Schema = table.Schema,
            RowCount = table.RowCount,
            Columns = columns.ToList(),
            ForeignKeys = foreignKeys.ToList(),
            PrimaryKeys = columns.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToList()
        };
    }

    public async Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        var query = @"
            SELECT 
                COLUMN_NAME as ColumnName,
                DATA_TYPE as DataType,
                IS_NULLABLE as IsNullable,
                COLUMN_DEFAULT as DefaultValue,
                CHARACTER_MAXIMUM_LENGTH as MaxLength,
                NUMERIC_PRECISION as NumericPrecision,
                NUMERIC_SCALE as NumericScale,
                COLUMN_KEY as ColumnKey
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName";

        if (!string.IsNullOrEmpty(schema))
        {
            query += " AND TABLE_SCHEMA = @schema";
        }

        query += " ORDER BY ORDINAL_POSITION";

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        if (!string.IsNullOrEmpty(schema))
        {
            command.Parameters.AddWithValue("@schema", schema);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<ColumnInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var maxLengthValue = reader.IsDBNull("MaxLength") ? null :
                (reader.GetValue("MaxLength") is long longVal && longVal <= int.MaxValue) ? (int?)longVal : null;
            var precisionValue = reader.IsDBNull("NumericPrecision") ? null :
                (reader.GetValue("NumericPrecision") is long longPrec && longPrec <= int.MaxValue) ? (int?)longPrec : null;
            var scaleValue = reader.IsDBNull("NumericScale") ? null :
                (reader.GetValue("NumericScale") is long longScale && longScale <= int.MaxValue) ? (int?)longScale : null;

            columns.Add(new ColumnInfo
            {
                Name = reader.GetString("ColumnName"),
                DataType = reader.GetString("DataType"),
                IsNullable = reader.GetString("IsNullable").Equals("YES", StringComparison.OrdinalIgnoreCase),
                IsPrimaryKey = reader.GetString("ColumnKey").Equals("PRI", StringComparison.OrdinalIgnoreCase),
                DefaultValue = reader.IsDBNull("DefaultValue") ? null : reader.GetString("DefaultValue"),
                MaxLength = maxLengthValue,
                Precision = precisionValue,
                Scale = scaleValue
            });
        }

        return columns;
    }

    public async Task<IEnumerable<ForeignKeyInfo>> GetForeignKeysAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        var query = @"
            SELECT 
                CONSTRAINT_NAME as Name,
                COLUMN_NAME as Column,
                REFERENCED_TABLE_NAME as ReferencedTable,
                REFERENCED_COLUMN_NAME as ReferencedColumn,
                REFERENCED_TABLE_SCHEMA as ReferencedSchema
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_NAME = @tableName
            AND REFERENCED_TABLE_NAME IS NOT NULL";

        if (!string.IsNullOrEmpty(schema))
        {
            query += " AND TABLE_SCHEMA = @schema";
        }

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@tableName", tableName);
        if (!string.IsNullOrEmpty(schema))
        {
            command.Parameters.AddWithValue("@schema", schema);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var foreignKeys = new List<ForeignKeyInfo>();
        while (await reader.ReadAsync(cancellationToken))
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString("Name"),
                Column = reader.GetString("Column"),
                ReferencedTable = reader.GetString("ReferencedTable"),
                ReferencedColumn = reader.GetString("ReferencedColumn"),
                ReferencedSchema = reader.GetString("ReferencedSchema")
            });
        }

        return foreignKeys;
    }

    public async Task<string> ExportToCsvAsync(string tableName, string? schema = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = $"SELECT * FROM {(schema != null ? $"`{schema}`." : "")}`{tableName}`";

        if (limit.HasValue)
        {
            query += $" LIMIT {limit.Value}";
        }

        var result = await ExecuteQueryAsync(query, cancellationToken);

        if (!result.Success || result.ColumnNames == null || result.Rows == null)
        {
            throw new InvalidOperationException($"Failed to export table {tableName}: {result.ErrorMessage}");
        }

        var csv = new StringBuilder();

        // Add header
        csv.AppendLine(string.Join(",", result.ColumnNames.Select(EscapeCsvField)));

        // Add data rows
        foreach (var row in result.Rows)
        {
            csv.AppendLine(string.Join(",", row.Select(field => EscapeCsvField(field?.ToString() ?? string.Empty))));
        }

        return csv.ToString();
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }

    public async Task<ExecutionPlan> GetExecutionPlanAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Get MySQL execution plan using EXPLAIN
            var explainQuery = $"EXPLAIN FORMAT=JSON {query}";

            using var command = new MySqlCommand(explainQuery, connection);
            var planJson = await command.ExecuteScalarAsync(cancellationToken) as string ?? "";

            // Also get text format for easier parsing
            var explainTextQuery = $"EXPLAIN {query}";
            using var textCommand = new MySqlCommand(explainTextQuery, connection);
            using var reader = await textCommand.ExecuteReaderAsync(cancellationToken);

            var planText = new StringBuilder();
            var steps = new List<ExecutionPlanStep>();
            var warnings = new List<string>();
            double totalCost = 0;
            double totalRows = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                var selectType = reader.GetString("select_type");
                var table = reader.IsDBNull("table") ? null : reader.GetString("table");
                var type = reader.IsDBNull("type") ? null : reader.GetString("type");
                var possibleKeys = reader.IsDBNull("possible_keys") ? null : reader.GetString("possible_keys");
                var key = reader.IsDBNull("key") ? null : reader.GetString("key");
                var rows = reader.IsDBNull("rows") ? 0 : reader.GetInt64("rows");
                var extra = reader.IsDBNull("Extra") ? null : reader.GetString("Extra");

                planText.AppendLine($"Table: {table}, Type: {type}, Rows: {rows}, Key: {key ?? "None"}");

                var step = new ExecutionPlanStep
                {
                    Operation = $"{selectType} - {type}",
                    Details = $"Table: {table}, Rows: {rows}, Key: {key ?? "None"}",
                    Cost = rows * 0.1, // Rough cost estimation
                    Rows = rows,
                    TableName = table,
                    IndexName = key,
                    IsExpensive = rows > 1000 || type == "ALL"
                };

                // Add suggestions based on analysis
                if (type == "ALL")
                {
                    step.Suggestions.Add("Consider adding an index - full table scan detected");
                    warnings.Add($"Full table scan on table '{table}' - consider adding appropriate indexes");
                }

                if (string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(possibleKeys))
                {
                    step.Suggestions.Add($"MySQL found possible keys ({possibleKeys}) but didn't use them - consider query optimization");
                }

                if (rows > 10000)
                {
                    step.Suggestions.Add("Large number of rows - consider adding WHERE clause or pagination");
                    warnings.Add($"Query processes {rows:N0} rows - performance may be impacted");
                }

                steps.Add(step);
                totalCost += step.Cost;
                totalRows += rows;
            }

            return new ExecutionPlan
            {
                Query = query,
                PlanText = planText.ToString(),
                PlanXml = planJson,
                EstimatedCost = totalCost,
                EstimatedRows = totalRows,
                EstimatedExecutionTime = TimeSpan.FromMilliseconds(totalCost * 10), // Rough estimation
                Steps = steps,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get execution plan for MySQL");
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
            bool hasFullTableScan = false;
            bool hasHighRowCount = false;
            bool hasMissingIndexes = false;
            int expensiveOperations = 0;

            foreach (var step in executionPlan.Steps)
            {
                if (step.Operation.Contains("ALL"))
                {
                    hasFullTableScan = true;
                    issues.Add($"Full table scan detected on table '{step.TableName}'");
                    indexSuggestions.Add($"CREATE INDEX idx_{step.TableName}_<column> ON {step.TableName}(<column>)");
                    potentialImprovement += 50;
                }

                if (step.Rows > 1000)
                {
                    hasHighRowCount = true;
                    issues.Add($"High row count ({step.Rows:N0}) in operation: {step.Operation}");
                }

                if (string.IsNullOrEmpty(step.IndexName) && !string.IsNullOrEmpty(step.TableName))
                {
                    hasMissingIndexes = true;
                    issues.Add($"No index used for table '{step.TableName}'");
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
                potentialImprovement += 20;
            }

            if (!queryLower.Contains("limit") && queryLower.Contains("select"))
            {
                issues.Add("No LIMIT clause - could return excessive data");
                rewriteSuggestions.Add("Add LIMIT clause to control result set size");
                potentialImprovement += 30;
            }

            if (queryLower.Contains("order by") && !queryLower.Contains("limit"))
            {
                issues.Add("ORDER BY without LIMIT - sorting entire result set");
                rewriteSuggestions.Add("Consider adding LIMIT when using ORDER BY");
                potentialImprovement += 25;
            }

            if (queryLower.Contains("like '%"))
            {
                issues.Add("Leading wildcard in LIKE pattern - cannot use indexes");
                rewriteSuggestions.Add("Avoid leading wildcards in LIKE patterns or consider full-text search");
                potentialImprovement += 40;
            }

            // Determine performance rating
            if (hasFullTableScan || expensiveOperations > 2 || executionPlan.EstimatedRows > 50000)
            {
                performanceRating = "Poor";
            }
            else if (hasMissingIndexes || hasHighRowCount || expensiveOperations > 0)
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

            // General recommendations
            if (hasFullTableScan)
            {
                recommendations.Add("Add appropriate indexes to eliminate full table scans");
            }

            if (hasHighRowCount)
            {
                recommendations.Add("Consider adding more selective WHERE conditions");
                recommendations.Add("Implement pagination for large result sets");
            }

            if (queryLower.Contains("join") && !queryLower.Contains("on"))
            {
                recommendations.Add("Ensure all JOINs have proper ON conditions");
            }

            recommendations.Add("Run ANALYZE TABLE periodically to update table statistics");
            recommendations.Add("Monitor query performance with MySQL's Performance Schema");

            // Generate optimized query suggestion (basic)
            string? optimizedQuery = null;
            if (queryLower.Contains("select *"))
            {
                optimizedQuery = query; // Would need more sophisticated parsing for real optimization
                // This is a placeholder - in a real implementation, you'd parse and rewrite the query
            }

            return new QueryAnalysis
            {
                OriginalQuery = query,
                ExecutionPlan = executionPlan,
                PerformanceRating = performanceRating,
                Issues = issues,
                Recommendations = recommendations,
                OptimizedQuery = optimizedQuery,
                IndexSuggestions = indexSuggestions,
                RewriteSuggestions = rewriteSuggestions,
                PotentialImprovement = Math.Min(potentialImprovement, 95) // Cap at 95%
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze query performance for MySQL");
            throw new InvalidOperationException($"Failed to analyze query performance: {ex.Message}", ex);
        }
    }
}
