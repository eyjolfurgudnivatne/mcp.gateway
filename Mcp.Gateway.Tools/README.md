# 🚀 MCP Gateway Tools

**Model Context Protocol (MCP) Gateway for .NET** - A flexible, production-ready implementation of the MCP protocol with support for HTTP, WebSocket, and stdio transports.

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![C# 14](https://img.shields.io/badge/C%23-14-blue)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--06--18-green)](https://modelcontextprotocol.io/)
[![JSON-RPC 2.0](https://img.shields.io/badge/JSON--RPC-2.0-orange)](https://www.jsonrpc.org/specification)

---

## 📋 Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Transport Modes](#transport-modes)
- [Creating Tools](#creating-tools)
- [Tool Naming Rules](#tool-naming-rules)
- [Examples](#examples)
- [Architecture](#architecture)
- [Testing](#testing)
- [Contributing](#contributing)

---

## ✨ Features

### Transport Support
- ✅ **HTTP RPC** - Simple POST requests for tool invocation
- ✅ **WebSocket** - Full-duplex streaming with binary support
- ✅ **SSE (Server-Sent Events)** - Remote MCP clients (Claude Desktop)
- ✅ **stdio** - Standard input/output for local MCP clients (GitHub Copilot, Claude Desktop)

### Protocol Support
- ✅ **JSON-RPC 2.0** - Fully compliant with spec (including number, string, or null `id`)
- ✅ **MCP Protocol 2025-06-18** - Latest version compatibility
- ✅ **Batch requests** - Multiple requests in single call
- ✅ **Notifications** - Fire-and-forget messages

### Streaming Capabilities
- ✅ **Text streaming** - JSON chunks over WebSocket
- ✅ **Binary streaming** - High-performance binary data transfer
- ✅ **Duplex streaming** - Bidirectional real-time communication
- ✅ **Stream lifecycle** - Proper start/chunk/done/error handling

### Performance (v1.0.1)
- ⚡ **ArrayPool buffers** - 90% reduction in GC pressure for WebSocket streaming
- ⚡ **Direct UTF-8 serialization** - Optimized JSON-to-bytes conversion
- ⚡ **Zero-copy streaming** - Efficient buffer reuse across connections
- 🎯 **Benchmarked** - Comprehensive performance testing with BenchmarkDotNet

### Developer Experience
- ✅ **Attribute-based tools** - Simple `[McpTool]` attribute
- ✅ **Auto-discovery** - Tools are automatically scanned and registered
- ✅ **Dependency injection** - Full DI support for tool methods
- ✅ **Type safety** - Strongly-typed request/response models
- ✅ **Comprehensive logging** - Debug-friendly with file logging for stdio
- ✅ **Production-ready** - Battle-tested with 45+ comprehensive tests

---

## 🚀 Quick Start

### 1. Create a Tool

```csharp
using Mcp.Gateway.Tools;

public class Calculator
{
    [McpTool("add",
        Title = "Add Numbers",
        Description = "Adds two integers",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""a"":{""type"":""integer"",""description"":""First number""},
                ""b"":{""type"":""integer"",""description"":""Second number""}
            },
            ""required"":[""a"",""b""]
        }")]
    public async Task<JsonRpcMessage> AddTool(JsonRpcMessage request)
    {
        var args = request.GetParams<AddRequest>();
        var result = new { sum = args.a + args.b };
        return ToolResponse.Success(request.Id, result);
    }
    
    record AddRequest(int a, int b);
}
```

### 2. Configure Server

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add MCP tools service
builder.AddToolsService();

var app = builder.Build();

// Enable WebSockets
app.UseWebSockets();

// Map endpoints
app.MapHttpRpcEndpoint("/rpc");      // HTTP POST
app.MapWsRpcEndpoint("/ws");         // WebSocket
app.MapSseRpcEndpoint("/sse");       // Server-Sent Events (SSE)

app.Run();
```

### 3. Test with curl

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "add",
    "id": 1,
    "params": {"a": 5, "b": 3}
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {"sum": 8}
}
```

---

## 🔌 Transport Modes

### HTTP RPC (Simple)

**Use case:** Simple request/response, no streaming needed

```csharp
app.MapHttpRpcEndpoint("/rpc");
```

**Client:**
```bash
POST http://localhost:5000/rpc
Content-Type: application/json

{"jsonrpc":"2.0","method":"tools/list","id":1}
```

---

### WebSocket (Streaming)

**Use case:** Real-time streaming, binary data, duplex communication

```csharp
app.UseWebSockets();
app.MapWsRpcEndpoint("/ws");
```

**Client:**
```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.send(JSON.stringify({
  jsonrpc: '2.0',
  method: 'tools/list',
  id: 1
}));
```

---

### SSE (Server-Sent Events)

**Use case:** Remote MCP clients, Claude Desktop, browser-based clients

```csharp
app.MapSseRpcEndpoint("/sse");
```

**Client:**
```bash
curl -X POST http://localhost:5000/sse \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

**Claude Desktop Configuration:**
```json
{
  "mcpServers": {
    "my_server": {
      "url": "https://your-domain.com/sse",
      "transport": "sse"
    }
  }
}
```

---

### stdio (Local MCP Clients)

**Use case:** GitHub Copilot, Claude Desktop, local automation

**Program.cs:**
```csharp
var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// ... HTTP/WebSocket setup
```

**Run:**
```bash
dotnet run --project YourServer -- --stdio
```

**Configure in `.mcp.json`:**
```json
{
  "servers": {
    "my_server": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "path/to/YourServer", "--", "--stdio"]
    }
  }
}
```

---

## 🛠️ Creating Tools

### Basic Tool (JSON-RPC)

```csharp
[McpTool("ping",
    Title = "Ping",
    Description = "Returns pong",
    InputSchema = @"{""type"":""object"",""properties"":{}}")]
public async Task<JsonRpcMessage> PingTool(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new { message = "Pong" });
}
```

### Tool with Parameters

```csharp
[McpTool("greet",
    Title = "Greet User",
    Description = "Greets a user by name",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""name"":{""type"":""string"",""description"":""Name to greet""}
        },
        ""required"":[""name""]
    }")]
public async Task<JsonRpcMessage> GreetTool(JsonRpcMessage request)
{
    var args = request.GetParams<GreetRequest>();
    return ToolResponse.Success(request.Id, new { 
        greeting = $"Hello, {args.name}!" 
    });
}

record GreetRequest(string name);
```

### Streaming Tool (Binary)

```csharp
[McpTool("download_file",
    Title = "Download File",
    Description = "Streams a file to the client",
    InputSchema = @"{""type"":""object"",""properties"":{}}")]
public static async Task DownloadFileTool(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "file_download",
        Binary: true,
        Mime: "application/octet-stream");

    using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
    
    var fileData = await File.ReadAllBytesAsync("file.bin");
    await handle.WriteAsync(fileData);
    await handle.CompleteAsync(new { size = fileData.Length });
}
```

---

## ⚠️ Tool Naming Rules

### ✅ Valid Tool Names

Tool names **MUST** match the pattern: `^[a-zA-Z0-9_-]{1,128}$`

**Examples:**
```csharp
✅ "ping"
✅ "add_numbers"
✅ "get-current-time"
✅ "my_tool_v2"
✅ "CalculateSum"
```

### ❌ Invalid Tool Names

```csharp
❌ "system.ping"        // NO dots (.) allowed!
❌ "get current time"   // NO spaces
❌ "hello@world"        // NO special chars (@, %, etc.)
❌ "tool#123"           // NO hash (#)
❌ ""                   // NO empty names
❌ "very_long_name_that_exceeds_the_maximum_allowed_length_of_128_characters_..." // Too long
```

### 💡 Why No Dots?

GitHub Copilot and other MCP clients enforce strict naming validation:
```
tools.0.custom.name: String should match pattern '^[a-zA-Z0-9_-]{1,128}$'
```

**Use underscores (`_`) or hyphens (`-`) instead:**
```csharp
❌ "system.ping"  →  ✅ "system_ping" or "system-ping"
❌ "get.user.id"  →  ✅ "get_user_id" or "get-user-id"
```

---

## 📚 Examples

### Example 1: Calculator

```csharp
public class Calculator
{
    [McpTool("multiply",
        Title = "Multiply",
        Description = "Multiplies two numbers",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""x"":{""type"":""number""},
                ""y"":{""type"":""number""}
            },
            ""required"":[""x"",""y""]
        }")]
    public async Task<JsonRpcMessage> MultiplyTool(JsonRpcMessage request)
    {
        var args = request.GetParams<MultiplyRequest>();
        return ToolResponse.Success(request.Id, new { 
            product = args.x * args.y 
        });
    }
    
    record MultiplyRequest(double x, double y);
}
```

### Example 2: With Dependency Injection

```csharp
public class DatabaseTool
{
    [McpTool("get_user",
        Title = "Get User",
        Description = "Fetches user by ID",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""userId"":{""type"":""integer""}
            },
            ""required"":[""userId""]
        }")]
    public async Task<JsonRpcMessage> GetUserTool(
        JsonRpcMessage request, 
        IUserRepository repo)  // ← Injected from DI!
    {
        var args = request.GetParams<GetUserRequest>();
        var user = await repo.GetByIdAsync(args.userId);
        return ToolResponse.Success(request.Id, user);
    }
    
    record GetUserRequest(int userId);
}
```

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│         MCP Gateway Server              │
│  (HTTP / WebSocket / stdio transport)   │
└─────────────────┬───────────────────────┘
                  │
        ┌─────────▼─────────┐
        │   ToolInvoker     │
        │ (Request Router)  │
        └─────────┬─────────┘
                  │
        ┌─────────▼─────────┐
        │   ToolService     │
        │ (Tool Registry)   │
        └─────────┬─────────┘
                  │
      ┌───────────┼───────────┐
      │           │           │
┌─────▼────┐ ┌───▼────┐ ┌────▼────┐
│  Tool A  │ │ Tool B │ │ Tool C  │
│  (Ping)  │ │ (Echo) │ │(Stream) │
└──────────┘ └────────┘ └─────────┘
```

### Key Components

| Component | Responsibility |
|-----------|----------------|
| **ToolInvoker** | Routes JSON-RPC requests to tools, handles HTTP/WS/stdio |
| **ToolService** | Scans and registers tools, manages tool metadata |
| **ToolConnector** | Manages WebSocket ownership for streaming tools |
| **JsonRpcMessage** | Type-safe JSON-RPC 2.0 message model |
| **StreamMessage** | Streaming protocol message model |

---

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Test Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Example Test

```csharp
[Fact]
public async Task Ping_ReturnsSuccessResponse()
{
    // Arrange
    var request = new
    {
        jsonrpc = "2.0",
        method = "ping",
        id = 1
    };

    // Act
    var response = await httpClient.PostAsJsonAsync("/rpc", request);
    
    // Assert
    response.EnsureSuccessStatusCode();
    var json = await response.Content.ReadFromJsonAsync<JsonRpcMessage>();
    Assert.Equal(1, json.Id);
    Assert.NotNull(json.Result);
}
```

---

## 🤝 Contributing

### Extending with New Tools

1. Create a class with `[McpTool]` methods
2. Place it in your project (tools are auto-discovered)
3. Build and run - it's automatically registered!

### Naming Conventions

- Tool names: `lowercase_with_underscores` or `kebab-case`
- Class names: `PascalCase`
- Methods: `PascalCase` + `Tool` suffix (e.g., `PingTool`)

---

## 📖 Protocol Documentation

- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [MCP Protocol](https://modelcontextprotocol.io/)
- [MCP Protocol Documentation](../docs/MCP-Protocol.md)
- [Streaming Protocol v1.0](../docs/StreamingProtocol.md)
- [Performance Optimization](../docs/Performance-Optimization-Plan.md)

---

## ⚡ Performance

**v1.0.1 Optimizations:**

### ArrayPool for WebSocket Buffers
- **159x faster** buffer management
- **100% GC elimination** for WebSocket streaming
- **99.7% memory reduction** in real-world scenarios

### Benchmark Results

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Execution | 77,653 ns | 490 ns | **159x faster** |
| Gen0 GC | 781 collections | 0 | **100% eliminated** |
| Allocated | 6.4 MB | 0 bytes | **Perfect reuse** |

**See:** [Performance Optimization Plan](../docs/Performance-Optimization-Plan.md) for details.

---

## 📜 License

MIT License - see LICENSE file for details

---

## 🙏 Acknowledgments

**Built by:** ARKo AS - AHelse Development Team

Built with ❤️ using:
- [.NET 10](https://dotnet.microsoft.com/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [BenchmarkDotNet](https://benchmarkdotnet.org/) - For performance optimization

**Special thanks:**
- **Anthropic** - For MCP specification
- **Microsoft** - For .NET 10 and ArrayPool<T>
- **GitHub** - For Copilot and MCP client support

---

**Version:** 1.0.1  
**Last Updated:** 5. desember 2025  
**License:** MIT

---

**Need help?** Open an issue or check the [examples](../Mcp.Gateway.Server/Tools/Systems/) folder!
