using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using ModelContextProtocol.Server;
using MsDbServer.Configuration;
using MsDbServer.Transport;
using MsDbServer.Application.Services;
using MsDbServer.Domain.Interfaces;
using MsDbServer.Infrastructure.Factories;
using MsDbServer.Infrastructure.HealthChecks;
using McpOptions = MsDbServer.Configuration.McpServerOptions;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// Add ASP.NET Core services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure transport services
builder.Services.AddSingleton<TransportManager>();
builder.Services.AddSingleton<StdioTransportProvider>();
builder.Services.AddSingleton(provider => new HttpTransportProvider(
    provider.GetRequiredService<ILogger<HttpTransportProvider>>(),
    config.Transport.Http));

// Configure MCP server with proper SDK setup
var mcpBuilder = builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
    {
        Name = "Rent Wizard Database MCP Server",
        Version = "1.0.0"
    };
    options.ServerInstructions = "MCP server providing database introspection tools for the rent_wizard MySQL database. Use the provided tools to explore database structure, query data, and analyze table relationships.";
    options.ProtocolVersion = "2025-06-18"; // Use latest protocol version
});

// Add HTTP transport for automatic endpoint mapping
if (config.Transport.Http.Enabled)
{
    mcpBuilder.WithHttpTransport(httpOptions =>
    {
        httpOptions.Stateless = false; // Enable stateful mode for better functionality
    });
}

// Add STDIO transport if enabled
if (config.Transport.Stdio.Enabled)
{
    mcpBuilder.WithStdioServerTransport();
}

// Add tools from assembly - this will automatically discover DatabaseTools
mcpBuilder.WithToolsFromAssembly();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.MapHealthChecks("/health");

// Map MCP endpoints using official SDK - this replaces the custom McpController
if (config.Transport.Http.Enabled)
{
    app.MapMcp(); // This automatically creates all MCP protocol endpoints
}

// Add API endpoints
app.MapGet("/", () => "MCP Database Server is running!");
app.MapGet("/status", (IServiceProvider services) =>
{
    var transportManager = services.GetRequiredService<TransportManager>();
    var enabledTransports = transportManager.GetEnabledProviders().Select(p => p.Name).ToList();

    return new
    {
        Status = "Running",
        Transports = enabledTransports,
        Environment = app.Environment.EnvironmentName,
        Timestamp = DateTime.UtcNow
    };
});

// Configure transport manager
var transportManager = app.Services.GetRequiredService<TransportManager>();
var stdioProvider = app.Services.GetRequiredService<StdioTransportProvider>();
var httpProvider = app.Services.GetRequiredService<HttpTransportProvider>();

transportManager.AddProvider(stdioProvider);
transportManager.AddProvider(httpProvider);

// Test database connection on startup
var databaseService = app.Services.GetRequiredService<DatabaseService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Starting MCP Database Server with modern architecture");
    logger.LogInformation("Enabled transports: {Transports}",
        string.Join(", ", transportManager.GetEnabledProviders().Select(p => p.Name)));

    var connectionTest = await databaseService.TestConnectionAsync();
    if (!connectionTest)
    {
        logger.LogWarning("Database connection test failed. Server will start anyway for testing MCP endpoints.");
        logger.LogWarning("Database-related tools may not function correctly until connection is established.");
    }
    else
    {
        logger.LogInformation("Database connection test successful. Starting MCP server...");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to test database connection");
    return;
}

await app.RunAsync();
