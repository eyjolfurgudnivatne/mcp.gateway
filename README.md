# 🚀 Mcp.Gateway.Tools

> Build MCP servers in .NET 10 – production-ready in minutes

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![Tests](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml/badge.svg)](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--11--25-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**Mcp.Gateway.Tools** is a .NET library for building Model Context Protocol (MCP) servers. It makes it easy to expose C# code as tools that can be discovered and invoked by clients like **GitHub Copilot** and **Claude Desktop**.

The main product in this repo is `Mcp.Gateway.Tools`.  
`DevTestServer` and `Mcp.Gateway.Tests` are used for development and verification only – they are not part of the product.

---

## ⚡ Getting started

### 1. Install the package

```
dotnet new web -n MyMcpServer
cd MyMcpServer
dotnet add package Mcp.Gateway.Tools
```

### 2. Configure the server (`Program.cs`)

Minimal HTTP + WebSocket server (v1.7.0 with MCP 2025-11-25 support):

```
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register ToolService + ToolInvoker + Session Management (v1.7.0)
builder.AddToolsService();

var app = builder.Build();

// stdio mode for GitHub Copilot (optional)
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// WebSockets for streaming
app.UseWebSockets();

// MCP 2025-11-25 Streamable HTTP (v1.7.0 - RECOMMENDED)
app.UseProtocolVersionValidation();  // Protocol version validation
app.MapStreamableHttpEndpoint("/mcp");  // Unified endpoint (POST + GET + DELETE)

// Legacy endpoints (still work, deprecated)
app.MapHttpRpcEndpoint("/rpc");  // HTTP POST only (deprecated)
app.MapWsRpcEndpoint("/ws");     // WebSocket (keep for binary streaming)
app.MapSseRpcEndpoint("/sse");   // SSE only (deprecated, use /mcp GET instead)

app.Run();
```

See `DevTestServer/Program.cs` for a more complete setup with health endpoint and stdio logging.

### 3. Create your first tool

#### 3.1. Simplest Tool (Auto-generated Schema)

The easiest way to create a tool using **strongly-typed parameters** and **automatic schema generation**:

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("greet")]
    public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
    {
        var name = request.Params.Name;
        return ToolResponse.Success(
            request.Id,
            new { message = $"Hello, {name}!" });
    }
}

public record GreetParams(string Name);
```

**Benefits:**
- ✅ **No manual JSON Schema** - automatically generated from `GreetParams`
- ✅ **Strongly-typed** - IntelliSense and compile-time safety
- ✅ **Clean code** - easy to read and maintain

#### 3.2. Advanced Tool (Custom Schema)

For complex validation or when you need full control over the JSON Schema:

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("greet",
        Title = "Greet user",
        Description = "Greets a user by name with custom validation.",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":
            {
                ""name"":{ 
                    ""type"":""string"",
                    ""description"":""Name of the user"",
                    ""minLength"": 2,
                    ""maxLength"": 50
                }
            },
            ""required"": [""name""]
        }")]
    public JsonRpcMessage Greet(JsonRpcMessage message)
    {
        var name = message.GetParams().GetProperty("name").GetString();
        return ToolResponse.Success(
            message.Id,
            new { message = $"Hello, {name}!" });
    }
}
```

**When to use:**
- ✅ Custom validation rules (minLength, maxLength, pattern, etc.)
- ✅ Complex schema features not supported by auto-generation
- ✅ Full control over JSON Schema

More complete tool examples:

- `Examples/CalculatorMcpServer/Tools/CalculatorTools.cs`
- `Examples/DateTimeMcpServer/Tools/DateTimeTools.cs`
- `DevTestServer/Tools/Calculator.cs` (with DI)

---

## 🔌 Connect from MCP clients

### GitHub Copilot (VS Code / Visual Studio)

Create or update `.mcp.json` for Copilot (global or per workspace):

```
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

Then in Copilot Chat:

```
@my_server call greet with name = "Alice"
```

### Claude Desktop

Point Claude to your HTTP endpoint:

```
{
  "mcpServers": {
    "my_server": {
      "transport": "http",
      "url": "https://your-server.example.com/rpc"
    }
  }
}
```

Claude can also use WebSocket (`/ws`) for full duplex and binary streaming.

---

## 📄 Pagination (v1.6.0)

When you have many tools, prompts, or resources, use cursor-based pagination:

### Request with pagination
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "eyJvZmZzZXQiOjEwMH0=",
    "pageSize": 50
  },
  "id": 1
}
```

