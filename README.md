# 🚀 Mcp.Gateway.Tools

> Build MCP servers in .NET 10 – production-ready in minutes

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![Tests](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml/badge.svg)](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--06--18-green)](https://modelcontextprotocol.io/)
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

Minimal HTTP + WebSocket server:

```
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register ToolService + ToolInvoker
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

// MCP endpoints
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");

app.Run();
```

See `DevTestServer/Program.cs` for a more complete setup with health endpoint and stdio logging.

### 3. Create your first tool

```
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("greet",
        Title = "Greet user",
        Description = "Greets a user by name.",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""name"":{ ""type"":""string"", ""description"":""Name of the user"" }
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

## 🔔 Notifications (v1.6.0)

Get real-time updates when tools, prompts, or resources change (WebSocket only):

### Server sends notification
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/tools/changed",
  "params": {}
}
```

### Client re-fetches tools
```csharp
// Client receives notification → re-fetch tools/list
var response = await httpClient.PostAsJsonAsync("/rpc", new {
    jsonrpc = "2.0",
    method = "tools/list",
    id = 2
});
```

**Notification types:**
- `notifications/tools/changed` – Tools added, removed, or modified
- `notifications/prompts/changed` – Prompts updated
- `notifications/resources/updated` – Resources changed (optional `uri` parameter)

**How to send notifications:**
```csharp
public class MyTools(INotificationSender notificationSender)
{
    [McpTool("reload_tools")]
    public async Task<JsonRpcMessage> ReloadTools(JsonRpcMessage request)
    {
        // Your tool logic...
        
        // Notify all WebSocket clients
        await notificationSender.SendNotificationAsync(
            NotificationMessage.ToolsChanged());
        
        return ToolResponse.Success(request.Id, new { reloaded = true });
    }
}
```

**Limitations:**
- ⚠️ Requires WebSocket transport
- ⚠️ HTTP and stdio clients must poll `tools/list` to detect changes
- 📅 SSE-based notifications planned for v1.7.0 (full MCP 2025-11-25 compliance)

See `Examples/NotificationMcpServer` for a demo with manual notification triggers.

---

## 💡 Features

- ✅ **MCP 2025‑06‑18** – up to date with the current MCP specification
- ✅ **Transports** – HTTP (`/rpc`), WebSocket (`/ws`), SSE (`/sse`), stdio
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
- ✅ **Notification infrastructure (v1.6.0)**  
  - WebSocket-based push notifications for dynamic updates  
  - `notifications/tools/changed`, `notifications/prompts/changed`, `notifications/resources/updated`  
  - `NotificationService` with thread-safe subscriber management  
  - Notification capabilities in `initialize` response  
  - **Note:** Notifications require WebSocket; HTTP/stdio clients must poll
- ✅ **Streaming** – text and binary streaming via `ToolConnector`
- ✅ **DI support** – tools, prompts, and resources can take services as parameters
- ✅ **Tested** – 130 tests covering HTTP, WS, SSE and stdio

---

## 📚 Learn more

- **Library README:** `Mcp.Gateway.Tools/README.md`  
  Details for the tools API (attributes, JsonRpc models, etc.)
- **MCP protocol:** `docs/MCP-Protocol.md`
- **Streaming protocol:** `docs/StreamingProtocol.md`
- **JSON‑RPC 2.0:** `docs/JSON-RPC-2.0-spec.md`
- **Examples:**
  - `Examples/CalculatorMcpServer` – calculator server
  - `Examples/DateTimeMcpServer` – date/time tools
  - `Examples/PromptMcpServer` – prompt templates
  - `Examples/ResourceMcpServer` – file, system, and database resources
  - `Examples/PaginationMcpServer` – pagination with 120+ mock tools (v1.6.0)
  - `Examples/NotificationMcpServer` – WebSocket notifications demo (v1.6.0)

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

