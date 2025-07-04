# Modern MCP Database Server - 2025-06-18 Specification

## Overview

This MySQL MCP (Model Context Protocol) server implements the latest **2025-06-18 MCP specification** with a modern clean architecture approach. It provides secure, efficient database introspection capabilities for LLMs with structured outputs, resource links, and multi-transport support.

## 🌟 **Key Features**

### ✅ **MCP 2025-06-18 Compliance**
- **ModelContextProtocol SDK v0.3.0-preview.2** - Latest C# SDK
- **Structured Tool Output** - All tools return structured JSON content
- **Resource Links** - Tools can return links to database resources
- **Tool Title Fields** - Human-friendly display names for tools
- **Comprehensive Metadata** - Rich tool metadata with annotations
- **Modern Error Handling** - Structured error responses with `CallToolResult`

### ✅ **Clean Architecture Implementation**
- **Layered Design** - Transport, Presentation, Application, Domain, Infrastructure
- **Dependency Injection** - Full DI container integration
- **Repository Pattern** - Database abstraction layer
- **Options Pattern** - Configurable service settings
- **Multi-Transport Support** - STDIO and HTTP transports

### ✅ **Advanced Database Features**
- **Connection Pooling** - Optimized MySQL connection management
- **Async/Await Patterns** - Non-blocking database operations
- **Structured Logging** - Comprehensive logging with correlation IDs
- **Performance Monitoring** - Query execution timing and metrics
- **Security Hardening** - Parameter validation and secure connections

### ✅ **Multi-Transport Support**
- **STDIO Transport** - Standard MCP transport for client integration
- **HTTP Transport** - RESTful API with health checks and Swagger
- **Extensible Design** - Easy to add WebSocket, gRPC, or custom transports

## Available Tools

### 1. **test_connection** - Database Connection Test

- **Description**: Test database connection with structured output and resource links
- **Features**:
  - Connection validation
  - Database statistics
  - Resource links to schema and stats
  - Structured JSON output
- **Returns**: Connection status, database info, and resource links

### 2. **describe_table** - Table Structure Analysis

- **Description**: Get detailed table structure including columns, keys, and constraints
- **Features**:
  - Column information (types, constraints, defaults)
  - Primary and foreign keys
  - Index information
  - Structured metadata
- **Parameters**: `tableName` (required), `schemaName` (optional)

### 3. **list_tables** - Database Table Listing

- **Description**: Get all tables in the database
- **Features**:
  - Sorted table list
  - Table count metadata
  - Clean formatted output
- **Returns**: List of all database tables

### 4. **execute_query** - SQL Query Execution

- **Description**: Execute SQL queries with result limits and timeout protection
- **Features**:
  - Query result limiting (default 100 rows)
  - Execution time tracking
  - Structured result format
  - SQL injection protection
- **Parameters**: `query` (required), `maxRows` (optional, default 100)

### 5. **get_database_stats** - Database Statistics

- **Description**: Comprehensive database statistics with structured output
- **Features**:
  - Table counts and sizes
  - Database version information
  - Performance metrics
  - Structured JSON metadata
- **Returns**: Complete database statistics

### 6. **get_schema_info** - Schema Information

- **Description**: Get detailed schema information for tables
- **Features**:
  - Pattern matching support (% wildcards)
  - Complete table structures
  - Relationship information
  - Batch processing for performance
- **Parameters**: `tablePattern` (optional, supports wildcards)

### 7. **get_table_relationships** - Relationship Analysis

- **Description**: Analyze foreign key relationships between tables
- **Features**:
  - Parent-child relationships
  - Constraint information
  - Dependency mapping
  - Structured relationship data
- **Parameters**: `tableName` (optional, filter by table)

## Modern Architecture

### Database Service Layer

```csharp
// Modern connection pooling and async patterns
public class MySqlDatabaseService : IDatabaseService, IDisposable
{
    // Singleton connection with semaphore for thread safety
    private readonly SemaphoreSlim _connectionSemaphore;
    // Structured logging with scopes
    private readonly ILogger<MySqlDatabaseService> _logger;
    // Options pattern for configuration
    private readonly McpDatabaseServiceOptions _options;
}
```

### Tool Implementation

