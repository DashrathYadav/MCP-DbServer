# MCP Database Server - Technical Documentation

This document provides detailed technical information about the MCP Database Server architecture, design patterns, implementation details, and code-level documentation.

## 🏗️ Architecture Overview

### Clean Architecture Pattern

The project follows Clean Architecture principles with clear separation of concerns:

```
┌─────────────────────┐
│   Presentation      │ ← MCP Tools, HTTP Controllers
├─────────────────────┤
│   Application       │ ← Business Logic, Services
├─────────────────────┤
│   Domain            │ ← Entities, Interfaces, Models
├─────────────────────┤
│   Infrastructure    │ ← Data Access, External Services
└─────────────────────┘
```

### Layer Responsibilities

- **Presentation Layer**: MCP tool definitions, HTTP endpoints, transport protocols
- **Application Layer**: Business logic, orchestration, validation
- **Domain Layer**: Core business entities, interfaces, domain models
- **Infrastructure Layer**: Database repositories, external service integrations

---

## 📁 Detailed Project Structure

### Core Application (`MsDbServer/`)

```
MsDbServer/
├── Program.cs                           # Application bootstrap and DI configuration
├── Application/
│   └── Services/
│       └── DatabaseService.cs          # Main business logic orchestrator
├── Domain/
│   ├── Interfaces/
│   │   └── IDatabaseRepository.cs      # Repository contract
│   └── Models/
│       └── DatabaseModels.cs           # Domain entities and DTOs
├── Infrastructure/
│   ├── Factories/
│   │   └── DatabaseRepositoryFactory.cs # Repository factory pattern
│   ├── HealthChecks/
│   │   └── DatabaseHealthCheck.cs      # Health monitoring
│   └── Repositories/
│       ├── MySqlDatabaseRepository.cs   # MySQL data access implementation
│       └── SqlServerDatabaseRepository.cs # SQL Server data access implementation
├── Presentation/
│   └── Tools/
│       └── DatabaseTools.cs            # MCP tool definitions
└── Transport/
    ├── HttpTransportProvider.cs        # HTTP transport implementation
    ├── StdioTransportProvider.cs       # STDIO transport implementation
    ├── TransportManager.cs             # Transport orchestration
    └── ITransportProvider.cs           # Transport contract
```

### Configuration Files

```
├── docker-compose.yml                   # Multi-database Docker setup
├── docker-compose.mssql.yml            # MSSQL-specific configuration
├── Dockerfile                          # Container build instructions
├── MCP-DbServer.sln                    # Solution file
└── .vscode/
    └── mcp.json                         # VS Code MCP client configuration
```

### Database Initialization

```
docker/
├── mysql/
│   └── init/
│       └── 01-init-sample-data.sql     # MySQL sample data
└── mssql/
    └── init/
        ├── init-db.sh                  # MSSQL initialization script
        └── 01-init-sample-data.sql     # MSSQL sample data
```

### Management Scripts

```
└── start-mcp.sh                       # MCP server management
```

---

## 🔧 Core Components

### 1. Domain Models (`Domain/Models/DatabaseModels.cs`)

#### Primary Entities

```csharp
// Database connection configuration
public class DatabaseConnection
{
    public string ConnectionString { get; init; }
    public string Provider { get; init; }
    public int MaxPoolSize { get; init; } = 20;
    public int CommandTimeout { get; init; } = 30;
    public bool EnableRetryOnFailure { get; init; } = true;
}

// Query execution result
public class QueryResult
{
    public bool Success { get; init; }
    public string[]? ColumnNames { get; init; }
    public object[][]? Rows { get; init; }
    public int RowCount { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
}

// Database schema information
public class SchemaInfo
{
    public string Name { get; init; }
    public string Type { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, object> Properties { get; init; }
}

// Table structure information
public class TableInfo
{
    public string Name { get; init; }
    public string Schema { get; init; }
    public List<ColumnInfo> Columns { get; init; }
    public List<string> PrimaryKeys { get; init; }
    public List<ForeignKeyInfo> ForeignKeys { get; init; }
    public long RowCount { get; init; }
}
```

### 2. Repository Pattern (`Domain/Interfaces/IDatabaseRepository.cs`)

