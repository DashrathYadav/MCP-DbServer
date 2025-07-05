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

## 🐳 **Docker Deployment**

### **Quick Start with Docker**

The MCP Database Server is now **Docker-ready** with support for multiple deployment scenarios:

#### **1. Single MySQL Instance**

```bash
# Deploy MySQL MCP Server instance
./scripts/deploy.ps1 mysql

# Or using Docker Compose directly
docker-compose up -d mcp-mysql mysql-db
```

Access the server at: `http://localhost:8080`

#### **2. Development Mode (STDIO)**

```bash
# Deploy development instance with STDIO transport
./scripts/deploy.ps1 dev

# Or using Docker Compose directly
docker-compose --profile development up -d mcp-dev mysql-db
```

#### **3. Full Stack with Load Balancer**

```bash
# Deploy full stack with Nginx load balancer
./scripts/deploy.ps1 full

# Or using Docker Compose directly
docker-compose --profile loadbalancer up -d
```

Access through load balancer at: `http://localhost`

### **Docker Architecture**

```
┌─────────────────────────────────────────────────────────────┐
│                    Docker Environment                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ MCP Server  │  │ MCP Server  │  │    Nginx    │         │
│  │   MySQL     │  │ PostgreSQL  │  │    Load     │         │
│  │ Port: 8080  │  │ Port: 8081  │  │  Balancer   │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│         │                 │                 │              │
│  ┌─────────────┐  ┌─────────────┐           │              │
│  │   MySQL     │  │ PostgreSQL  │           │              │
│  │  Database   │  │  Database   │           │              │
│  │ Port: 3306  │  │ Port: 5432  │           │              │
│  └─────────────┘  └─────────────┘           │              │
│                                              │              │
│  ┌─────────────────────────────────────────────────────────┤
│  │              Docker Network: mcp-network                │
│  └─────────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────────┘
```

### **Available Deployment Profiles**

| Profile        | Description           | Ports      | Use Case                  |
| -------------- | --------------------- | ---------- | ------------------------- |
| `mysql`        | MySQL MCP Server      | 8080, 3306 | Production MySQL          |
| `postgresql`   | PostgreSQL MCP Server | 8081, 5432 | Production PostgreSQL     |
| `development`  | STDIO Development     | 3306       | Development/Testing       |
| `loadbalancer` | Full Stack + Nginx    | 80, 443    | Multi-instance Production |

### **Environment Variables**

Each deployment scenario uses specific environment variables:

```bash
# MySQL Instance
McpServer__Database__Provider=mysql
McpServer__Database__ConnectionString=server=mysql-db;port=3306;database=app_db;user=mcpuser;password=mcppass123
McpServer__Transport__Http__Enabled=true
McpServer__Transport__Http__Port=8080

# PostgreSQL Instance (Future)
McpServer__Database__Provider=postgresql
McpServer__Database__ConnectionString=Host=postgres-db;Port=5432;Database=app_db;Username=mcpuser;Password=mcppass123
McpServer__Transport__Http__Enabled=true
McpServer__Transport__Http__Port=8081

# Development Instance
McpServer__Database__Provider=mysql
McpServer__Transport__Stdio__Enabled=true
McpServer__Transport__Http__Enabled=false
McpServer__Logging__LogLevel=Debug
```

## 📖 **User Manual**

### **Installation & Setup**

#### **Prerequisites**

- **Docker** and **Docker Compose** installed
- **PowerShell** (for deployment scripts on Windows)
- **Bash** (for deployment scripts on Linux/Mac)

#### **Step-by-Step Installation**

1. **Clone the Repository**

   ```bash
   git clone <repository-url>
   cd MCP-DbServer
   ```

2. **Choose Your Deployment Scenario**

   **Option A: MySQL Production Instance**

   ```bash
   ./scripts/deploy.ps1 mysql
   ```

   **Option B: Development with STDIO**

   ```bash
   ./scripts/deploy.ps1 dev
   ```

   **Option C: Full Stack with Load Balancer**

   ```bash
   ./scripts/deploy.ps1 full
   ```

3. **Verify Deployment**

   ```bash
   # Check service status
   ./scripts/deploy.ps1 status

   # Check health endpoints
   curl http://localhost:8080/health  # MySQL instance
   curl http://localhost/health       # Load balancer
   ```

### **Using the MCP Database Server**

#### **HTTP API Endpoints**

The server provides RESTful endpoints for MCP operations:

##### **Health Check**

```bash
GET /health
```

Returns server health status and database connectivity.

##### **MCP Tool Listing**

```bash
POST /mcp/tools/list
Content-Type: application/json

{}
```

##### **MCP Tool Execution**

```bash
POST /mcp/tools/call
Content-Type: application/json

{
  "name": "test_connection",
  "arguments": {}
}
```

##### **Example Tool Calls**