```csharp
// Modern tool with structured output and resource links
[McpServerTool(Name = "test_connection", Title = "Test Database Connection")]
public static async Task<CallToolResult> TestConnection(IDatabaseService databaseService)
{
    // Structured content with JSON schema
    var structuredData = JsonSerializer.SerializeToNode(new { ... });

    // Resource links for additional context
    content.Add(new ResourceLinkBlock
    {
        Type = "resource_link",
        Uri = $"mysql://schema/{databaseName}",
        Name = $"{databaseName}-schema",
        Description = "Complete database schema",
        MimeType = "application/sql"
    });

    return new CallToolResult
    {
        Content = content,
        StructuredContent = structuredData,
        Meta = metadata
    };
}
```

## Configuration

### Quick Setup

1. **Copy the example configuration:**

   ```powershell
   cd MCP-DbServer\MsDbServer
   copy appsettings.json.example appsettings.json
   ```

2. **Update your database connection:**
   Edit `appsettings.json` with your MySQL database details:

### Database Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=your_db;user=root;password=your_password"
  }
}
```

### Service Options

```csharp
public class McpDatabaseServiceOptions
{
    public int MaxPoolSize { get; set; } = 20;
    public int MinPoolSize { get; set; } = 2;
    public int ConnectionTimeoutSeconds { get; set; } = 30;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnablePerformanceMonitoring { get; set; } = true;
    public bool EnableQueryLogging { get; set; } = false;
}
```

## Security Features

### ✅ **Connection Security**

- SSL/TLS encryption preferred
- Parameter validation
- SQL injection protection
- Connection timeout limits

### ✅ **Resource Management**

- Proper connection disposal
- Semaphore-based thread safety
- Memory-efficient result processing
- Graceful error handling

### ✅ **Query Safety**

- Parameterized queries
- Result size limits
- Execution timeouts
- Input validation

## Performance Features

### ✅ **Optimized Operations**

- Connection pooling (min 2, max 20 connections)
- Parallel database operations
- Batch processing for large datasets
- Efficient memory usage

### ✅ **Monitoring**

- Execution time tracking
- Performance metrics
- Structured logging
- Resource usage monitoring

## Usage Example

### Running the Server

```powershell
cd MCP-DbServer\MsDbServer
dotnet run
```

### MCP Client Integration

```typescript
// Example client usage
const client = await McpClientFactory.CreateAsync(transport);

// Test connection with structured output
const result = await client.CallToolAsync("test_connection", {});
console.log(result.structuredContent); // JSON schema data
console.log(result.content[0].text); // Human-readable text

// Get table structure
const tableInfo = await client.CallToolAsync("describe_table", {
  tableName: "users",
});
```

## Technology Stack

- **.NET 8.0** - Latest .NET runtime
- **ModelContextProtocol v0.3.0-preview.2** - Latest MCP SDK
- **MySql.Data v9.3.0** - Latest MySQL connector
- **Microsoft.Extensions.Hosting v9.0.6** - Modern hosting model
- **Structured Logging** - Comprehensive logging framework
- **Dependency Injection** - Full DI container support

## Key Improvements Made

### 🔄 **From Legacy to Modern**

1. **Updated NuGet Packages** - Latest MCP SDK and dependencies
2. **Refactored Database Service** - Modern async patterns and connection pooling
3. **Enhanced Tools** - CallToolResult with structured output
4. **Added Resource Links** - 2025-06-18 specification feature
5. **Improved Error Handling** - Structured error responses
6. **Security Hardening** - SSL connections and parameter validation
7. **Performance Optimization** - Parallel processing and connection pooling

### 🆕 **New Features Added**

- **Structured Tool Output** - JSON schemas for all tool responses
- **Resource Links** - Links to database resources and schemas
- **Tool Metadata** - Rich metadata with structured information
- **Connection Testing** - Comprehensive connection validation
- **Performance Monitoring** - Execution time and resource tracking
- **Modern Configuration** - Options pattern and dependency injection

## Development Notes

This implementation represents a fully modernized MCP server that:

- Complies with the latest MCP 2025-06-18 specification
- Uses modern C# patterns and best practices
- Provides structured, LLM-friendly outputs
- Implements comprehensive security measures
- Delivers optimal performance for production use

The server is ready for production deployment and can serve as a reference implementation for other MCP servers using the latest specification features.