### Response with nextCursor
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [ /* 50 tools */ ],
    "nextCursor": "eyJvZmZzZXQiOjE1MH0="
  },
  "id": 1
}
```

**Features:**
- ✅ Optional parameters – works without pagination by default
- ✅ Base64-encoded cursor format: `{"offset": 100}`
- ✅ Default page size: 100 items
- ✅ Works with `tools/list`, `prompts/list`, `resources/list`
- ✅ Alphabetically sorted for consistent results

See `Examples/PaginationMcpServer` for a demo with 120+ tools.

---

## 🔔 Notifications (v1.7.0 - MCP 2025-11-25 Compliant!)

Get real-time updates when tools, prompts, or resources change via **SSE** (Server-Sent Events):

### Server sends notification via SSE
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/tools/list_changed",
  "params": {}
}
```

### Client receives notification via SSE stream
```http
GET /mcp HTTP/1.1
Accept: text/event-stream
MCP-Session-Id: abc123
MCP-Protocol-Version: 2025-11-25

HTTP/1.1 200 OK
Content-Type: text/event-stream

id: 1
event: message
data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}

id: 2
event: message
data: {"jsonrpc":"2.0","method":"notifications/prompts/list_changed","params":{}}
```

**Notification types:**
- `notifications/tools/list_changed` – Tools added, removed, or modified
- `notifications/prompts/list_changed` – Prompts updated
- `notifications/resources/updated` – Resources changed (optional `uri` parameter)

**How to send notifications:**
```csharp
public class MyTools(INotificationSender notificationSender)
{
    [McpTool("reload_tools")]
    public async Task<JsonRpcMessage> ReloadTools(JsonRpcMessage request)
    {
        // Your tool logic...
        
        // Notify all active SSE streams (v1.7.0)
        await notificationSender.SendNotificationAsync(
            NotificationMessage.ToolsChanged());
        
        return ToolResponse.Success(request.Id, new { reloaded = true });
    }
}
```

**Features (v1.7.0):**
- ✅ SSE-based notifications (MCP 2025-11-25 compliant)
- ✅ Message buffering per session (100 messages)
- ✅ `Last-Event-ID` resumption on reconnect
- ✅ Automatic broadcast to all active sessions
- ✅ WebSocket notifications still work (deprecated)

**Migration from v1.6.x:**
```csharp
// v1.6.x - WebSocket only (still works!):
notificationService.AddSubscriber(webSocket);

// v1.7.0 - SSE (recommended, automatic):
// Client opens: GET /mcp with MCP-Session-Id header
// Server automatically broadcasts via SSE to all sessions
// No code changes needed!
```

See `Examples/NotificationMcpServer` for a demo with manual notification triggers.

---

## 📊 Lifecycle Hooks (v1.8.0)

Monitor and track tool invocations with **Lifecycle Hooks** - perfect for metrics, logging, authorization, and production monitoring:

### Built-in Hooks

**LoggingToolLifecycleHook** - Simple logging integration:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddToolsService();
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();  // Log all invocations

var app = builder.Build();
```

**MetricsToolLifecycleHook** - In-memory metrics tracking:
```csharp
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();

// Expose metrics via HTTP endpoint
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    var metrics = metricsHook?.GetMetrics();
    
    return Results.Json(new
    {
        timestamp = DateTime.UtcNow,
        metrics = metrics?.Select(kvp => new
        {
            tool = kvp.Key,
            invocations = kvp.Value.InvocationCount,
            successes = kvp.Value.SuccessCount,
            failures = kvp.Value.FailureCount,
            successRate = Math.Round(kvp.Value.SuccessRate * 100, 2),
            avgDuration = Math.Round(kvp.Value.AverageDuration.TotalMilliseconds, 2)
        })
    });
});
```

### Authorization Hooks

Implement role-based authorization using lifecycle hooks:

```csharp
using Mcp.Gateway.Tools.Lifecycle;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RequireRoleAttribute : Attribute
{
    public string Role { get; }
    public RequireRoleAttribute(string role) => Role = role;
}

public class AuthorizationHook : IToolLifecycleHook
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var requiredRoles = GetRequiredRoles(toolName);
        var userRoles = httpContext?.Items["UserRoles"] as List<string>;
        
        if (!HasRequiredRole(requiredRoles, userRoles))
        {
            throw new ToolInvalidParamsException(
                $"Insufficient permissions to invoke '{toolName}'.",
                toolName);
        }
        
        return Task.CompletedTask;
    }
    
    // ... other methods
}

