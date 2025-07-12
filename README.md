# MCP Database Server

A Model Context Protocol (MCP) server that provides seamless database operations for MySQL and Microsoft SQL Server databases. This server enables VS Code clients to interact with databases through natural language commands and provides comprehensive database management capabilities.

## 🚀 Quick Start Guide

### Prerequisites

- **.NET 8.0 SDK** or later
- **Docker & Docker Compose** (for containerized databases)
- **VS Code** with MCP client extension
- **Local Database** (MySQL or MSSQL) or Docker for containerized setup

---

## 📖 Usage Guide

### Prerequisites

- **.NET 8.0 SDK** or later
- **VS Code** with MCP client extension
- **Database Server**: MySQL or MSSQL (local, Docker, or remote)

---

## 🗄️ Database Setup Options

**The MCP server connects to any accessible database - it doesn't matter how your database is deployed:**

### Option 1: Local Database Installation

- Install MySQL or SQL Server directly on your machine
- Use standard connection strings with `localhost`

### Option 2: Docker Containers

```bash
# MySQL in Docker
docker run -d --name mysql-db -p 3306:3306 \
  -e MYSQL_ROOT_PASSWORD=your_password \
  -e MYSQL_DATABASE=your_db \
  mysql:8.0

# MSSQL in Docker
docker run -d --name mssql-db -p 1433:1433 \
  -e SA_PASSWORD=your_password \
  -e ACCEPT_EULA=Y \
  mcr.microsoft.com/mssql/server:2022-latest
```

### Option 3: Remote/Cloud Databases

- AWS RDS, Azure SQL Database, Google Cloud SQL
- Any network-accessible database server
- Update connection strings with appropriate host/port

---

## ⚙️ MCP Server Configuration

### Local Development (STDIO Transport)

**For MySQL:**

```jsonc
// .vscode/mcp.json
{
  "servers": {
    "database-server": {
      "command": "dotnet",
      "args": ["run", "--project", "MsDbServer"],
      "cwd": "${workspaceFolder}",
      "env": {
        "McpServer__Transport__Stdio__Enabled": "true",
        "McpServer__Transport__Http__Enabled": "false",
        "McpServer__Database__Provider": "mysql",
        "McpServer__Database__ConnectionString": "Server=localhost;Port=3306;Database=your_db;Uid=root;Pwd=your_password;",
        "McpServer__Logging__LogLevel": "Information"
      }
    }
  }
}
```

**For MSSQL:**

```jsonc
// .vscode/mcp.json
{
  "servers": {
    "database-server-mssql": {
      "command": "dotnet",
      "args": ["run", "--project", "MsDbServer"],
      "cwd": "${workspaceFolder}",
      "env": {
        "McpServer__Transport__Stdio__Enabled": "true",
        "McpServer__Transport__Http__Enabled": "false",
        "McpServer__Database__Provider": "mssql",
        "McpServer__Database__ConnectionString": "Server=localhost,1433;Database=your_db;User Id=sa;Password=your_password;TrustServerCertificate=true;",
        "McpServer__Logging__LogLevel": "Information"
      }
    }
  }
}
```

### Containerized MCP Server (HTTP Transport)

If you want to run the MCP server itself in a container:

```bash
# Build MCP server image
./start-mcp.sh build

# Start MCP server with HTTP transport
./start-mcp.sh http mysql    # or mssql
```

Configure VS Code for HTTP transport:

```jsonc
// .vscode/mcp.json
{
  "servers": {
    "database-server-http": {
      "command": "curl",
      "args": [
        "-X",
        "POST",
        "http://localhost:8080/mcp",
        "-H",
        "Content-Type: application/json"
      ],
      "transport": "http"
    }
  }
}
```

---

## 🛠️ Management Commands

### MCP Server Management

```bash
# Check MCP server status
./start-mcp.sh status

# Stop MCP servers
./start-mcp.sh stop
```

### Database Connection Examples

**Local MySQL:**

```
Server=localhost;Port=3306;Database=mydb;Uid=root;Pwd=password123;
```

**Docker MySQL:**

```
Server=localhost;Port=3306;Database=mydb;Uid=root;Pwd=password123;
```

**Remote MySQL:**

```
Server=remote-host.com;Port=3306;Database=mydb;Uid=user;Pwd=password123;
```

**Local MSSQL:**

```
Server=localhost,1433;Database=mydb;User Id=sa;Password=password123;TrustServerCertificate=true;
```

**Docker MSSQL:**

```
Server=localhost,1433;Database=mydb;User Id=sa;Password=password123;TrustServerCertificate=true;
```

**Remote MSSQL:**

```
Server=remote-host.com,1433;Database=mydb;User Id=sa;Password=password123;TrustServerCertificate=true;
```

---

## 🤖 AI-Powered Query Performance Analysis

### New Tools for Database Optimization

The MCP Database Server now includes advanced query analysis capabilities powered by AI to help you optimize database performance:

#### **get_query_execution_plan**

Get detailed execution plans with cost analysis and performance warnings:

```bash
# Example usage via HTTP API
curl -X POST "http://localhost:8081/mcp/tools/call" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/call",
    "params": {
      "name": "get_query_execution_plan",
      "arguments": {
        "query": "SELECT * FROM Users WHERE Email LIKE '\''%gmail%'\''"
      }
    }
  }'
```

#### **analyze_query_performance**

AI-powered analysis with optimization suggestions:

```bash
# Get AI-powered optimization suggestions
curl -X POST "http://localhost:8081/mcp/tools/call" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "analyze_query_performance",
      "arguments": {
        "query": "SELECT u.*, p.* FROM Users u LEFT JOIN Properties p ON u.UserId = p.OwnerId ORDER BY u.CreatedDate"
      }
    }
  }'
```

