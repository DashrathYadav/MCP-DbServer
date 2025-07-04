using MsDbServer.Transport;

namespace MsDbServer.Configuration;

/// <summary>
/// Configuration options for the MCP Database Server
/// </summary>
public class McpServerOptions
{
    public const string SectionName = "McpServer";

    /// <summary>
    /// Database connection settings
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Transport configuration
    /// </summary>
    public TransportOptions Transport { get; set; } = new();

    /// <summary>
    /// Logging configuration
    /// </summary>
    public LoggingOptions Logging { get; set; } = new();
}

/// <summary>
/// Database configuration options
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Database provider (mysql, postgresql, sqlserver)
    /// </summary>
    public string Provider { get; set; } = "mysql";

    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Maximum connection pool size
    /// </summary>
    public int MaxPoolSize { get; set; } = 20;

    /// <summary>
    /// Command timeout in seconds
    /// </summary>
    public int CommandTimeout { get; set; } = 30;

    /// <summary>
    /// Enable retry on failure
    /// </summary>
    public bool EnableRetryOnFailure { get; set; } = true;

    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Enable performance monitoring
    /// </summary>
    public bool EnablePerformanceMonitoring { get; set; } = true;

    /// <summary>
    /// Enable query logging (for debugging)
    /// </summary>
    public bool EnableQueryLogging { get; set; } = false;
}

/// <summary>
/// Transport configuration options
/// </summary>
public class TransportOptions
{
    /// <summary>
    /// STDIO transport configuration
    /// </summary>
    public StdioTransportOptions Stdio { get; set; } = new();

    /// <summary>
    /// HTTP transport configuration
    /// </summary>
    public HttpTransportOptions Http { get; set; } = new();
}

/// <summary>
/// STDIO transport configuration
/// </summary>
public class StdioTransportOptions
{
    /// <summary>
    /// Enable STDIO transport
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Logging configuration options
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Log level (Trace, Debug, Information, Warning, Error, Critical)
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>
    /// Enable structured logging
    /// </summary>
    public bool EnableStructuredLogging { get; set; } = true;

    /// <summary>
    /// Enable console logging
    /// </summary>
    public bool EnableConsoleLogging { get; set; } = true;

    /// <summary>
    /// Enable file logging
    /// </summary>
    public bool EnableFileLogging { get; set; } = false;

    /// <summary>
    /// Log file path (when file logging is enabled)
    /// </summary>
    public string LogFilePath { get; set; } = "logs/mcp-server.log";
}