**Test Database Connection**

```bash
curl -X POST http://localhost:8080/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "test_connection",
    "arguments": {}
  }'
```

**List All Tables**

```bash
curl -X POST http://localhost:8080/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "list_tables",
    "arguments": {}
  }'
```

**Execute SQL Query**

```bash
curl -X POST http://localhost:8080/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "execute_query",
    "arguments": {
      "query": "SELECT * FROM users LIMIT 10"
    }
  }'
```

**Describe Table Structure**

```bash
curl -X POST http://localhost:8080/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{
    "name": "describe_table",
    "arguments": {
      "tableName": "users"
    }
  }'
```

#### **STDIO Transport Usage**

For development and direct client integration:

```bash
# Start development instance
./scripts/deploy.ps1 dev

# Connect to container
docker exec -it mcp-server-dev /bin/bash

# Server will respond to MCP protocol messages via STDIO
```

### **Configuration Management**

#### **Database Configuration**

Update database settings through environment variables:

```bash
# MySQL Configuration
McpServer__Database__Provider=mysql
McpServer__Database__ConnectionString=server=mysql-db;port=3306;database=app_db;user=mcpuser;password=mcppass123
McpServer__Database__MaxPoolSize=20
McpServer__Database__EnablePerformanceMonitoring=true

# PostgreSQL Configuration (Future)
McpServer__Database__Provider=postgresql
McpServer__Database__ConnectionString=Host=postgres-db;Port=5432;Database=app_db;Username=mcpuser;Password=mcppass123
```

#### **Transport Configuration**

Configure transport protocols:

```bash
# Enable HTTP transport
McpServer__Transport__Http__Enabled=true
McpServer__Transport__Http__Port=8080

# Enable STDIO transport
McpServer__Transport__Stdio__Enabled=true

# Logging configuration
McpServer__Logging__LogLevel=Information
```

### **Monitoring & Troubleshooting**

#### **Monitoring Commands**

```bash
# Check service status
./scripts/deploy.ps1 status

# View logs
./scripts/deploy.ps1 logs

# View specific service logs
docker-compose logs -f mcp-mysql
docker-compose logs -f mysql-db
```

#### **Health Checks**

```bash
# Server health
curl http://localhost:8080/health

# Database connectivity
curl -X POST http://localhost:8080/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{"name": "test_connection", "arguments": {}}'
```

#### **Common Issues**

**Issue: Container fails to start**

```bash
# Check logs
docker-compose logs mcp-mysql

# Common causes:
# - Database connection string incorrect
# - Database not ready (wait for health check)
# - Port conflicts (change port in docker-compose.yml)
```

**Issue: Database connection failed**

```bash
# Check database status
docker-compose logs mysql-db

# Verify connectivity
docker exec -it mcp-mysql-db mysql -u mcpuser -p

# Check network connectivity
docker exec -it mcp-server-mysql ping mysql-db
```

### **Scaling & Multi-Instance Deployment**

#### **Horizontal Scaling**

```bash
# Scale MySQL instances
docker-compose up -d --scale mcp-mysql=3

# Use load balancer
./scripts/deploy.ps1 full
```

#### **Multi-Database Deployment**

```bash
# Deploy MySQL and PostgreSQL instances
docker-compose up -d mcp-mysql mysql-db
docker-compose --profile postgresql up -d mcp-postgresql postgres-db
```

### **Production Deployment Checklist**

- [ ] **Configure secure connection strings**
- [ ] **Set up SSL/TLS certificates**
- [ ] **Configure firewall rules**
- [ ] **Set up monitoring and logging**
- [ ] **Configure backup strategies**
- [ ] **Test health check endpoints**
- [ ] **Configure resource limits**
- [ ] **Set up log rotation**
- [ ] **Test disaster recovery procedures**

### **Development Workflow**

#### **Local Development**

```bash
# Start development environment
./scripts/deploy.ps1 dev

# Make changes to code
# ...

# Rebuild and restart
./scripts/deploy.ps1 dev -Rebuild
```

#### **Testing**

```bash
# Test all endpoints
curl http://localhost:8080/health
curl -X POST http://localhost:8080/mcp/tools/list
curl -X POST http://localhost:8080/mcp/tools/call -d '{"name":"test_connection","arguments":{}}'
```

### **Cleanup & Maintenance**

#### **Stopping Services**

```bash
# Stop all services
./scripts/deploy.ps1 stop

# Stop specific profile
docker-compose --profile development down
```

#### **Cleaning Up**

```bash
# Remove containers and volumes
./scripts/deploy.ps1 clean

# Remove unused images
docker system prune -a
```

#### **Updating**

```bash
# Pull latest changes
git pull origin main

# Rebuild and restart
./scripts/deploy.ps1 mysql -Rebuild
```

## Technology Stack

### **Core Technologies**