// Usage on tools:
[McpTool("delete_user")]
[RequireRole("Admin")]
public JsonRpcMessage DeleteUser(JsonRpcMessage request) { /* ... */ }

// Register authorization hook:
builder.AddToolLifecycleHook<AuthorizationHook>();
```

See `docs/Authorization.md` for complete authorization guide and production patterns.

### Custom Hooks

Implement `IToolLifecycleHook` for custom monitoring:

```csharp
using Mcp.Gateway.Tools.Lifecycle;

public class PrometheusHook : IToolLifecycleHook
{
    private readonly Counter _invocations;
    private readonly Histogram _duration;
    
    public PrometheusHook()
    {
        _invocations = Metrics.CreateCounter(
            "mcp_tool_invocations_total",
            "Total tool invocations",
            new CounterConfiguration { LabelNames = new[] { "tool", "status" } });
        
        _duration = Metrics.CreateHistogram(
            "mcp_tool_duration_seconds",
            "Tool execution duration",
            new HistogramConfiguration { LabelNames = new[] { "tool" } });
    }
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        _invocations.WithLabels(toolName, "started").Inc();
        return Task.CompletedTask;
    }
    
    public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
    {
        _invocations.WithLabels(toolName, "success").Inc();
        _duration.WithLabels(toolName).Observe(duration.TotalSeconds);
        return Task.CompletedTask;
    }
    
    public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
    {
        _invocations.WithLabels(toolName, "failure").Inc();
        return Task.CompletedTask;
    }
}

// Register custom hook
builder.AddToolLifecycleHook<PrometheusHook>();
```

### Tracked Metrics

Per tool, `MetricsToolLifecycleHook` tracks:
- **Invocation count** - Total calls (success + failures)
- **Success/Failure count** - Breakdown by outcome
- **Success rate** - Percentage of successful invocations
- **Duration** - Min, Max, Average execution time
- **Error types** - Count of errors by exception type

### Example Output

```json
{
  "timestamp": "2025-12-19T16:30:00Z",
  "metrics": [
    {
      "tool": "add_numbers",
      "invocations": 150,
      "successes": 150,
      "failures": 0,
      "successRate": 100.0,
      "avgDuration": 1.23
    },
    {
      "tool": "divide",
      "invocations": 50,
      "successes": 48,
      "failures": 2,
      "successRate": 96.0,
      "avgDuration": 1.15
    }
  ]
}
```

**Features:**
- ✅ **Fire-and-forget** - Hooks don't block tool execution
- ✅ **Exception-safe** - Hook errors are logged, not propagated (except `ToolInvalidParamsException` for authorization)
- ✅ **Multiple hooks** - Register as many as needed
- ✅ **Zero config** - Optional, backward compatible
- ✅ **Production-ready** - Thread-safe metrics tracking
- ✅ **Authorization support** - Use for role-based access control

See:
- `Examples/MetricsMcpServer` - Metrics endpoint demo
- `Examples/AuthorizationMcpServer` - Role-based authorization
- `docs/LifecycleHooks.md` - Complete API reference
- `docs/Authorization.md` - Authorization patterns and best practices

---

## 💡 Features

- ✅ **MCP 2025‑11‑25** – 100% compliant with latest MCP specification (v1.7.0)
- ✅ **Transports** 
  - **Unified /mcp endpoint (v1.7.0):** POST + GET + DELETE for MCP 2025-11-25 compliance
  - HTTP (`/rpc`), WebSocket (`/ws`), SSE (`/sse`), stdio
  - Legacy endpoints still work (deprecated)
- ✅ **Session Management (v1.7.0)** 
  - `MCP-Session-Id` header support
  - Configurable timeout (30 min default)
  - Session-scoped event IDs
- ✅ **Protocol Version Validation (v1.7.0)**
  - `MCP-Protocol-Version` header validation
  - Supports 2025-11-25, 2025-06-18, 2025-03-26
- ✅ **Auto‑discovery** – tools, prompts, and resources discovered via attributes
- ✅ **Transport‑aware filtering (v1.2.0)**  
  - HTTP/stdio: standard tools only  
  - SSE: standard + text streaming  
  - WebSocket: all tools (incl. binary streaming)
- ✅ **Typed tools & optional schema generation (v1.3.0)**  
  - `TypedJsonRpc<T>` helper for strongly-typed tool implementations  
  - Optional JSON Schema auto-generation when `InputSchema` is omitted and the tool uses `TypedJsonRpc<T>`
- ✅ **MCP Prompts (v1.4.0)**  
  - `[McpPrompt]` attribute and prompt models for defining reusable prompt templates  
  - MCP prompt protocol support: `prompts/list`, `prompts/get`, and prompt capabilities in `initialize`
- ✅ **MCP Resources (v1.5.0)**  
  - `[McpResource]` attribute for exposing data and content (files, databases, APIs, system metrics)  
  - MCP resource protocol support: `resources/list`, `resources/read`, and resource capabilities in `initialize`  
  - URI-based addressing: `file://`, `db://`, `system://`, `http://`
