---
layout: mcp-default
title: AI Assistant Quickstart
description: Quick reference guide for AI assistants building MCP servers
breadcrumbs:
  - title: Home
    url: /
  - title: Getting Started
    url: /getting-started/ai-quickstart/
  - title: AI Quickstart
    url: /getting-started/ai-quickstart/
toc: true
---

# AI Assistant Quickstart

> **For AI Assistants (GitHub Copilot, Claude, ChatGPT, etc.):**  
> This page provides everything you need to help users build MCP servers with **Mcp.Gateway.Tools**.

## Quick Facts

- **Library:** `Mcp.Gateway.Tools` (NuGet package)
- **Framework:** .NET 10+
- **Protocol:** MCP 2025-11-25 (Model Context Protocol)
- **Transports:** HTTP, WebSocket, SSE, stdio
- **GitHub:** https://github.com/eyjolfurgudnivatne/mcp.gateway
- **Docs:** https://eyjolfurgudnivatne.github.io/mcp.gateway/

---

## Minimal Server Template

**File:** `Program.cs`

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Detect stdio mode (for GitHub Copilot, Claude Desktop)
var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Register MCP Gateway
builder.AddToolsService();

var app = builder.Build();

// stdio mode
if (isStdioMode)
{
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MyMcpServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// HTTP/WebSocket mode
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();
```

---

## Tool Definition Patterns

### Pattern 1: Typed Tool (Auto-Schema) ✅ RECOMMENDED

**Benefits:** Type-safe, auto-generated JSON Schema, IntelliSense

```csharp
using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Text.Json.Serialization;

public class MyTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and returns result")]
    public JsonRpcMessage Add(TypedJsonRpc<AddRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException("Parameters required");
        
        var result = args.Number1 + args.Number2;
        
        return ToolResponse.Success(request.Id, new { result });
    }
}

public sealed record AddRequest(
    [property: JsonPropertyName("number1")]
    [property: Description("First number")] double Number1,
    [property: JsonPropertyName("number2")]
    [property: Description("Second number")] double Number2);
```

### Pattern 2: Manual Schema

**Use when:** Custom validation (minLength, pattern, etc.) or complex schemas

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("validate_email",
        Description = "Validates email format",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""email"":{
                    ""type"":""string"",
                    ""description"":""Email address to validate"",
                    ""pattern"":""^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$""
                }
            },
            ""required"":[""email""]
        }")]
    public JsonRpcMessage ValidateEmail(JsonRpcMessage request)
    {
        var email = request.GetParams().GetProperty("email").GetString();
        var isValid = email?.Contains("@") == true;
        
        return ToolResponse.Success(request.Id, new { isValid });
    }
}
```

---

## Common Tool Examples

### 1. Calculator

```csharp
[McpTool("add", Description = "Add two numbers")]
public JsonRpcMessage Add(TypedJsonRpc<MathParams> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, new { result = args.A + args.B });
}

[McpTool("multiply", Description = "Multiply two numbers")]
public JsonRpcMessage Multiply(TypedJsonRpc<MathParams> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, new { result = args.A * args.B });
}

public sealed record MathParams(double A, double B);
```

### 2. Date/Time

```csharp
[McpTool("get_current_time", Description = "Get current UTC time")]
public JsonRpcMessage GetTime(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new
    {
        timestamp = DateTime.UtcNow.ToString("o"),
        date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        time = DateTime.UtcNow.ToString("HH:mm:ss")
    });
}

[McpTool("add_days", Description = "Add days to a date")]
public JsonRpcMessage AddDays(TypedJsonRpc<DateParams> request)
{
    var args = request.GetParams()!;
    var date = DateTime.Parse(args.Date);
    var result = date.AddDays(args.Days);
    
    return ToolResponse.Success(request.Id, new
    {
        result = result.ToString("yyyy-MM-dd")
    });
}

public sealed record DateParams(string Date, int Days);
```

### 3. File Operations

```csharp
[McpTool("read_file", Description = "Read file contents")]
public JsonRpcMessage ReadFile(TypedJsonRpc<FileParams> request)
{
    var args = request.GetParams()!;
    
    if (!File.Exists(args.Path))
    {
        throw new ToolInvalidParamsException($"File not found: {args.Path}");
    }
    
    var content = File.ReadAllText(args.Path);
    return ToolResponse.Success(request.Id, new { content });
}

[McpTool("write_file", Description = "Write content to file")]
public JsonRpcMessage WriteFile(TypedJsonRpc<WriteParams> request)
{
    var args = request.GetParams()!;
    
    File.WriteAllText(args.Path, args.Content);
    return ToolResponse.Success(request.Id, new { written = true });
}

public sealed record FileParams(string Path);
public sealed record WriteParams(string Path, string Content);
```

### 4. HTTP Requests

```csharp
private readonly IHttpClientFactory _httpClientFactory;

[McpTool("fetch_url", Description = "Fetch content from URL")]
public async Task<JsonRpcMessage> FetchUrl(TypedJsonRpc<UrlParams> request)
{
    var args = request.GetParams()!;
    
    using var client = _httpClientFactory.CreateClient();
    var response = await client.GetStringAsync(args.Url);
    
    return ToolResponse.Success(request.Id, new { content = response });
}

public sealed record UrlParams(string Url);
```

