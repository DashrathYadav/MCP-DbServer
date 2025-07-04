using Microsoft.Extensions.Diagnostics.HealthChecks;
using MsDbServer.Domain.Interfaces;

namespace MsDbServer.Infrastructure.HealthChecks;

/// <summary>
/// Health check for database connectivity
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDatabaseRepository _repository;

    public DatabaseHealthCheck(IDatabaseRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _repository.TestConnectionAsync(cancellationToken);
            
            if (isConnected)
            {
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            else
            {
                return HealthCheckResult.Unhealthy("Database connection failed");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Database health check failed: {ex.Message}", ex);
        }
    }
}
