# 🛠️ Mcp.Gateway.Tools

> Core library for building MCP servers in .NET 10

[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--11--25-green)](https://modelcontextprotocol.io/)

`Mcp.Gateway.Tools` contains the infrastructure for MCP tools, prompts, and resources:

- JSON‑RPC models (`JsonRpcMessage`, `JsonRpcError`)
- Attributes (`McpToolAttribute`, `McpPromptAttribute`, `McpResourceAttribute`)
- Tool/Prompt/Resource registration (`ToolService`)
- Invocation and protocol implementation (`ToolInvoker`)
- Streaming (`ToolConnector`, `ToolCapabilities`)
- ASP.NET Core extensions for endpoints (`MapHttpRpcEndpoint`, `MapWsRpcEndpoint`, `MapSseRpcEndpoint`, `AddToolsService`)

This README focuses on **how to use the library in your own server**.  
See the root `README.md` for a high‑level overview and client integration.

---

## 🔧 Register tool infrastructure

In your `Program.cs` (v1.7.0 with MCP 2025-11-25 support):

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register ToolService + ToolInvoker + Session Management (v1.7.0)
builder.AddToolsService();

var app = builder.Build();

// Custom: stdio, logging, etc. (see DevTestServer for a full example)

// WebSockets must be enabled before WS/SSE routes
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

`AddToolsService` registers:

- `ToolService` as a singleton (discovers/validates tools)
- `ToolInvoker` as scoped (handles JSON‑RPC and MCP methods)
- `EventIdGenerator` as singleton (v1.7.0 - SSE event IDs)
- `SessionService` as singleton (v1.7.0 - session management)
- `SseStreamRegistry` as singleton (v1.7.0 - SSE stream management)
- `INotificationSender` → `NotificationService` as singleton (notification infrastructure)

---

## 🧩 Defining tools

### 3.1. Simplest Tool (Auto-generated Schema)

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

### 3.2. Advanced Tool (Custom Schema)

For complex validation or when you need full control over the JSON Schema:

```csharp
using CalculatorMcpServer.Models;
using Mcp.Gateway.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and returns the result.",
        Icon = "https://example.com/icons/calculator.png",  // NEW: Icon (v1.6.5)
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":
            {
                ""number1"":{""type"":""number"",""description"":""First number""},
                ""number2"":{""type"":""number"",""description"":""Second number""}
            },
            ""required"": [""number1"",""number2""]
        }")]
    public JsonRpcMessage AddNumbersTool(JsonRpcMessage request)
    {
        var args = request.GetParams<AddNumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new AddNumbersResponse(args.Number1 + args.Number2));
    }
}
```

**When to use:**
- ✅ Custom validation rules (minLength, maxLength, pattern, etc.)
- ✅ Complex schema features not supported by auto-generation
- ✅ Full control over JSON Schema

**Icons (v1.6.5+):**

Tools, prompts, and resources can include optional icons for visual representation in MCP clients:

```csharp
[McpTool("calculator_add",
    Icon = "https://example.com/icons/calculator.png")]

[McpPrompt("summarize",
    Icon = "https://example.com/icons/document.png")]

[McpResource("file://logs/app.log",
    Icon = "https://example.com/icons/log-file.png")]
```

Icons are serialized as a single-item array in the MCP protocol:
```json
{
  "name": "add_numbers",
  "icons": [
    {
      "src": "https://example.com/icons/calculator.png",
      "mimeType": null,
      "sizes": null
    }
  ]
}
```

The `Icon` property accepts:
- ✅ HTTPS URLs: `"https://example.com/icon.png"`
- ✅ Data URIs: `"data:image/svg+xml;base64,..."`
- ℹ️ `mimeType` and `sizes` are automatically set to `null` (client infers from URL)

### 3.3. With Validation and DI

For tools that need dependency injection and advanced validation (from `DevTestServer/Tools/Calculator.cs`):

```
using DevTestServer.MyServices;
using Mcp.Gateway.Tools;
using System.Text.Json;

public class Calculator
{
    public sealed record NumbersRequest(double Number1, double Number2);
    public sealed record NumbersResponse(double Result);

    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""number1"":{""type"":""number"",""description"":""First number to add""},
                ""number2"":{""type"":""number"",""description"":""Second number to add""}
            },
            ""required"": [""number1"",""number2""]
        }")]
    public async Task<JsonRpcMessage> AddNumbersTool(
        JsonRpcMessage request,
        CalculatorService calculatorService)
    {
        await Task.CompletedTask; // placeholder for async work

        var @params = request.GetParams();

        if (@params.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ||
            !@params.TryGetProperty("number1", out _) ||
            !@params.TryGetProperty("number2", out _))
        {
            throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");
        }

        var args = request.GetParams<NumbersRequest>()!;
        var result = calculatorService.Add(args.Number1, args.Number2);

        return ToolResponse.Success(request.Id, new NumbersResponse(result));
    }
}
```

Register the DI service in `Program.cs`:

```
builder.Services.AddScoped<CalculatorService>();
```

### TypedJsonRpc<T> (optional helper, v1.3.0)

For tools that prefer strongly-typed request models, you can use `TypedJsonRpc<T>`:

```
public sealed record AddNumbersRequestTyped(
    [property: JsonPropertyName("number1")]
    [property: Description("First number to add")] double Number1,

    [property: JsonPropertyName("number2")]
    [property: Description("Second number to add")] double Number2);

[McpTool("add_numbers_typed_ii",
    Title = "Add Numbers (typed)",
    Description = "Adds two numbers using TypedJsonRpc with auto-generated schema.")]
public JsonRpcMessage AddNumbersToolTypedII(TypedJsonRpc<AddNumbersRequestTyped> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException(
            "Parameters 'number1' and 'number2' are required and must be numbers.");

    return ToolResponse.Success(
        request.Id,
        new { result = args.Number1 + args.Number2 });
}
```

If `InputSchema` is omitted on `[McpTool]` **and** the first parameter is `TypedJsonRpc<TParams>`:

- `tools/list` will auto-generate a JSON Schema for `TParams`:
  - Root: `type: "object"`
  - `properties` from public properties on `TParams`
  - `required` from non-nullable properties (nullable → optional)
  - `JsonPropertyName` for JSON field names
  - `[property: Description("...")]` mapped to `description`
  - enums represented as string enums (`"type": "string", "enum": ["Active", "Disabled"]`)

If `InputSchema` is set on `[McpTool]`, it always wins and auto-generation is skipped.

### MCP Prompts (v1.4.0)

You can define reusable prompt templates using `[McpPrompt]` and the prompt models:

```
[McpPrompt(Description = "Report to Santa Claus")]
public JsonRpcMessage SantaReportPrompt(JsonRpcMessage request)
{
    return ToolResponse.Success(
        request.Id,
        new PromptResponse(
            Name: "santa_report_prompt",
            Description: "A prompt that reports to Santa Claus",
            Messages: [
                new(
                    PromptRole.System,
                    "You are a very helpful assistant for Santa Claus."),
                new(
                    PromptRole.User,
                    "Send a letter to Santa Claus and tell him that {name} has been {behavior}.")
            ],
            Arguments: new {
                name = new {
                    type = "string",
                    description = "Name of the child"
                },
                behavior = new {
                    type = "string",
                    description = "Behavior of the child (e.g., Good, Naughty)",
                    @enum = new[] { "Good", "Naughty" }
                }
            }
        ));
}
```

Prompts are exposed via the MCP prompt methods and `initialize` capabilities:

- `prompts/list` – returns all discovered prompts with `name`, `description`, `arguments`.
- `prompts/get` – returns the expanded prompt `messages` for a given prompt and arguments.
- `initialize` – includes a `prompts` capability flag when prompts are registered.

On the wire, prompts are regular JSON-RPC responses whose `result` contains a prompt object with `name`,
`description`, `messages` (each with `role` and `content`), and `arguments` that clients can use to build LLM calls.

### MCP Resources (v1.5.0)

You can expose data and content using `[McpResource]`:

```csharp
[McpResource("system://status",
    Name = "System Status",
    Description = "Current system health metrics",
    MimeType = "application/json")]
public JsonRpcMessage SystemStatus(JsonRpcMessage request)
{
    var status = new
    {
        uptime = Environment.TickCount64,
        memoryUsed = GC.GetTotalMemory(false) / (1024 * 1024),
        timestamp = DateTime.UtcNow
    };
    
    var json = JsonSerializer.Serialize(status, JsonOptions.Default);
    
    return ToolResponse.Success(request.Id, new ResourceContent(
        Uri: "system://status",
        MimeType: "application/json",
        Text: json
    ));
}
```

**Resource URI patterns:**
- `file://logs/app.log` - File-based resources
- `db://users/123` - Database resources
- `system://status` - System metrics
- `http://api.example.com/data` - External APIs

Resources are exposed via the MCP resource methods and `initialize` capabilities:

- `resources/list` – returns all discovered resources with `uri`, `name`, `description`, `mimeType`.
- `resources/read` – returns the content of a specific resource by URI.
- `initialize` – includes a `resources` capability flag when resources are registered.

On the wire, resources are regular JSON-RPC responses whose `result` contains resource metadata (for list) or
content (for read) with `uri`, `mimeType`, and `text` fields.

---

## 🕒 Date/time tools (example)

From `Examples/DateTimeMcpServer/Tools/DateTimeTools.cs`:

```
using DateTimeMcpServer.Models;
using Mcp.Gateway.Tools;

public class DateTimeTools
{
    [McpTool("get_current_datetime",
        Title = "Get current date and time",
        Description = "Get current date and time in specified timezone (default: local).",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""timezoneName"":{
                    ""type"":""string"",
                    ""description"":""Timezone name (e.g., 'Europe/Oslo', 'UTC')"",
                    ""default"":""UTC""
                }
            }
        }")]
    public JsonRpcMessage GetCurrentDateTime(JsonRpcMessage message)
    {
        var request = message.GetParams<CurrentDateTimeRequest>();
        TimeZoneInfo tz;

        try
        {
            tz = string.IsNullOrWhiteSpace(request?.TimezoneName)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(request.TimezoneName);
        }
        catch
        {
            tz = TimeZoneInfo.Local;
        }

        var now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);

        return ToolResponse.Success(
            message.Id,
            new CurrentDateTimeResponse(
                now.ToString("o"),
                now.ToString("yyyy-MM-dd"),
                now.ToString("HH:mm:ss"),
                tz.Id,
                now.ToString("dddd"),
                System.Globalization.ISOWeek.GetWeekOfYear(now),
                now.Year,
                now.Month,
                now.Day));
    }
}
```

---

## 🧵 Streaming and `ToolCapabilities`

### Capabilities

`ToolCapabilities` is used to filter tools per transport:

```
[Flags]
public enum ToolCapabilities
{
    Standard        = 1,
    TextStreaming   = 2,
    BinaryStreaming = 4,
    RequiresWebSocket = 8
}
```

- HTTP/stdio: `Standard` only
- SSE: `Standard` + `TextStreaming`
- WebSocket: all (incl. `BinaryStreaming` and `RequiresWebSocket`)

### Simple text streaming tool

```
[McpTool("stream_data",
    Description = "Streams incremental data to the client.",
    Capabilities = ToolCapabilities.TextStreaming)]
public async Task StreamData(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "stream_data",
        Binary: false);

    using var handle = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);

    for (int i = 0; i < 5; i++)
    {
        await handle.WriteAsync(new { chunk = i });
    }

    await handle.CompleteAsync(new { done = true });
}
```

See `docs/StreamingProtocol.md` and `docs/examples/toolconnector-usage.md` for more details.

---

## 📄 Pagination (v1.6.0)

When you have many tools, prompts, or resources, use cursor-based pagination to avoid overwhelming clients:

### Using pagination in tools/list

```csharp
// Client request with pagination
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "eyJvZmZzZXQiOjEwMH0=",
    "pageSize": 50
  },
  "id": 1
}

// Server response with nextCursor
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [ /* 50 tools */ ],
    "nextCursor": "eyJvZmZzZXQiOjE1MH0="
  },
  "id": 1
}
```

### Pagination helper (CursorHelper)

You can use `CursorHelper` in your own tools to implement pagination:

```csharp
using Mcp.Gateway.Tools.Pagination;

public class MyTools
{
    [McpTool("list_items")]
    public JsonRpcMessage ListItems(JsonRpcMessage request)
    {
        // Get pagination params
        var @params = request.GetParams();
        string? cursor = null;
        int pageSize = 100;
        
        if (@params.TryGetProperty("cursor", out var cursorProp))
            cursor = cursorProp.GetString();
        
        if (@params.TryGetProperty("pageSize", out var sizeProp))
            pageSize = sizeProp.GetInt32();
        
        // Get your items (e.g., from database)
        var allItems = GetAllItems();
        
        // Apply pagination
        var paginatedResult = CursorHelper.Paginate(allItems, cursor, pageSize);
        
        // Build response
        var response = new Dictionary<string, object>
        {
            ["items"] = paginatedResult.Items
        };
        
        if (paginatedResult.NextCursor is not null)
            response["nextCursor"] = paginatedResult.NextCursor;
        
        return ToolResponse.Success(request.Id, response);
    }
}
```

**Features:**
- ✅ Base64-encoded cursor: `{"offset": 100}`
- ✅ Default page size: 100 items
- ✅ Works with any `IEnumerable<T>`
- ✅ Thread-safe and stateless

See `Examples/PaginationMcpServer` for a complete example with 120+ tools.

---

## 🔔 Notifications (v1.7.0 - MCP 2025-11-25 Compliant!)

Send real-time updates via **SSE** (Server-Sent Events) when your tools, prompts, or resources change:

### Using INotificationSender

Inject `INotificationSender` into your tools to send notifications:

```csharp
using Mcp.Gateway.Tools.Notifications;

public class MyTools
{
    private readonly INotificationSender _notificationSender;
    
    public MyTools(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    [McpTool("reload_tools")]
    public async Task<JsonRpcMessage> ReloadTools(JsonRpcMessage request)
    {
        // Reload your tools (e.g., scan file system, refresh cache)
        await ReloadToolsFromFileSystem();
        
        // Notify all active SSE streams (automatic broadcast!)
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ToolsChanged());
        
        return ToolResponse.Success(request.Id, new { reloaded = true });
    }
    
    [McpTool("update_resource")]
    public async Task<JsonRpcMessage> UpdateResource(JsonRpcMessage request)
    {
        var uri = request.GetParams().GetProperty("uri").GetString();
        
        // Update the resource
        await UpdateResourceContent(uri);
        
        // Notify with specific URI
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated(uri));
        
        return ToolResponse.Success(request.Id, new { updated = uri });
    }
}
```

### Notification types

Three notification methods are available:

```csharp
// Tools changed
await notificationSender.SendNotificationAsync(
    NotificationMessage.ToolsChanged());

// Prompts changed
await notificationSender.SendNotificationAsync(
    NotificationMessage.PromptsChanged());

// Resources updated (optional URI)
await notificationSender.SendNotificationAsync(
    NotificationMessage.ResourcesUpdated("file://config/settings.json"));
```

### How it works (v1.7.0)

1. **Client connects** via `GET /mcp` with `MCP-Session-Id` header
2. **Server opens SSE stream** with keep-alive pings
3. **Server detects change** (tool added, resource updated, etc.)
4. **Server broadcasts notification** to all active SSE streams:
   ```http
   id: 42
   event: message
   data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}
   ```
5. **Client re-fetches** tools/list, prompts/list, or resources/list

### Message buffering and resumption (v1.7.0)

Notifications are automatically buffered per session (max 100 messages) for Last-Event-ID resumption:

```http
GET /mcp HTTP/1.1
MCP-Session-Id: abc123
Last-Event-ID: 42  # Resume from event 42

# Server replays events 43, 44, 45, ... then streams new events
```

### Notification capabilities

When `NotificationService` is registered (automatic via `AddToolsService()`), the `initialize` response includes:

```json
{
  "capabilities": {
    "tools": {},
    "prompts": {},
    "resources": {},
    "notifications": {
      "tools": {},
      "prompts": {},
      "resources": {}
    }
  }
}
```

**Note:** Only capabilities for registered function types are included. If your server has no prompts, `notifications.prompts` will not be present.

### Migration from v1.6.x (v1.7.0)

**Good news:** No code changes needed! Notifications automatically work via SSE in v1.7.0.

```csharp
// v1.6.x - WebSocket only (still works!)
notificationService.AddSubscriber(webSocket);

// v1.7.0 - SSE automatic (recommended)
// Client opens: GET /mcp with MCP-Session-Id
// Server automatically broadcasts via SSE
// No code changes needed!
```

WebSocket notifications are **deprecated but still functional** for backward compatibility.

See `Examples/NotificationMcpServer` for a complete example with manual notification triggers.

---

## 🧱 JSON models

Core models live in `ToolModels.cs`:

- `JsonRpcMessage` – JSON‑RPC 2.0 messages
- `JsonRpcError` – structured errors
- `JsonOptions.Default` – shared `JsonSerializerOptions` (camelCase, etc.)

Typical usage:

```
public JsonRpcMessage Echo(JsonRpcMessage message)
{
    var raw = message.GetParams();
    return JsonRpcMessage.CreateSuccess(message.Id, raw);
}
```

---

## 🧪 Verification and tests

The library itself is tested via `DevTestServer` + `Mcp.Gateway.Tests`:

- Protocol tests: `Mcp.Gateway.Tests/Endpoints/Http/McpProtocolTests.cs`
- Streaming tests: `Mcp.Gateway.Tests/Endpoints/Ws/*`, `.../Sse/*`
- stdio tests: `Mcp.Gateway.Tests/Endpoints/Stdio/*`

For your own development:

```
dotnet test
```

---

## 📌 Summary

To use `Mcp.Gateway.Tools` in your project:

1. Add the NuGet package
2. Call `builder.AddToolsService()` in `Program.cs`
3. Map `MapHttpRpcEndpoint`, `MapWsRpcEndpoint`, `MapSseRpcEndpoint` as needed
4. Define tools by annotating methods with `[McpTool]`
5. Connect the server to an MCP client (GitHub Copilot, Claude, etc.)

For complete examples, see:

- `Examples/CalculatorMcpServer` - Calculator tools
- `Examples/DateTimeMcpServer` - Date/time utilities
- `Examples/PromptMcpServer` - Prompt templates
- `Examples/ResourceMcpServer` - File, system, and database resources
- `Examples/PaginationMcpServer` - Pagination with 120+ mock tools (v1.6.0)
- `Examples/NotificationMcpServer` - WebSocket notifications demo (v1.6.0)
- `DevTestServer` - Full-featured reference (used by tests)

---

**License:** MIT – see root `LICENSE`.