---

## Error Handling

### Validation Errors

```csharp
if (string.IsNullOrWhiteSpace(args.Name))
{
    throw new ToolInvalidParamsException("Name cannot be empty");
}

if (args.Age < 0 || args.Age > 150)
{
    throw new ToolInvalidParamsException("Age must be between 0 and 150");
}

if (args.Divisor == 0)
{
    throw new ToolInvalidParamsException("Cannot divide by zero");
}
```

### Automatic JSON-RPC Error Response

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": {
      "detail": "Cannot divide by zero"
    }
  },
  "id": "1"
}
```

---

## Dependency Injection

### Constructor Injection

```csharp
public class MyTools
{
    private readonly ILogger<MyTools> _logger;
    private readonly IMyService _service;
    
    public MyTools(ILogger<MyTools> logger, IMyService service)
    {
        _logger = logger;
        _service = service;
    }
    
    [McpTool("my_tool")]
    public async Task<JsonRpcMessage> MyTool(JsonRpcMessage request)
    {
        _logger.LogInformation("Tool invoked");
        var result = await _service.DoSomethingAsync();
        return ToolResponse.Success(request.Id, result);
    }
}

// Register in Program.cs
builder.Services.AddScoped<MyTools>();
builder.Services.AddSingleton<IMyService, MyService>();
```

### Method Parameter Injection ✅ SIMPLER

```csharp
public class MyTools
{
    [McpTool("my_tool")]
    public async Task<JsonRpcMessage> MyTool(
        JsonRpcMessage request,
        ILogger<MyTools> logger,  // ← Auto-injected!
        IMyService service)        // ← Auto-injected!
    {
        logger.LogInformation("Tool invoked");
        var result = await service.DoSomethingAsync();
        return ToolResponse.Success(request.Id, result);
    }
}

// Only register services (class auto-discovered)
builder.Services.AddSingleton<IMyService, MyService>();
```

---

## Project Structure

```
MyMcpServer/
├── Program.cs              # Server setup
├── Tools/
│   ├── CalculatorTools.cs  # Calculator tools
│   ├── FileTools.cs        # File operations
│   └── DateTimeTools.cs    # Date/time tools
├── Models/
│   └── ToolModels.cs       # Request/response records
└── MyMcpServer.csproj      # Project file
```

---

## Key Attributes

| Attribute | Purpose | Required |
|-----------|---------|----------|
| `[McpTool("name")]` | Define a tool | Yes |
| `[Description("...")]` | Document parameters | No |
| `[JsonPropertyName("...")]` | JSON field name | No |
| `[McpPrompt("name")]` | Define a prompt template | No |
| `[McpResource("uri")]` | Define a resource | No |

---

## Key Classes & Methods

| Class/Method | Purpose |
|--------------|---------|
| `JsonRpcMessage` | Request/response wrapper |
| `TypedJsonRpc<T>` | Typed request wrapper with auto-schema |
| `ToolResponse.Success(id, result)` | Create success response |
| `ToolInvalidParamsException` | Parameter validation error |
| `request.GetParams()` | Extract raw parameters (JsonElement) |
| `request.GetParams<T>()` | Deserialize to typed model |

---

## Testing

### Start Server

```bash
# HTTP mode (default)
dotnet run

# stdio mode (for GitHub Copilot)
dotnet run -- --stdio
```

### Test with curl

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "add_numbers",
      "arguments": {"number1": 5, "number2": 3}
    },
    "id": 1
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"result\":8}"
      }
    ]
  },
  "id": 1
}
```

---

## GitHub Copilot Integration

### Configuration File: `.mcp.json`

```json
{
  "mcpServers": {
    "my_server": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\MyMcpServer",
        "--",
        "--stdio"
      ]
    }
  }
}
```

### Usage in Copilot Chat

```
@my_server add 5 and 3
@my_server what's the current time?
@my_server read file config.json
```

---

## Complete Examples

Full working examples available at:

| Example | Description | Path |
|---------|-------------|------|
| **Calculator** | Basic arithmetic (add, multiply, divide) | `/examples/calculator/` |
| **DateTime** | Date/time operations with timezones | `/examples/datetime/` |
| **Prompt** | Prompt templates (Santa Claus example) | `/examples/prompt/` |
| **Resource** | File, system, database resources | `/examples/resource/` |
| **Metrics** | Lifecycle hooks with metrics endpoint | `/examples/metrics/` |
| **Authorization** | Role-based access control | `/examples/authorization/` |
| **Pagination** | Cursor-based pagination (120+ tools) | `/examples/pagination/` |

---

## Common Patterns

### Async Tools

```csharp
[McpTool("async_tool")]
public async Task<JsonRpcMessage> AsyncTool(JsonRpcMessage request)
{
    await Task.Delay(100);
    return ToolResponse.Success(request.Id, new { done = true });
}
```

