using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MsDbServer.Transport;

/// <summary>
/// Abstraction for MCP transport providers (STDIO, HTTP, WebSocket, etc.)
/// </summary>
public interface ITransportProvider
{
    /// <summary>
    /// The name of this transport provider
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this transport is enabled in the current configuration
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Configure services needed for this transport
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// Configure the host builder for this transport
    /// </summary>
    /// <param name="builder">Host builder to configure</param>
    void ConfigureHost(IHostBuilder builder);
}
