using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System.Data;

namespace MsDbServer;

/// <summary>
/// MySQL database service implementation
/// </summary>
public class MySqlDatabaseService : IDatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlDatabaseService> _logger;

    public MySqlDatabaseService(IConfiguration configuration, ILogger<MySqlDatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "server=localhost;port=3306;database=rent_wizard;user=root;password=Temp@123";
        _logger = logger;
    }

    public async Task<TableDescription> DescribeTableAsync(string tableName, string? schemaName = null)
    {
        _logger.LogInformation("Describing table: {TableName} in schema: {SchemaName}", tableName, schemaName ?? "default");

        var tableDescription = new TableDescription
        {
            TableName = tableName,
            Schema = schemaName ?? "default"
        };

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        // Get column information
        await GetColumnInformationAsync(connection, tableDescription);

        // Get primary key information
        await GetPrimaryKeyInformationAsync(connection, tableDescription);

        // Get foreign key information
        await GetForeignKeyInformationAsync(connection, tableDescription);

        // Get index information
        await GetIndexInformationAsync(connection, tableDescription);

        return tableDescription;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            _logger.LogInformation("MySQL database connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MySQL database connection test failed");
            return false;
        }
    }

    public async Task<List<string>> GetTablesAsync()
    {
        var tables = new List<string>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var databaseName = connection.Database;
        var query = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @DatabaseName
            AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    public async Task<QueryResults> ExecuteQueryAsync(string query, int maxRows = 100)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new QueryResults
        {
            Query = query,
            TotalRows = 0,
            HasMoreRows = false
        };

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        using var command = new MySqlCommand(query, connection);
        command.CommandTimeout = 30; // 30 seconds timeout

        using var reader = await command.ExecuteReaderAsync();

        // Get column names
        for (int i = 0; i < reader.FieldCount; i++)
        {
            results.ColumnNames.Add(reader.GetName(i));
        }

        // Read data rows
        int rowCount = 0;
        while (await reader.ReadAsync() && rowCount < maxRows)
        {
            var row = new List<object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row.Add(reader.IsDBNull(i) ? null : reader.GetValue(i));
            }
            results.Rows.Add(row);
            rowCount++;
        }

        results.TotalRows = rowCount;
        
        // Check if there are more rows
        if (rowCount == maxRows)
        {
            results.HasMoreRows = await reader.ReadAsync();
        }

        stopwatch.Stop();
        results.ExecutionTime = stopwatch.Elapsed;

        return results;
    }

    public async Task<DatabaseStats> GetDatabaseStatsAsync()
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var stats = new DatabaseStats
        {
            DatabaseName = connection.Database,
            GeneratedAt = DateTime.UtcNow
        };

        // Get database version
        await GetDatabaseVersionAsync(connection, stats);

        // Get table count and database size
        await GetDatabaseSizeInfoAsync(connection, stats);

        // Get individual table statistics
        await GetTableStatsAsync(connection, stats);

        return stats;
    }

    public async Task<List<TableDescription>> GetSchemaInfoAsync(string? tablePattern = null)
    {
        var tables = new List<string>();
        
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var databaseName = connection.Database;
        var query = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @DatabaseName
            AND TABLE_TYPE = 'BASE TABLE'";

        if (!string.IsNullOrWhiteSpace(tablePattern))
        {
            query += " AND TABLE_NAME LIKE @TablePattern";
        }

        query += " ORDER BY TABLE_NAME";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        if (!string.IsNullOrWhiteSpace(tablePattern))
        {
            command.Parameters.AddWithValue("@TablePattern", tablePattern);
        }

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        // Get detailed description for each table
        var schemaInfo = new List<TableDescription>();
        foreach (var tableName in tables)
        {
            var tableDesc = await DescribeTableAsync(tableName);
            schemaInfo.Add(tableDesc);
        }

        return schemaInfo;
    }

    public async Task<List<TableRelationship>> GetTableRelationshipsAsync(string? tableName = null)
    {
        var relationships = new List<TableRelationship>();

        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var databaseName = connection.Database;
        var query = @"
            SELECT 
                kcu.CONSTRAINT_NAME,
                kcu.TABLE_NAME as CHILD_TABLE,
                kcu.COLUMN_NAME as CHILD_COLUMN,
                kcu.REFERENCED_TABLE_NAME as PARENT_TABLE,
                kcu.REFERENCED_COLUMN_NAME as PARENT_COLUMN,
                kcu.TABLE_SCHEMA as CHILD_SCHEMA,
                kcu.REFERENCED_TABLE_SCHEMA as PARENT_SCHEMA
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
            WHERE kcu.TABLE_SCHEMA = @DatabaseName
            AND kcu.REFERENCED_TABLE_NAME IS NOT NULL";

        if (!string.IsNullOrWhiteSpace(tableName))
        {
            query += " AND (kcu.TABLE_NAME = @TableName OR kcu.REFERENCED_TABLE_NAME = @TableName)";
        }

        query += " ORDER BY kcu.TABLE_NAME, kcu.CONSTRAINT_NAME";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            command.Parameters.AddWithValue("@TableName", tableName);
        }

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var relationship = new TableRelationship
            {
                ConstraintName = reader.GetString("CONSTRAINT_NAME"),
                ChildTable = reader.GetString("CHILD_TABLE"),
                ChildColumn = reader.GetString("CHILD_COLUMN"),
                ParentTable = reader.GetString("PARENT_TABLE"),
                ParentColumn = reader.GetString("PARENT_COLUMN"),
                ChildSchema = reader.GetString("CHILD_SCHEMA"),
                ParentSchema = reader.GetString("PARENT_SCHEMA")
            };

            relationships.Add(relationship);
        }

        return relationships;
    }

    private async Task GetColumnInformationAsync(MySqlConnection connection, TableDescription tableDescription)
    {
        var databaseName = connection.Database;
        var query = @"
            SELECT 
                COLUMN_NAME,
                DATA_TYPE,
                IS_NULLABLE,
                COLUMN_DEFAULT,
                CHARACTER_MAXIMUM_LENGTH,
                NUMERIC_PRECISION,
                NUMERIC_SCALE,
                EXTRA
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @TableName 
                AND TABLE_SCHEMA = @DatabaseName
            ORDER BY ORDINAL_POSITION";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", tableDescription.TableName);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnInfo = new ColumnInfo
            {
                ColumnName = reader.GetString("COLUMN_NAME"),
                DataType = reader.GetString("DATA_TYPE"),
                IsNullable = reader.GetString("IS_NULLABLE") == "YES",
                DefaultValue = reader.IsDBNull("COLUMN_DEFAULT") ? null : reader.GetString("COLUMN_DEFAULT"),
                IsIdentity = reader.GetString("EXTRA").Contains("auto_increment"),
                MaxLength = reader.IsDBNull("CHARACTER_MAXIMUM_LENGTH") ? null : GetSafeIntValue(reader.GetValue("CHARACTER_MAXIMUM_LENGTH")),
                Precision = reader.IsDBNull("NUMERIC_PRECISION") ? null : GetSafeIntValue(reader.GetValue("NUMERIC_PRECISION")),
                Scale = reader.IsDBNull("NUMERIC_SCALE") ? null : GetSafeIntValue(reader.GetValue("NUMERIC_SCALE"))
            };

            tableDescription.Columns.Add(columnInfo);
        }
    }

    private async Task GetPrimaryKeyInformationAsync(MySqlConnection connection, TableDescription tableDescription)
    {
        var databaseName = connection.Database;
        var query = @"
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_NAME = @TableName 
                AND TABLE_SCHEMA = @DatabaseName
                AND CONSTRAINT_NAME = 'PRIMARY'
            ORDER BY ORDINAL_POSITION";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", tableDescription.TableName);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var columnName = reader.GetString("COLUMN_NAME");
            tableDescription.PrimaryKeys.Add(columnName);

            // Mark column as primary key
            var column = tableDescription.Columns.FirstOrDefault(c => c.ColumnName == columnName);
            if (column != null)
            {
                column.IsPrimaryKey = true;
            }
        }
    }

    private async Task GetForeignKeyInformationAsync(MySqlConnection connection, TableDescription tableDescription)
    {
        var databaseName = connection.Database;
        var query = @"
            SELECT 
                CONSTRAINT_NAME,
                COLUMN_NAME,
                REFERENCED_TABLE_NAME,
                REFERENCED_COLUMN_NAME,
                REFERENCED_TABLE_SCHEMA
            FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
            WHERE TABLE_NAME = @TableName 
                AND TABLE_SCHEMA = @DatabaseName
                AND REFERENCED_TABLE_NAME IS NOT NULL";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", tableDescription.TableName);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var foreignKey = new ForeignKeyInfo
            {
                ForeignKeyName = reader.GetString("CONSTRAINT_NAME"),
                ColumnName = reader.GetString("COLUMN_NAME"),
                ReferencedTable = reader.GetString("REFERENCED_TABLE_NAME"),
                ReferencedColumn = reader.GetString("REFERENCED_COLUMN_NAME"),
                ReferencedSchema = reader.GetString("REFERENCED_TABLE_SCHEMA")
            };

            tableDescription.ForeignKeys.Add(foreignKey);
        }
    }

    private async Task GetIndexInformationAsync(MySqlConnection connection, TableDescription tableDescription)
    {
        var databaseName = connection.Database;
        var query = @"
            SELECT 
                INDEX_NAME,
                NON_UNIQUE,
                COLUMN_NAME
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_NAME = @TableName 
                AND TABLE_SCHEMA = @DatabaseName
                AND INDEX_NAME != 'PRIMARY'
            ORDER BY INDEX_NAME, SEQ_IN_INDEX";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@TableName", tableDescription.TableName);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        var indexDict = new Dictionary<string, IndexInfo>();

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var indexName = reader.GetString("INDEX_NAME");
            var columnName = reader.GetString("COLUMN_NAME");
            var isUnique = reader.GetInt32("NON_UNIQUE") == 0;

            if (!indexDict.ContainsKey(indexName))
            {
                indexDict[indexName] = new IndexInfo
                {
                    IndexName = indexName,
                    IsUnique = isUnique,
                    Columns = new List<string>()
                };
            }

            indexDict[indexName].Columns.Add(columnName);
        }

        tableDescription.Indexes = indexDict.Values.ToList();
    }

    private static int? GetSafeIntValue(object value)
    {
        if (value == null || value == DBNull.Value)
            return null;

        try
        {
            // Handle different numeric types that MySQL might return
            return value switch
            {
                int intValue => intValue,
                long longValue => longValue > int.MaxValue ? int.MaxValue : (int)longValue,
                uint uintValue => uintValue > int.MaxValue ? int.MaxValue : (int)uintValue,
                ulong ulongValue => ulongValue > int.MaxValue ? int.MaxValue : (int)ulongValue,
                byte byteValue => byteValue,
                sbyte sbyteValue => sbyteValue,
                short shortValue => shortValue,
                ushort ushortValue => ushortValue,
                _ => int.TryParse(value.ToString(), out var parsed) ? parsed : null
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task GetDatabaseVersionAsync(MySqlConnection connection, DatabaseStats stats)
    {
        var query = "SELECT VERSION() as Version";
        using var command = new MySqlCommand(query, connection);
        var version = await command.ExecuteScalarAsync();
        stats.DatabaseVersion = version?.ToString() ?? "Unknown";
    }

    private async Task GetDatabaseSizeInfoAsync(MySqlConnection connection, DatabaseStats stats)
    {
        var databaseName = connection.Database;
        var query = @"
            SELECT 
                COUNT(*) as TableCount,
                ROUND(SUM(data_length + index_length), 0) as DatabaseSizeBytes
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_SCHEMA = @DatabaseName 
            AND TABLE_TYPE = 'BASE TABLE'";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            stats.TableCount = reader.GetInt32("TableCount");
            stats.DatabaseSizeBytes = reader.IsDBNull("DatabaseSizeBytes") ? 0 : Convert.ToInt64(reader.GetValue("DatabaseSizeBytes"));
        }
    }

    private async Task GetTableStatsAsync(MySqlConnection connection, DatabaseStats stats)
    {
        var databaseName = connection.Database;
        var query = @"
            SELECT 
                TABLE_NAME,
                TABLE_ROWS,
                DATA_LENGTH,
                INDEX_LENGTH,
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME AND TABLE_SCHEMA = t.TABLE_SCHEMA) as COLUMN_COUNT,
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_NAME = t.TABLE_NAME AND TABLE_SCHEMA = t.TABLE_SCHEMA AND INDEX_NAME != 'PRIMARY') as INDEX_COUNT
            FROM INFORMATION_SCHEMA.TABLES t
            WHERE TABLE_SCHEMA = @DatabaseName 
            AND TABLE_TYPE = 'BASE TABLE'
            ORDER BY DATA_LENGTH DESC";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@DatabaseName", databaseName);

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tableStat = new TableStats
            {
                TableName = reader.GetString("TABLE_NAME"),
                RowCount = reader.IsDBNull("TABLE_ROWS") ? 0 : Convert.ToInt64(reader.GetValue("TABLE_ROWS")),
                DataSizeBytes = reader.IsDBNull("DATA_LENGTH") ? 0 : Convert.ToInt64(reader.GetValue("DATA_LENGTH")),
                IndexSizeBytes = reader.IsDBNull("INDEX_LENGTH") ? 0 : Convert.ToInt64(reader.GetValue("INDEX_LENGTH")),
                ColumnCount = reader.GetInt32("COLUMN_COUNT"),
                IndexCount = reader.GetInt32("INDEX_COUNT")
            };

            stats.TableStats.Add(tableStat);
        }
    }
}
