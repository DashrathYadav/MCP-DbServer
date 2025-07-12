using MsDbServer.Domain.Interfaces;
using MsDbServer.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace MsDbServer.Infrastructure.Factories;

/// <summary>
/// Factory for creating database repositories
/// </summary>
public class DatabaseRepositoryFactory : IDatabaseRepositoryFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public DatabaseRepositoryFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public IDatabaseRepository CreateRepository(string provider, string connectionString)
    {
        return provider.ToLowerInvariant() switch
        {
            "mysql" => new MySqlDatabaseRepository(connectionString, _loggerFactory.CreateLogger<MySqlDatabaseRepository>()),
            "mssql" or "sqlserver" => new SqlServerDatabaseRepository(connectionString, _loggerFactory.CreateLogger<SqlServerDatabaseRepository>()),
            "postgresql" => throw new NotImplementedException("PostgreSQL support will be added in Phase 3"),
            _ => throw new ArgumentException($"Unsupported database provider: {provider}", nameof(provider))
        };
    }

    public IEnumerable<string> GetSupportedProviders()
    {
        return new[] { "mysql", "mssql", "sqlserver" };
    }
}
