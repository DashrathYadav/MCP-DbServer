using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using MsDbServer.Configuration;
using MsDbServer.Transport;
using MsDbServer.Application.Services;
using MsDbServer.Domain.Interfaces;
using MsDbServer.Infrastructure.Factories;
using MsDbServer.Infrastructure.HealthChecks;
using McpOptions = MsDbServer.Configuration.McpServerOptions;

var builder = Host.CreateApplicationBuilder(args);

// Configure options from appsettings.json
builder.Services.Configure<McpOptions>(builder.Configuration.GetSection(McpOptions.SectionName));

// Configure logging to stderr for MCP compliance
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Get configuration
var config = builder.Configuration.GetSection(McpOptions.SectionName).Get<McpOptions>() ?? new McpOptions();

// Configure database services
builder.Services.AddSingleton<IDatabaseRepositoryFactory, DatabaseRepositoryFactory>();
builder.Services.AddSingleton<IDatabaseRepository>(provider =>
{
    var factory = provider.GetRequiredService<IDatabaseRepositoryFactory>();
    return factory.CreateRepository(config.Database.Provider, config.Database.ConnectionString);
});
builder.Services.AddSingleton<DatabaseService>();

// Configure health checks
builder.Services.AddSingleton<DatabaseHealthCheck>();

// Configure transport services
builder.Services.AddSingleton<TransportManager>();
builder.Services.AddSingleton<StdioTransportProvider>();
builder.Services.AddSingleton(provider => new HttpTransportProvider(
    provider.GetRequiredService<ILogger<HttpTransportProvider>>(), 
    config.Transport.Http));

// Configure MCP server with tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

// Configure transport manager
var transportManager = host.Services.GetRequiredService<TransportManager>();
var stdioProvider = host.Services.GetRequiredService<StdioTransportProvider>();
var httpProvider = host.Services.GetRequiredService<HttpTransportProvider>();

transportManager.AddProvider(stdioProvider);
transportManager.AddProvider(httpProvider);

// Test database connection on startup
var databaseService = host.Services.GetRequiredService<DatabaseService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting MCP Database Server with modern architecture");
    logger.LogInformation("Enabled transports: {Transports}", 
        string.Join(", ", transportManager.GetEnabledProviders().Select(p => p.Name)));
    
    var connectionTest = await databaseService.TestConnectionAsync();
    if (!connectionTest)
    {
        logger.LogError("Database connection test failed. Please check your connection string.");
        return;
    }
    
    logger.LogInformation("Database connection test successful. Starting MCP server...");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to test database connection");
    return;
}

await host.RunAsync();