#### Core Interface

```csharp
public interface IDatabaseRepository
{
    // Connection management
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    // Query operations
    Task<QueryResult> ExecuteQueryAsync(string query, CancellationToken cancellationToken = default);
    Task<int> ExecuteNonQueryAsync(string command, CancellationToken cancellationToken = default);

    // Schema discovery
    Task<IEnumerable<SchemaInfo>> GetSchemasAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<TableInfo>> GetTablesAsync(string? schema = null, CancellationToken cancellationToken = default);
    Task<TableInfo?> GetTableInfoAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default);

    // Relationship discovery
    Task<IEnumerable<ColumnInfo>> GetColumnsAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<ForeignKeyInfo>> GetForeignKeysAsync(string tableName, string? schema = null, CancellationToken cancellationToken = default);

    // Data operations
    Task<string> ExportTableToCsvAsync(string tableName, string? schema = null, int limit = 1000, CancellationToken cancellationToken = default);

    // Analysis
    Task<DatabaseAnalysis> AnalyzeDatabaseAsync(bool includeStats = false, CancellationToken cancellationToken = default);
}
```

### 3. Database Implementations

#### MySQL Repository (`Infrastructure/Repositories/MySqlDatabaseRepository.cs`)

```csharp
public class MySqlDatabaseRepository : IDatabaseRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlDatabaseRepository> _logger;

    // Key features:
    // - MySQL-specific SQL syntax
    // - INFORMATION_SCHEMA queries
    // - Connection pooling with MySqlConnection
    // - Parameterized query support
    // - Comprehensive error handling
}
```

#### SQL Server Repository (`Infrastructure/Repositories/SqlServerDatabaseRepository.cs`)

```csharp
public class SqlServerDatabaseRepository : IDatabaseRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqlServerDatabaseRepository> _logger;

    // Key features:
    // - T-SQL syntax optimization
    // - sys.* system view queries
    // - SqlConnection with connection pooling
    // - Advanced schema analysis
    // - Performance optimizations
}
```

### 4. Factory Pattern (`Infrastructure/Factories/DatabaseRepositoryFactory.cs`)

```csharp
public class DatabaseRepositoryFactory
{
    public IDatabaseRepository CreateRepository(string provider, string connectionString)
    {
        return provider.ToLowerInvariant() switch
        {
            "mysql" => new MySqlDatabaseRepository(connectionString, logger),
            "mssql" or "sqlserver" => new SqlServerDatabaseRepository(connectionString, logger),
            _ => throw new NotSupportedException($"Database provider '{provider}' is not supported")
        };
    }
}
```

---

## 🚀 Transport Layer

### STDIO Transport (`Transport/StdioTransportProvider.cs`)

```csharp
public class StdioTransportProvider : ITransportProvider
{
    // Features:
    // - Direct process communication
    // - JSON-RPC 2.0 protocol
    // - Streaming support
    // - Error propagation
    // - Cancellation token support
}
```

### HTTP Transport (`Transport/HttpTransportProvider.cs`)

```csharp
public class HttpTransportProvider : ITransportProvider
{
    // Features:
    // - RESTful API endpoints
    // - CORS support
    // - Request/response logging
    // - Health check endpoints
    // - Swagger documentation
}
```

### Transport Manager (`Transport/TransportManager.cs`)

```csharp
public class TransportManager
{
    // Responsibilities:
    // - Transport protocol selection
    // - Request routing
    // - Response formatting
    // - Error handling
    // - Logging coordination
}
```

---

## 🛠️ MCP Tools (`Presentation/Tools/DatabaseTools.cs`)

### Available Tools

```csharp
[McpTool("test_connection")]
public async Task<object> TestConnection()

[McpTool("list_schemas")]
public async Task<object> ListSchemas()

[McpTool("list_tables")]
public async Task<object> ListTables(string? schemaName = null)

[McpTool("describe_table")]
public async Task<object> DescribeTable(string tableName, string? schemaName = null)

[McpTool("execute_query")]
public async Task<object> ExecuteQuery(string query, int maxRows = 100)

[McpTool("export_csv")]
public async Task<object> ExportCsv(string tableName, string? schemaName = null, int limit = 1000)

[McpTool("analyze_database")]
public async Task<object> AnalyzeDatabase(bool includeStats = false)
```