- **.NET 8.0** - Latest .NET runtime
- **ModelContextProtocol v0.3.0-preview.2** - Latest MCP SDK
- **MySql.Data v9.3.0** - Latest MySQL connector
- **Microsoft.Extensions.Hosting v9.0.6** - Modern hosting model
- **Structured Logging** - Comprehensive logging framework
- **Dependency Injection** - Full DI container support

### **Container Technologies**

- **Docker** - Multi-stage containerization
- **Docker Compose** - Multi-service orchestration
- **Nginx** - Load balancing and reverse proxy
- **MySQL 8.0** - Database container
- **PostgreSQL 15** - Database container (future)

### **Architecture Patterns**

- **Clean Architecture** - Layered separation of concerns
- **Repository Pattern** - Database abstraction
- **Factory Pattern** - Database provider creation
- **Options Pattern** - Configuration management
- **Multi-Transport** - STDIO and HTTP support

## Key Improvements Made

### 🔄 **From Legacy to Modern**

1. **Updated NuGet Packages** - Latest MCP SDK and dependencies
2. **Refactored Database Service** - Modern async patterns and connection pooling
3. **Enhanced Tools** - CallToolResult with structured output
4. **Added Resource Links** - 2025-06-18 specification feature
5. **Improved Error Handling** - Structured error responses
6. **Security Hardening** - SSL connections and parameter validation
7. **Performance Optimization** - Parallel processing and connection pooling
8. **Clean Architecture** - Layered separation of concerns
9. **Multi-Transport Support** - STDIO and HTTP protocols
10. **Docker Containerization** - Production-ready deployment

### 🆕 **New Features Added**

- **Structured Tool Output** - JSON schemas for all tool responses
- **Resource Links** - Links to database resources and schemas
- **Tool Metadata** - Rich metadata with structured information
- **Connection Testing** - Comprehensive connection validation
- **Performance Monitoring** - Execution time and resource tracking
- **Modern Configuration** - Options pattern and dependency injection
- **Multi-Instance Deployment** - Docker-based scaling
- **Load Balancing** - Nginx-based request distribution
- **Health Checks** - Container and application health monitoring
- **Environment Profiles** - Development, staging, and production configurations

### 🐳 **Docker & DevOps Features**

- **Multi-Stage Builds** - Optimized container images
- **Security Hardening** - Non-root user, minimal attack surface
- **Health Checks** - Container and application health monitoring
- **Service Discovery** - Docker network-based service communication
- **Environment Management** - Profile-based deployments
- **Scaling Support** - Horizontal and vertical scaling capabilities
- **Monitoring Integration** - Structured logging and metrics
- **Deployment Scripts** - Automated deployment and management

## Development Notes

This implementation represents a **production-ready, Docker-enabled** MCP server that:

- **Complies with MCP 2025-06-18 specification** - Latest features and best practices
- **Uses modern C# patterns** - Clean architecture, dependency injection, async/await
- **Provides structured, LLM-friendly outputs** - JSON schemas and resource links
- **Implements comprehensive security** - SSL, parameter validation, non-root containers
- **Delivers optimal performance** - Connection pooling, parallel processing, caching
- **Supports multi-instance deployment** - Docker Compose, load balancing, scaling
- **Enables easy configuration** - Environment variables, profiles, health checks
- **Includes monitoring and observability** - Structured logging, health endpoints, metrics

### **Deployment Scenarios**

The server supports multiple deployment scenarios:

1. **Development** - STDIO transport for direct client integration
2. **Single Instance** - HTTP transport for API access
3. **Multi-Instance** - Load-balanced deployment with multiple databases
4. **Production** - Full stack with monitoring, security, and scaling

### **Future Enhancements**

- **Kubernetes Orchestration** - K8s manifests and Helm charts
- **PostgreSQL Support** - Multi-database provider implementation
- **Advanced Monitoring** - Prometheus, Grafana, distributed tracing
- **Security Enhancements** - OAuth, JWT, API keys
- **Performance Optimization** - Redis caching, query optimization
- **CI/CD Integration** - GitHub Actions, automated testing

The server is **ready for production deployment** and can serve as a **reference implementation** for modern MCP servers using the latest specification features with Docker containerization.

---

## 🚀 **Quick Start Summary**

```bash
# 1. Clone and navigate to project
git clone <repository-url>
cd MCP-DbServer

# 2. Deploy MySQL instance
./scripts/deploy.ps1 mysql

# 3. Test the deployment
curl http://localhost:8080/health
curl -X POST http://localhost:8080/mcp/tools/list

# 4. Use the MCP tools
curl -X POST http://localhost:8080/mcp/tools/call \
  -H "Content-Type: application/json" \
  -d '{"name": "test_connection", "arguments": {}}'
```

**Server is now running at** `http://localhost:8080` 🎉
