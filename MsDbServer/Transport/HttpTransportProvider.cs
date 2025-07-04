using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore;
using MsDbServer.Infrastructure.HealthChecks;

namespace MsDbServer.Transport;

/// <summary>
/// HTTP transport provider for MCP server using ASP.NET Core
/// </summary>
public class HttpTransportProvider : ITransportProvider
{
    private readonly ILogger<HttpTransportProvider> _logger;
    private readonly HttpTransportOptions _options;

    public HttpTransportProvider(ILogger<HttpTransportProvider> logger, HttpTransportOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public string Name => "HTTP";

    public bool IsEnabled => _options.Enabled;

    public void ConfigureServices(IServiceCollection services)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("HTTP transport is disabled");
            return;
        }

        _logger.LogInformation("Configuring HTTP transport services on port {Port}", _options.Port);
        
        // Add ASP.NET Core services
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        // Add health checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database");
        
        // Add CORS if needed
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }

    public void ConfigureHost(IHostBuilder builder)
    {
        if (!IsEnabled)
        {
            return;
        }

        _logger.LogInformation("Configuring HTTP transport host");
        
        builder.ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseUrls($"http://localhost:{_options.Port}");
            webBuilder.Configure(app =>
            {
                var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
                
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseRouting();
                app.UseCors();
                
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapHealthChecks("/health");
                    // MCP endpoints will be added via the MCP server configuration
                });
            });
        });
    }
}

/// <summary>
/// Configuration options for HTTP transport
/// </summary>
public class HttpTransportOptions
{
    public bool Enabled { get; set; } = false;
    public int Port { get; set; } = 8080;
    public bool EnableSwagger { get; set; } = true;
    public bool EnableHealthChecks { get; set; } = true;
}
