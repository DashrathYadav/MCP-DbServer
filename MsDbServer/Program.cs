using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using MsDbServer;
using System.ComponentModel;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr for MCP compliance
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Add database service for dependency injection
builder.Services.AddSingleton<IDatabaseService, MySqlDatabaseService>();

// Configure MCP server with tools
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var host = builder.Build();

// Test database connection on startup
var databaseService = host.Services.GetRequiredService<IDatabaseService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
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
