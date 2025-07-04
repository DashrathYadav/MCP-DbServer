using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MsDbServer.Transport;

/// <summary>
/// Manages multiple transport providers for the MCP server
/// </summary>
public class TransportManager
{
    private readonly ILogger<TransportManager> _logger;
    private readonly List<ITransportProvider> _providers = new();

    public TransportManager(ILogger<TransportManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Add a transport provider to the manager
    /// </summary>
    public void AddProvider(ITransportProvider provider)
    {
        _providers.Add(provider);
        _logger.LogInformation("Added transport provider: {Name} (Enabled: {Enabled})", 
            provider.Name, provider.IsEnabled);
    }

    /// <summary>
    /// Configure all services for enabled transport providers
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        var enabledProviders = _providers.Where(p => p.IsEnabled).ToList();
        
        _logger.LogInformation("Configuring services for {Count} enabled transport providers", 
            enabledProviders.Count);

        foreach (var provider in enabledProviders)
        {
            _logger.LogDebug("Configuring services for {Name} transport", provider.Name);
            provider.ConfigureServices(services);
        }
    }

    /// <summary>
    /// Configure the host for all enabled transport providers
    /// </summary>
    public void ConfigureHost(IHostBuilder builder)
    {
        var enabledProviders = _providers.Where(p => p.IsEnabled).ToList();
        
        _logger.LogInformation("Configuring host for {Count} enabled transport providers", 
            enabledProviders.Count);

        foreach (var provider in enabledProviders)
        {
            _logger.LogDebug("Configuring host for {Name} transport", provider.Name);
            provider.ConfigureHost(builder);
        }
    }

    /// <summary>
    /// Get all enabled transport providers
    /// </summary>
    public IEnumerable<ITransportProvider> GetEnabledProviders()
    {
        return _providers.Where(p => p.IsEnabled);
    }
}
