using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace MsDbServer.Transport;

/// <summary>
/// STDIO transport provider for MCP server
/// </summary>
public class StdioTransportProvider : ITransportProvider
{
    private readonly ILogger<StdioTransportProvider> _logger;

    public StdioTransportProvider(ILogger<StdioTransportProvider> logger)
    {
        _logger = logger;
    }

    public string Name => "STDIO";

    public bool IsEnabled => true; // Always enabled for backward compatibility

    public void ConfigureServices(IServiceCollection services)
    {
        _logger.LogInformation("Configuring STDIO transport services");
        
        // STDIO transport is handled by the MCP server itself
        // No additional services needed
    }

    public void ConfigureHost(IHostBuilder builder)
    {
        _logger.LogInformation("Configuring STDIO transport host");
        
        // STDIO transport uses the default host configuration
        // The MCP server will handle STDIO communication
    }
}
