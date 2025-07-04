using MsDbServer.Domain.Models;

namespace MsDbServer.Domain.Interfaces;

/// <summary>
/// Repository interface for database operations
/// </summary>
public interface IDatabaseRepository
{
    /// <summary>
    /// Test the database connection
    /// </summary>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a query and return results
    /// </summary>
    Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a non-query command (INSERT, UPDATE, DELETE)
    /// </summary>
    Task<int> ExecuteNonQueryAsync(string command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all database schemas
    /// </summary>
    Task<IEnumerable<SchemaInfo>> GetSchemasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all tables in a schema
    /// </summary>
    Task<IEnumerable<TableInfo>> GetTablesAsync(string? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed information about a specific table
    /// </summary>
    Task<TableInfo?> GetTableInfoAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get column information for a table
    /// </summary>
    Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get foreign key relationships for a table
    /// </summary>
    Task<IEnumerable<ForeignKeyInfo>> GetForeignKeysAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export table data as CSV
    /// </summary>
    Task<string> ExportToCsvAsync(string tableName, string? schema = null, int? limit = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Factory interface for creating database repositories
/// </summary>
public interface IDatabaseRepositoryFactory
{
    /// <summary>
    /// Create a repository for the specified database provider
    /// </summary>
    IDatabaseRepository CreateRepository(string provider, string connectionString);

    /// <summary>
    /// Get all supported database providers
    /// </summary>
    IEnumerable<string> GetSupportedProviders();
}
