# MsDbServer - MySQL Database MCP Server

A powerful Model Context Protocol (MCP) server that provides MySQL database introspection capabilities for AI assistants like GitHub Copilot. Built with .NET 8 and the official Microsoft MCP SDK.

## 🎯 What is this?

**MsDbServer** is an MCP (Model Context Protocol) server that acts as a bridge between AI assistants and MySQL databases. It allows you to ask natural language questions about your database structure and get instant, detailed responses.

### Example Usage with GitHub Copilot:

- 🗣️ "List all tables in my database"
- 🗣️ "Show me the structure of the users table"
- 🗣️ "What are the foreign key relationships?"
- 🗣️ "Execute this query: SELECT COUNT(\*) FROM orders"

## ✨ Features

### 🛠️ 6 Powerful Database Tools

| Tool                        | Description                                       | Example                            |
| --------------------------- | ------------------------------------------------- | ---------------------------------- |
| **`ListTables`**            | Get all tables in database                        | "Show me all tables"               |
| **`DescribeTable`**         | Detailed table schema with columns, keys, indexes | "Describe the users table"         |
| **`ExecuteQuery`**          | Run SELECT queries safely (with limits)           | "Query the first 10 users"         |
| **`GetDatabaseStats`**      | Database size, table counts, statistics           | "Get database statistics"          |
| **`GetSchemaInfo`**         | Multiple table schemas with pattern matching      | "Show tables starting with 'user'" |
| **`GetTableRelationships`** | Foreign key dependencies and relationships        | "Show table relationships"         |

### 🔒 Safety Features

- ✅ **Query Restrictions** - Only SELECT and WITH statements allowed
- ✅ **Row Limits** - Maximum 1000 rows per query
- ✅ **Timeout Protection** - 30-second query timeout
- ✅ **Input Validation** - All inputs sanitized and validated

### 🏗️ Technical Features

- ✅ **Built with Microsoft MCP SDK** - Official ModelContextProtocol package
- ✅ **MySQL Support** - Full MySQL database introspection
- ✅ **Async Operations** - Non-blocking database operations
- ✅ **Rich Formatting** - Beautiful, readable output
- ✅ **Error Handling** - Comprehensive error messages
- ✅ **VS Code Integration** - Works seamlessly with GitHub Copilot

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- MySQL database (accessible)
- [Visual Studio Code](https://code.visualstudio.com/) with [GitHub Copilot](https://github.com/features/copilot)

### 1. Clone and Setup

```bash
git clone https://github.com/your-username/MsDbServer.git
cd MsDbServer
```

### 2. Configure Database Connection

Edit `MsDbServer/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=your_database;user=your_user;password=your_password"
  }
}
```

### 3. Test the Server

```bash
cd MsDbServer
dotnet build
dotnet run
```

### 4. Setup VS Code Integration

Create `.vscode/mcp.json` in your project:

```json
{
  "servers": {
    "MsDbServer": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/MsDbServer/MsDbServer.csproj"],
      "cwd": "${workspaceFolder}",
      "env": {
        "ConnectionStrings__DefaultConnection": "server=localhost;port=3306;database=your_database;user=your_user;password=your_password"
      }
    }
  }
}
```

## 📋 Detailed Setup Instructions

### Step 1: Install Prerequisites

1. **Install .NET 8 SDK**

   ```bash
   # Windows (using winget)
   winget install Microsoft.DotNet.SDK.8

   # macOS (using brew)
   brew install dotnet

   # Or download from: https://dotnet.microsoft.com/download/dotnet/8.0
   ```

2. **Install Visual Studio Code**

   - Download from: https://code.visualstudio.com/
   - Install GitHub Copilot extension
   - Install GitHub Copilot Chat extension

3. **Ensure MySQL Access**
   - Have a MySQL database running
   - Know the connection details (host, port, database, username, password)

### Step 2: Project Setup

1. **Clone the Repository**

   ```bash
   git clone https://github.com/your-username/MsDbServer.git
   cd MsDbServer
   ```

2. **Configure Database Connection**

   **Option A: Edit appsettings.json**

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "server=your_host;port=3306;database=your_db;user=your_user;password=your_password"
     }
   }
   ```

   **Option B: Use Environment Variables**

   ```bash
   # Windows
   set ConnectionStrings__DefaultConnection=server=localhost;port=3306;database=your_db;user=your_user;password=your_password

   # Linux/macOS
   export ConnectionStrings__DefaultConnection="server=localhost;port=3306;database=your_db;user=your_user;password=your_password"
   ```

3. **Test the Connection**

   ```bash
   cd MsDbServer
   dotnet build
   dotnet run
   ```

   You should see:

   ```
   info: Database connection test successful. Starting MCP server...
   ```

### Step 3: VS Code Integration

1. **Create MCP Configuration**

   Create `.vscode/mcp.json` in your workspace:

   ```json
   {
     "servers": {
       "MsDbServer": {
         "command": "dotnet",
         "args": ["run", "--project", "MsDbServer/MsDbServer.csproj"],
         "cwd": "${workspaceFolder}",
         "env": {
           "ConnectionStrings__DefaultConnection": "server=localhost;port=3306;database=your_database;user=your_user;password=your_password"
         }
       }
     }
   }
   ```

2. **Open Workspace in VS Code**

   ```bash
   code .
   ```

3. **Test with GitHub Copilot**
   - Open GitHub Copilot Chat (Ctrl+Shift+I)
   - Try: "List all tables in the database"
   - Try: "Describe the users table"

## 💻 Usage Examples

### In GitHub Copilot Chat:

```
🗣️ List all tables in the database
📋 Response: Shows all table names

