# Modern MCP SDK Research & Implementation Guide

## 🔍 Latest MCP SDK Features (2025-06-18)

### **📦 Official Packages**

```xml
<!-- Core MCP functionality -->
<PackageReference Include="ModelContextProtocol" Version="0.3.0-preview.2" />

<!-- HTTP/Web transport support -->
<PackageReference Include="ModelContextProtocol.AspNetCore" Version="0.3.0-preview.2" />

<!-- Low-level/client APIs (if needed) -->
<PackageReference Include="ModelContextProtocol.Core" Version="0.3.0-preview.2" />
```

### **🚀 Key SDK Features to Use**

#### **1. Multi-Transport Support**

```csharp
// Modern approach - use official SDK transports
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()    // For local/CLI clients
    .WithHttpTransport()           // For web/cloud clients
    .WithToolsFromAssembly();
```

#### **2. Structured Tool Outputs (2025-06-18)**

```csharp
return new CallToolResult
{
    Content = [new TextContentBlock { Text = text, Type = "text" }],
    StructuredContent = jsonData,          // New: JSON schema output
    Meta = metadataObject                  // New: Rich metadata
};
```

#### **3. Resource Links (2025-06-18)**

```csharp
content.Add(new ResourceLinkBlock
{
    Type = "resource_link",
    Uri = "mysql://schema/database",
    Name = "schema-export",
    Description = "Database schema",
    MimeType = "application/sql"
});
```

#### **4. Tool Annotations**

```csharp
[McpServerTool(Name = "tool_name", Title = "Human-Friendly Name")]
[Description("Detailed description for LLMs")]
public static async Task<CallToolResult> ToolMethod(
    [Description("Parameter description")] string param)
```

### **🏗️ Modern Architecture Patterns**

#### **1. ASP.NET Core Integration**

```csharp
// Use WebApplicationBuilder for HTTP support
var builder = WebApplication.CreateBuilder(args);

// Add MCP with multiple transports
builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();
app.MapMcp();  // Map HTTP endpoints
```

#### **2. Dependency Injection Integration**

```csharp
// Register services with DI
builder.Services.AddSingleton<IDatabaseService, MySqlDatabaseService>();
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

// Tools automatically get DI services injected
[McpServerTool]
public static async Task<CallToolResult> MyTool(
    IDatabaseService database,  // Injected automatically
    ILogger<DatabaseTools> logger)  // Injected automatically
```

#### **3. Configuration Integration**

```csharp
// Options pattern for configuration
public class McpServerOptions
{
    public string Name { get; set; } = "MsDbServer";
    public string Version { get; set; } = "2.0.0";
    public TransportOptions Transport { get; set; } = new();
}

builder.Services.Configure<McpServerOptions>(
    builder.Configuration.GetSection("McpServer"));
```

---

## 🎯 Implementation Strategy

### **✅ Use Official SDK Features**

- **Transport Management**: Use `WithStdioServerTransport()` and `WithHttpTransport()`
- **Tool Registration**: Use `WithToolsFromAssembly()`
- **DI Integration**: Use built-in service injection
- **Configuration**: Use ASP.NET Core configuration system

### **✅ Modern Patterns**

- **WebApplicationBuilder**: For HTTP and cloud support
- **Options Pattern**: For configuration management
- **ILogger**: For structured logging
- **Health Checks**: For monitoring
- **CORS**: For web client support

### **❌ Avoid Custom Implementations**

- **Don't**: Create custom transport protocols
- **Don't**: Implement custom MCP message handling
- **Don't**: Build custom configuration systems
- **Don't**: Create custom logging abstractions

---

## 📋 SDK Usage Checklist

### **Transport Layer**

- [ ] Use `ModelContextProtocol.AspNetCore` for HTTP
- [ ] Use `WithStdioServerTransport()` for STDIO
- [ ] Use `WithHttpTransport()` for HTTP
- [ ] Use `app.MapMcp()` for endpoint mapping

### **Tool Layer**

- [ ] Use `[McpServerTool]` attributes
- [ ] Use `CallToolResult` for responses
- [ ] Use `StructuredContent` for JSON outputs
- [ ] Use `ResourceLinkBlock` for resource links

### **Configuration Layer**

- [ ] Use `WebApplicationBuilder`
- [ ] Use `IConfiguration` and Options pattern
- [ ] Use `builder.Services` for DI registration
- [ ] Use `appsettings.json` for environment config

### **Logging Layer**

- [ ] Use `ILogger<T>` throughout
- [ ] Use structured logging with scopes
- [ ] Use log levels appropriately
- [ ] Use ASP.NET Core logging configuration

---

## 🔧 Modern Best Practices

### **1. Error Handling**

```csharp
try
{
    var result = await operation();
    return new CallToolResult
    {
        Content = [new TextContentBlock { Text = result, Type = "text" }]
    };
}
catch (Exception ex)
{
    logger.LogError(ex, "Operation failed");
    return new CallToolResult
    {
        IsError = true,
        Content = [new TextContentBlock { Text = $"Error: {ex.Message}", Type = "text" }]
    };
}
```

### **2. Async Patterns**

```csharp
// Always use async/await for I/O operations
public static async Task<CallToolResult> DatabaseTool(
    IDatabaseService database)
{
    var result = await database.GetDataAsync();  // Async all the way
    return new CallToolResult { /* ... */ };
}
```

### **3. Cancellation Support**

```csharp
public static async Task<CallToolResult> LongRunningTool(
    IDatabaseService database,
    CancellationToken cancellationToken = default)
{
    var result = await database.GetDataAsync(cancellationToken);
    return new CallToolResult { /* ... */ };
}
```

---

_Reference this document when implementing MCP features to ensure we use official SDK capabilities instead of custom implementations._