### **What You Get:**

- **🎯 Performance Rating**: Excellent, Good, Fair, or Poor
- **💡 Smart Suggestions**: Index recommendations and query rewrites
- **⚠️ Warning System**: Identify table scans, missing indexes, and bottlenecks
- **📊 Cost Analysis**: Understand query execution costs
- **🚀 Improvement Estimates**: Potential performance gains percentage
- **🔧 Practical Fixes**: Specific SQL suggestions you can implement

### **Demo Script**

Run the included demo to see query analysis in action:

```bash
# Make sure your MCP server is running on port 8081
./demo-query-analysis.sh
```

---

## ✨ Features

### **Database Operations**

- **Multi-Database Support**: MySQL and Microsoft SQL Server
- **Connection Testing**: Verify database connectivity
- **Schema Discovery**: List databases, tables, columns, and relationships
- **Query Execution**: Run SELECT, INSERT, UPDATE, DELETE statements
- **Data Export**: Export table data to CSV format

### **Database Analysis**

- **Table Information**: Get detailed table structure and metadata
- **Relationship Mapping**: Discover foreign key relationships
- **Data Statistics**: Row counts and basic table analytics
- **Schema Exploration**: Navigate database structure

### **🚀 Query Performance Analysis** _(NEW)_

- **Execution Plans**: Get detailed query execution plans with cost analysis
- **AI-Powered Optimization**: Intelligent suggestions for query improvements
- **Performance Rating**: Automated performance assessment (Excellent/Good/Fair/Poor)
- **Index Recommendations**: Smart index suggestions based on query patterns
- **Query Rewriting**: AI-generated suggestions for better query structure
- **Warning System**: Identify performance bottlenecks and inefficient operations
- **Cost Analysis**: Estimate query execution costs and resource usage
- **Multi-Database Support**: Works with both MySQL EXPLAIN and SQL Server execution plans

### **Transport Modes**

- **STDIO Transport**: Direct process communication for local development
- **HTTP Transport**: REST API endpoints for web-based integrations
- **Container Support**: Dockerized deployment options

### **Developer Experience**

- **VS Code Integration**: Seamless MCP client support
- **Natural Language**: Interact with databases using conversational commands
- **Error Handling**: Comprehensive error reporting and debugging
- **Logging**: Configurable logging levels for troubleshooting

### **Security & Performance**

- **Connection Pooling**: Efficient database connection management
- **Parameterized Queries**: SQL injection protection
- **Timeout Handling**: Configurable query timeouts
- **Resource Management**: Automatic connection cleanup

---

## 🏗️ Project Structure

```
MCP-DbServer/
├── MsDbServer/                          # Main MCP Server Project
│   ├── Program.cs                       # Application entry point
│   ├── Application/Services/            # Business logic layer
│   ├── Domain/                          # Domain models and interfaces
│   │   ├── Interfaces/                  # Repository contracts
│   │   └── Models/                      # Data models
│   ├── Infrastructure/                  # Data access layer
│   │   ├── Repositories/                # Database implementations
│   │   ├── Factories/                   # Repository factories
│   │   └── HealthChecks/               # Health monitoring
│   ├── Presentation/Tools/              # MCP tool definitions
│   └── Transport/                       # Communication protocols
├── docker/                              # Docker configurations
│   ├── mysql/init/                      # MySQL initialization
│   └── mssql/init/                      # MSSQL initialization
├── start-mcp.sh                        # MCP server management script
├── docker-compose.yml                   # Multi-database setup
├── docker-compose.mssql.yml            # MSSQL-specific setup
└── .vscode/mcp.json                     # VS Code MCP configuration
```

---

## 🔧 Configuration

### Environment Variables

| Variable                                | Description            | Example                           |
| --------------------------------------- | ---------------------- | --------------------------------- |
| `McpServer__Database__Provider`         | Database type          | `mysql` or `mssql`                |
| `McpServer__Database__ConnectionString` | Database connection    | See examples above                |
| `McpServer__Transport__Stdio__Enabled`  | Enable STDIO transport | `true` or `false`                 |
| `McpServer__Transport__Http__Enabled`   | Enable HTTP transport  | `true` or `false`                 |
| `McpServer__Transport__Http__Port`      | HTTP server port       | `8080`                            |
| `McpServer__Logging__LogLevel`          | Logging verbosity      | `Information`, `Debug`, `Warning` |

### Connection String Formats

**MySQL:**

```
Server=localhost;Port=3306;Database=dbname;Uid=username;Pwd=password;
```

**MSSQL:**

```
Server=localhost,1433;Database=dbname;User Id=username;Password=password;TrustServerCertificate=true;
```

---

## 📋 Requirements

### System Requirements

- .NET 8.0 SDK
- 4GB RAM minimum
- Docker 20.0+ (for containerized setup)

### Database Requirements

- **MySQL**: 8.0 or later
- **MSSQL**: SQL Server 2019 or later (including SQL Server Express)

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes
4. Test thoroughly
5. Submit a pull request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🆘 Troubleshooting

### Common Issues

**Connection Failed:**

- Verify database server is running and accessible
- Check connection string format and credentials
- Test network connectivity: `telnet host port`
- For Docker: ensure ports are exposed correctly

**MCP Server Not Starting:**

- Check .NET SDK installation
- Verify project builds: `dotnet build MsDbServer`
- Check VS Code MCP configuration
- Review log output for errors

**Docker Issues:**

- Ensure Docker daemon is running
- Check container status: `docker ps`
- Review container logs: `docker logs [container-name]`
- Verify port availability

For more detailed troubleshooting, check the [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) file.