🗣️ Describe the users table structure
📋 Response: Detailed schema with columns, types, constraints

🗣️ Show me the first 5 records from the orders table
📋 Response: Formatted table output

🗣️ What are the foreign key relationships in my database?
📋 Response: Visual relationship mapping

🗣️ Get database statistics and table sizes
📋 Response: Database overview with sizes and counts
```

### Manual Testing (JSON-RPC):

```bash
# List all tables
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"ListTables","arguments":{}}}' | dotnet run

# Describe a table
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"DescribeTable","arguments":{"tableName":"users"}}}' | dotnet run
```

## 📁 Project Structure

```
MsDbServer/
├── .vscode/
│   └── mcp.json              # VS Code MCP configuration
├── MsDbServer/
│   ├── Program.cs            # Application entry point
│   ├── DatabaseTools.cs     # 6 MCP tools implementation
│   ├── IDatabaseService.cs  # Service interface & data models
│   ├── MySqlDatabaseService.cs # MySQL implementation
│   ├── appsettings.json     # Configuration
│   └── MsDbServer.csproj    # Project file
└── README.md                # This file
```

## 🛠️ Development

### Building

```bash
cd MsDbServer
dotnet build
```

### Running

```bash
cd MsDbServer
dotnet run
```

### Testing Tools

```bash
# Test ListTables
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"ListTables","arguments":{}}}' | dotnet run

# Test DescribeTable
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"DescribeTable","arguments":{"tableName":"your_table"}}}' | dotnet run
```

## 🔧 Troubleshooting

### Common Issues

1. **"Database connection test failed"**

   - Check your connection string in `appsettings.json`
   - Verify MySQL server is running
   - Confirm database exists and credentials are correct

2. **"GitHub Copilot doesn't see the MCP server"**

   - Ensure `.vscode/mcp.json` exists in your workspace
   - Restart VS Code after creating/editing MCP configuration
   - Check that GitHub Copilot and Copilot Chat extensions are installed and active

3. **"Build failed"**

   - Ensure .NET 8 SDK is installed: `dotnet --version`
   - Try: `dotnet restore` then `dotnet build`

4. **"Permission denied" errors**
   - Check file permissions
   - On Linux/macOS, you might need: `chmod +x` on script files

### Debug Mode

```bash
# Run with detailed logging
dotnet run --configuration Debug
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature-name`
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with the [Microsoft MCP SDK](https://github.com/microsoft/model-context-protocol)
- Uses [MySql.Data](https://www.nuget.org/packages/MySql.Data/) for MySQL connectivity
- Powered by [.NET 8](https://dotnet.microsoft.com/)

---

**🚀 Ready to explore your database with AI? Clone, configure, and start asking questions!**

- **Dependency Injection** - Proper service registration and lifetime management
- **Async/Await** - All database operations are asynchronous
- **Comprehensive Error Handling** - Proper error responses with meaningful messages
- **Structured Logging** - Configurable logging with Microsoft.Extensions.Logging

### 🗄️ Database Support

- **MySQL** - Full support for MySQL database introspection
- **Connection String Configuration** - Configurable via appsettings.json
- **Safe Data Type Handling** - Handles MySQL-specific data types and large values

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- MySQL database (configured and accessible)
- Visual Studio Code with GitHub Copilot (for testing)

### Installation

1. Clone or download the project
2. Configure your database connection in `appsettings.json`
3. Build and run the server

```bash
dotnet build
dotnet run
```

### Configuration

Update `appsettings.json` with your MySQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=your_database;user=your_user;password=your_password"
  }
}
```

### VS Code Integration

Create or update `.vscode/mcp.json` in your workspace:

```json
{
  "inputs": [],
  "servers": {
    "MsDbServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/MsDbServer.csproj"]
    }
  }
}
```

## Usage Examples