---

## ⚙️ Configuration System

### Dependency Injection (`Program.cs`)

```csharp
// Service registration
builder.Services.Configure<McpServerOptions>(builder.Configuration.GetSection("McpServer"));
builder.Services.AddSingleton<DatabaseRepositoryFactory>();
builder.Services.AddScoped<IDatabaseRepository>(provider =>
{
    var factory = provider.GetRequiredService<DatabaseRepositoryFactory>();
    var options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
    return factory.CreateRepository(options.Database.Provider, options.Database.ConnectionString);
});

// Transport configuration
builder.Services.AddSingleton<ITransportProvider, StdioTransportProvider>();
builder.Services.AddSingleton<ITransportProvider, HttpTransportProvider>();
builder.Services.AddSingleton<TransportManager>();
```

### Configuration Options

```csharp
public class McpServerOptions
{
    public DatabaseOptions Database { get; set; } = new();
    public TransportOptions Transport { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
}

public class DatabaseOptions
{
    public string Provider { get; set; } = "mysql";
    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeout { get; set; } = 30;
    public int MaxPoolSize { get; set; } = 20;
}

public class TransportOptions
{
    public StdioTransportOptions Stdio { get; set; } = new();
    public HttpTransportOptions Http { get; set; } = new();
}
```

---

## 🔍 Database Schema Analysis

### Sample Database Schema (MSSQL `app_db`)

```sql
-- Rental Management System Schema

-- Core entities
CREATE TABLE [Addresses] (
    [AddressId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Street] NVARCHAR(MAX) NOT NULL,
    [City] NVARCHAR(MAX) NOT NULL,
    [State] NVARCHAR(MAX) NOT NULL,
    [Country] NVARCHAR(MAX) NOT NULL,
    [ZipCode] NVARCHAR(MAX) NOT NULL,
    -- Audit fields
    [CreatedBy] BIGINT NOT NULL,
    [LastModifiedBy] BIGINT NOT NULL,
    [CreationDate] DATETIME2 NOT NULL,
    [LastModificationDate] DATETIME2 NULL
);

-- Property owners
CREATE TABLE [Owners] (
    [OwnerId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [LoginId] NVARCHAR(MAX) NOT NULL,
    [FullName] NVARCHAR(MAX) NOT NULL,
    [Email] NVARCHAR(MAX) NOT NULL,
    [MobileNumber] NVARCHAR(MAX) NOT NULL,
    [AddressId] BIGINT NOT NULL,
    FOREIGN KEY ([AddressId]) REFERENCES [Addresses]([AddressId])
);

-- Rental payment history (newly added)
CREATE TABLE [RentHistory] (
    [RentHistoryId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [RentId] BIGINT NOT NULL,
    [TenantId] BIGINT NOT NULL,
    [PropertyId] BIGINT NOT NULL,
    [PaymentDate] DATETIME2 NOT NULL,
    [AmountDue] DECIMAL(18,2) NOT NULL,
    [AmountPaid] DECIMAL(18,2) NOT NULL,
    [PaymentMethod] NVARCHAR(50) NOT NULL,
    [PaymentStatus] NVARCHAR(20) NOT NULL,
    -- Foreign key relationships
    FOREIGN KEY ([RentId]) REFERENCES [Rents]([RentId]),
    FOREIGN KEY ([TenantId]) REFERENCES [Tenants]([TenantId]),
    FOREIGN KEY ([PropertyId]) REFERENCES [Properties]([PropertyId])
);
```

---

## 🧪 Testing Strategy

### Unit Tests Structure

```
Tests/
├── Unit/
│   ├── Domain/
│   │   └── Models/
│   ├── Infrastructure/
│   │   └── Repositories/
│   └── Presentation/
│       └── Tools/
├── Integration/
│   ├── Database/
│   └── Transport/
└── EndToEnd/
    └── MCP/
```

### Test Categories

1. **Unit Tests**: Individual component testing
2. **Integration Tests**: Database connectivity and operations
3. **End-to-End Tests**: Full MCP workflow testing

