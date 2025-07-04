using MsDbServer.Domain.Interfaces;
using MsDbServer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace MsDbServer.Application.Services;

/// <summary>
/// Application service for database operations
/// </summary>
public class DatabaseService
{
    private readonly IDatabaseRepository _repository;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IDatabaseRepository repository, ILogger<DatabaseService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Test database connectivity
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing database connection");
            var result = await _repository.TestConnectionAsync(cancellationToken);
            _logger.LogInformation("Database connection test result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            return false;
        }
    }

    /// <summary>
    /// Execute a SQL query with proper error handling and logging
    /// </summary>
    public async Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing query: {Query}", query);
            var result = await _repository.ExecuteQueryAsync(query, cancellationToken);
            _logger.LogInformation("Query executed successfully. Rows returned: {RowCount}", result.RowCount);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query: {Query}", query);
            return new QueryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Execute a non-query command with proper error handling
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string command, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing non-query command: {Command}", command);
            var result = await _repository.ExecuteNonQueryAsync(command, cancellationToken);
            _logger.LogInformation("Non-query command executed successfully. Rows affected: {RowsAffected}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing non-query command: {Command}", command);
            throw;
        }
    }

    /// <summary>
    /// Get database schema information
    /// </summary>
    public async Task<IEnumerable<SchemaInfo>> GetSchemasAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving database schemas");
            var schemas = await _repository.GetSchemasAsync(cancellationToken);
            _logger.LogInformation("Retrieved {Count} schemas", schemas.Count());
            return schemas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database schemas");
            throw;
        }
    }

    /// <summary>
    /// Get table information
    /// </summary>
    public async Task<IEnumerable<TableInfo>> GetTablesAsync(string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving tables for schema: {Schema}", schema ?? "default");
            var tables = await _repository.GetTablesAsync(schema, cancellationToken);
            _logger.LogInformation("Retrieved {Count} tables", tables.Count());
            return tables;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tables for schema: {Schema}", schema);
            throw;
        }
    }

    /// <summary>
    /// Get detailed table information
    /// </summary>
    public async Task<TableInfo?> GetTableInfoAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving table info for: {Schema}.{Table}", schema ?? "default", tableName);
            var tableInfo = await _repository.GetTableInfoAsync(tableName, schema, cancellationToken);
            _logger.LogInformation("Retrieved table info for: {Schema}.{Table}", schema ?? "default", tableName);
            return tableInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving table info for: {Schema}.{Table}", schema, tableName);
            throw;
        }
    }

    /// <summary>
    /// Export table data as CSV
    /// </summary>
    public async Task<string> ExportToCsvAsync(string tableName, string? schema = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Exporting table to CSV: {Schema}.{Table} (Limit: {Limit})", 
                schema ?? "default", tableName, limit?.ToString() ?? "unlimited");
            
            var csv = await _repository.ExportToCsvAsync(tableName, schema, limit, cancellationToken);
            
            _logger.LogInformation("Successfully exported table to CSV: {Schema}.{Table}", 
                schema ?? "default", tableName);
            
            return csv;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting table to CSV: {Schema}.{Table}", schema, tableName);
            throw;
        }
    }
}