### Using with GitHub Copilot

Once configured, you can use the tools in GitHub Copilot:

- "List all tables in the database"
- "Describe the structure of the users table"
- "Show me the schema for the orders table"
- "Get database statistics and table sizes"
- "Execute a query to show the first 10 users"
- "Show me all tables that start with 'user'"
- "What are the foreign key relationships for the orders table?"

### Direct JSON-RPC Testing

```bash
# List all tables
echo '{"jsonrpc":"2.0","id":1,"method":"tools/call","params":{"name":"ListTables","arguments":{}}}' | dotnet run

# Describe a specific table
echo '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"DescribeTable","arguments":{"tableName":"users"}}}' | dotnet run

# Execute a query
echo '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"ExecuteQuery","arguments":{"query":"SELECT * FROM users LIMIT 5","maxRows":5}}}' | dotnet run

# Get database statistics
echo '{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"GetDatabaseStats","arguments":{}}}' | dotnet run

# Get schema for tables starting with 'user'
echo '{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"GetSchemaInfo","arguments":{"tablePattern":"user%"}}}' | dotnet run

# Get relationships for a specific table
echo '{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"GetTableRelationships","arguments":{"tableName":"orders"}}}' | dotnet run
```

### PowerShell Testing

Use the included test script:

```powershell
.\test-mcp.ps1
```

## Sample Output

### ListTables Response

```
Database Tables:
================
  addresses
  orders
  products
  users
```

### DescribeTable Response

```
Table: default.users

Columns:
--------
  UserId bigint(19,0) NOT NULL IDENTITY PRIMARY KEY
  Username varchar(50) NOT NULL
  Email varchar(100) NULL
  CreatedDate datetime NOT NULL
    Default: CURRENT_TIMESTAMP

Primary Keys:
-------------
  UserId

Foreign Keys:
-------------
  (none)

Indexes:
--------
  IX_Users_Email: Email
  IX_Users_Username: Username
```

### ExecuteQuery Response

```
Query: SELECT UserId, Username, Email FROM users LIMIT 3
Execution Time: 12.45ms
Rows: 3

-----------------------------------------------------------------------
UserId | Username     | Email
-----------------------------------------------------------------------
1      | john_doe     | john.doe@example.com
2      | jane_smith   | jane.smith@example.com
3      | bob_wilson   | bob.wilson@example.com
-----------------------------------------------------------------------
```

### GetDatabaseStats Response

```
Database Statistics: rent_wizard
================================
Database Version: 8.0.35
Generated At: 2025-06-27 10:30:15 UTC

Overview:
---------
Total Tables: 6
Database Size: 2.4MB

Table Statistics:
-----------------
Table Name               Rows   Data Size Index Size  Columns Indexes
----------------------------------------------------------------------
users                    1,250     125.6KB    45.2KB        8       3
orders                     847      98.3KB    32.1KB       12       4
products                   156      23.4KB    12.8KB        9       2
addresses                  892      67.8KB    28.3KB        7       2
owners                      45       8.2KB     3.1KB        6       1
properties                 234      45.6KB    18.7KB       15       5
```

### GetTableRelationships Response

```
Table Relationships (8 found)
=========================

Parent Table: users
Children:
  orders.UserId -> users.UserId
    Constraint: FK_orders_users
  addresses.UserId -> users.UserId
    Constraint: FK_addresses_users

Parent Table: products
Children:
  order_items.ProductId -> products.ProductId
    Constraint: FK_order_items_products

Dependency Summary:
------------------
orders depends on: users
addresses depends on: users
order_items depends on: products, orders
```

## Project Structure

```
MsDbServer/
├── Program.cs                  # Application entry point and MCP server setup
├── IDatabaseService.cs         # Database service interface and models
├── MySqlDatabaseService.cs     # MySQL database implementation
├── DatabaseTools.cs           # MCP tools with [McpServerTool] attributes
├── appsettings.json           # Configuration file
└── MsDbServer.csproj          # Project file with dependencies
```

## Dependencies

- **ModelContextProtocol** (0.3.0-preview.1) - Microsoft MCP SDK
- **Microsoft.Extensions.Hosting** (9.0.6) - Hosting infrastructure
- **MySql.Data** (9.3.0) - MySQL connectivity

## Migration from Manual Implementation

This project replaces a previous manual JSON-RPC implementation with the official Microsoft MCP SDK, providing:

- **Simplified Development** - Attributes-based tool registration
- **Better Integration** - Official SDK support and updates
- **Reduced Boilerplate** - Automatic JSON-RPC handling
- **Future-Proof** - Follows Microsoft's recommended patterns

## License

This project is built for educational and development purposes.

---

_Built with ❤️ using .NET 8 and the Microsoft MCP SDK_