---

## 📊 Performance Considerations

### Database Optimization

- **Connection Pooling**: Reuse database connections
- **Parameterized Queries**: Prevent SQL injection and improve performance
- **Query Timeouts**: Configurable command timeouts
- **Resource Cleanup**: Automatic connection disposal

### Memory Management

- **Streaming**: Large result sets use streaming
- **Cancellation**: Proper cancellation token support
- **Disposal**: IDisposable pattern implementation
- **Garbage Collection**: Minimize object allocation

---

## 🔒 Security Implementation

### SQL Injection Prevention

```csharp
// Parameterized queries example
using var command = new SqlCommand("SELECT * FROM Users WHERE Id = @userId", connection);
command.Parameters.AddWithValue("@userId", userId);
```

### Connection Security

- **TLS Encryption**: Secure database connections
- **Credential Management**: Environment-based configuration
- **Access Control**: Role-based permissions
- **Audit Logging**: Comprehensive operation logging

---

## 🚀 Deployment Patterns

### Local Development

```bash
# Direct .NET execution
dotnet run --project MsDbServer

# Environment-based configuration
export McpServer__Database__Provider="mysql"
export McpServer__Database__ConnectionString="Server=localhost;..."
```

### Containerized Deployment

```dockerfile
# Multi-stage build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "MsDbServer.dll"]
```

### Orchestration

```yaml
# docker-compose.yml
services:
  mcp-server:
    build: .
    environment:
      - McpServer__Database__Provider=mssql
      - McpServer__Database__ConnectionString=Server=mssql-db,1433;...
    depends_on:
      - mssql-db

  mssql-db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
```

---

## 🔄 Development Workflow

### Code Standards

- **C# Conventions**: Microsoft C# coding standards
- **Clean Code**: SOLID principles
- **Documentation**: XML documentation comments
- **Testing**: TDD/BDD approach

### Git Workflow

```bash
# Feature development
git checkout -b feature/new-database-provider
git commit -m "feat: add PostgreSQL support"
git push origin feature/new-database-provider
```

### Release Process

1. **Version Bump**: Update assembly version
2. **Changelog**: Document changes
3. **Testing**: Full test suite execution
4. **Build**: Create release artifacts
5. **Deploy**: Container registry push

---

## 🔧 Extending the System

### Adding New Database Providers

1. **Implement Repository**:

   ```csharp
   public class PostgreSqlDatabaseRepository : IDatabaseRepository
   {
       // Implement all interface methods
   }
   ```

2. **Update Factory**:

   ```csharp
   return provider.ToLowerInvariant() switch
   {
       "mysql" => new MySqlDatabaseRepository(connectionString, logger),
       "mssql" => new SqlServerDatabaseRepository(connectionString, logger),
       "postgresql" => new PostgreSqlDatabaseRepository(connectionString, logger), // New
       _ => throw new NotSupportedException($"Provider '{provider}' not supported")
   };
   ```

3. **Add Configuration**:
   ```json
   {
     "McpServer": {
       "Database": {
         "Provider": "postgresql",
         "ConnectionString": "Host=localhost;Database=mydb;Username=user;Password=pass"
       }
     }
   }
   ```

### Adding New MCP Tools

```csharp
[McpTool("backup_database")]
public async Task<object> BackupDatabase(string backupPath)
{
    // Implementation
    return new { success = true, path = backupPath };
}
```

---

## 📈 Monitoring and Observability

### Logging

```csharp
// Structured logging
_logger.LogInformation("Executing query {Query} for user {UserId}",
    query, userId);

// Performance logging
using var activity = Activity.StartActivity("DatabaseQuery");
activity?.SetTag("query.table", tableName);
```

### Health Checks

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isConnected = await _repository.TestConnectionAsync(cancellationToken);
            return isConnected
                ? HealthCheckResult.Healthy("Database connection successful")
                : HealthCheckResult.Unhealthy("Database connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}
```

### Metrics

- **Query Execution Time**
- **Connection Pool Usage**
- **Error Rates**
- **Request Throughput**

---

This technical documentation provides comprehensive coverage of the MCP Database Server implementation, architecture decisions, and extension points for future development.