- ✅ **Cursor-based pagination (v1.6.0)**  
  - Optional `cursor` and `pageSize` parameters for `tools/list`, `prompts/list`, `resources/list`  
  - `nextCursor` in response when more results are available  
  - Default page size: 100 items (configurable)  
  - Alphabetically sorted results for consistent pagination
- ✅ **SSE-based Notifications (v1.7.0 - MCP 2025-11-25 compliant!)**  
  - Real-time updates via Server-Sent Events
  - `notifications/tools/list_changed`, `notifications/prompts/list_changed`, `notifications/resources/updated`  
  - Message buffering (100 messages per session)
  - `Last-Event-ID` resumption on reconnect
  - Automatic broadcast to all active sessions
  - `NotificationService` with thread-safe subscriber management  
  - WebSocket notifications still work (deprecated)
- ✅ **Lifecycle Hooks (v1.8.0)**
  - Monitor tool invocations with `IToolLifecycleHook`
  - Built-in hooks: `LoggingToolLifecycleHook`, `MetricsToolLifecycleHook`
  - Track invocation count, success rate, duration, errors
  - Fire-and-forget pattern (exception-safe)
  - Production-ready metrics for Prometheus, Application Insights
- ✅ **Streaming** – text and binary streaming via `ToolConnector`
- ✅ **DI support** – tools, prompts, and resources can take services as parameters
- ✅ **Tested** – 258 tests covering HTTP, WS, SSE and stdio

---

## 📚 Learn more

- **Library README:** `Mcp.Gateway.Tools/README.md`  
  Details for the tools API (attributes, JsonRpc models, etc.)
- **Documentation:**
  - `docs/MCP-Protocol.md` - MCP protocol specification
  - `docs/StreamingProtocol.md` - Streaming protocol details
  - `docs/JSON-RPC-2.0-spec.md` - JSON-RPC 2.0 specification
  - `docs/LifecycleHooks.md` - Lifecycle hooks API reference (v1.8.0)
  - `docs/Authorization.md` - Authorization patterns and best practices (v1.8.0)
- **Examples:**
  - `Examples/CalculatorMcpServer` – calculator server
  - `Examples/DateTimeMcpServer` – date/time tools
  - `Examples/PromptMcpServer` – prompt templates
  - `Examples/ResourceMcpServer` – file, system, and database resources
  - `Examples/PaginationMcpServer` – pagination with 120+ mock tools (v1.6.0)
  - `Examples/NotificationMcpServer` – WebSocket notifications demo (v1.6.0)
  - `Examples/MetricsMcpServer` – lifecycle hooks with metrics endpoint (v1.8.0)
  - `Examples/AuthorizationMcpServer` – role-based authorization (v1.8.0)

---

## 🧪 Testing

```
dotnet test
```

The tests use `DevTestServer` as the host for end‑to‑end scenarios:

- HTTP: `Mcp.Gateway.Tests/Endpoints/Http/*`
- WebSocket: `Mcp.Gateway.Tests/Endpoints/Ws/*`
- SSE: `Mcp.Gateway.Tests/Endpoints/Sse/*`
- stdio: `Mcp.Gateway.Tests/Endpoints/Stdio/*`

---

## 📦 Projects in this repo

| Project                  | Description                                         |
|--------------------------|-----------------------------------------------------|
| `Mcp.Gateway.Tools`      | Core library / published NuGet package              |
| `DevTestServer`          | Internal test server used by the tests              |
| `Mcp.Gateway.Tests`      | Test project (70+ integration tests)                |
| `Examples/*`             | Small focused sample servers (calculator, datetime) |

Only `Mcp.Gateway.Tools` is intended as a product / NuGet dependency. The other projects are for development, verification and examples.

---

## 🤝 Contributing

See `CONTRIBUTING.md` for guidelines, code style and test requirements.

---

## 📜 License

MIT © 2024–2025 ARKo AS – AHelse Development Team