### Optional Parameters

```csharp
public sealed record SearchParams(
    string Query,           // Required (non-nullable)
    int? Limit = null,      // Optional (nullable with default)
    string? Sort = "asc");  // Optional with default value
```

### Enum Parameters

```csharp
public enum Priority { Low, Medium, High }

public sealed record TaskParams(
    string Title,
    Priority Priority = Priority.Medium);
```

### Multiple Return Values

```csharp
return ToolResponse.Success(request.Id, new
{
    result = 42,
    timestamp = DateTime.UtcNow,
    status = "success",
    metadata = new { version = "1.0" }
});
```

---

## MCP Protocol Methods

**Auto-handled by Mcp.Gateway.Tools:**

| Method | Purpose | Auto-implemented |
|--------|---------|------------------|
| `initialize` | Protocol handshake | ✅ Yes |
| `tools/list` | List all tools | ✅ Yes |
| `tools/call` | Invoke a tool | ✅ Yes |
| `prompts/list` | List prompts | ✅ Yes (if prompts defined) |
| `prompts/get` | Get prompt | ✅ Yes (if prompts defined) |
| `resources/list` | List resources | ✅ Yes (if resources defined) |
| `resources/read` | Read resource | ✅ Yes (if resources defined) |

**You only implement:**
- Tool methods (marked with `[McpTool]`)
- Prompt methods (marked with `[McpPrompt]`)
- Resource methods (marked with `[McpResource]`)

---

## Quick Reference

### Import Namespaces

```csharp
using Mcp.Gateway.Tools;
using System.ComponentModel;
using System.Text.Json.Serialization;
```

### Tool Naming Convention

- **Pattern:** `^[a-zA-Z0-9_-]{1,128}$`
- **Use:** Underscores (`_`) or hyphens (`-`)
- **Avoid:** Dots (`.`) - breaks GitHub Copilot

```csharp
✅ GOOD: "add_numbers", "get-time", "fetch_data"
❌ BAD:  "add.numbers", "get.time"
```

### Response Helpers

```csharp
// Success
ToolResponse.Success(requestId, resultObject)

// Success with text + structured content
ToolResponse.SuccessWithStructured(requestId, textContent, structuredContent)

// Error (throw exception - auto-converted to JSON-RPC error)
throw new ToolInvalidParamsException("Error message")
```

---

## Advanced Features

### Lifecycle Hooks (v1.8.0+)

Monitor tool invocations:

```csharp
// Program.cs
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();

// Expose metrics
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    return Results.Json(metricsHook?.GetMetrics());
});
```

### Notifications

Send real-time updates:

```csharp
public class MyTools
{
    private readonly INotificationSender _notificationSender;
    
    [McpTool("reload_config")]
    public async Task<JsonRpcMessage> ReloadConfig(JsonRpcMessage request)
    {
        // Reload config...
        
        // Notify all connected clients
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("file://config.json"));
        
        return ToolResponse.Success(request.Id, new { reloaded = true });
    }
}
```

### Resource Subscriptions (v1.8.0+)

Clients can subscribe to specific resources:

```csharp
[McpResource("system://metrics",
    Name = "System Metrics",
    MimeType = "application/json")]
public JsonRpcMessage SystemMetrics(JsonRpcMessage request)
{
    var metrics = new { cpu = GetCpu(), memory = GetMemory() };
    var json = JsonSerializer.Serialize(metrics);
    
    return ToolResponse.Success(request.Id, new ResourceContent(
        Uri: "system://metrics",
        MimeType: "application/json",
        Text: json
    ));
}
```

---

## Links

- **Getting Started:** [/mcp.gateway/getting-started/index/](/mcp.gateway/getting-started/index/)
- **Tools API:** [/mcp.gateway/api/tools/](/mcp.gateway/api/tools/)
- **Examples:** [/mcp.gateway/examples/calculator/](/mcp.gateway/examples/calculator/)
- **GitHub:** https://github.com/eyjolfurgudnivatne/mcp.gateway
- **NuGet:** https://www.nuget.org/packages/Mcp.Gateway.Tools/

---

## Installation

```bash
# Create new project
dotnet new web -n MyMcpServer
cd MyMcpServer

# Add package
dotnet add package Mcp.Gateway.Tools

# Run
dotnet run
```

---

## Summary for AI Assistants

**When user asks: "Build me an MCP server with X"**

1. ✅ Create `Program.cs` (use minimal template above)
2. ✅ Create `Tools/XTools.cs` (use Pattern 1: Typed Tool)
3. ✅ Create models with `[Description]` attributes
4. ✅ Use `TypedJsonRpc<T>` for type-safety
5. ✅ Throw `ToolInvalidParamsException` for errors
6. ✅ Return `ToolResponse.Success(id, result)`

**Key principles:**
- ✅ Type-safe > manual JSON
- ✅ Auto-schema > manual InputSchema
- ✅ Simple records > complex classes
- ✅ Clear error messages
- ✅ Follow examples from `/examples/`

**No hallucinations needed - everything is in the examples!** 🎯
