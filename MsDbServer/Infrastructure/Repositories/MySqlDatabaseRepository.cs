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
                TABLE_SCHEMA as Schema,
                TABLE_NAME as Name,
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
                Schema = reader.GetString("Schema"),
                Name = reader.GetString("Name"),
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
                COLUMN_NAME as Name,
                DATA_TYPE as DataType,
                IS_NULLABLE as IsNullable,
                COLUMN_DEFAULT as DefaultValue,
                CHARACTER_MAXIMUM_LENGTH as MaxLength,
                NUMERIC_PRECISION as Precision,
                NUMERIC_SCALE as Scale,
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
            columns.Add(new ColumnInfo
            {
                Name = reader.GetString("Name"),
                DataType = reader.GetString("DataType"),
                IsNullable = reader.GetString("IsNullable").Equals("YES", StringComparison.OrdinalIgnoreCase),
                IsPrimaryKey = reader.GetString("ColumnKey").Equals("PRI", StringComparison.OrdinalIgnoreCase),
                DefaultValue = reader.IsDBNull("DefaultValue") ? null : reader.GetString("DefaultValue"),
                MaxLength = reader.IsDBNull("MaxLength") ? null : reader.GetInt32("MaxLength"),
                Precision = reader.IsDBNull("Precision") ? null : reader.GetInt32("Precision"),
                Scale = reader.IsDBNull("Scale") ? null : reader.GetInt32("Scale")
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
}
