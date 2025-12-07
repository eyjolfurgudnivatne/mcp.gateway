# 🛠️ Mcp.Gateway.Tools

> .NET library for building MCP servers - NuGet Package

[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--06--18-green)](https://modelcontextprotocol.io/)

**Mcp.Gateway.Tools** is a production-ready library for building Model Context Protocol (MCP) servers in .NET. Enable AI assistants like GitHub Copilot and Claude Desktop to discover and invoke your custom tools.

---

## ⚡ Quick Start

```bash
dotnet add package Mcp.Gateway.Tools
```

### Create Your First Tool

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("greet")]
    public JsonRpcMessage Greet(JsonRpcMessage request)
    {
        var name = request.GetParams().GetProperty("name").GetString();
        return ToolResponse.Success(request.Id, new { message = $"Hello, {name}!" });
    }
}
```

### Setup Server

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.AddToolsService();

var app = builder.Build();

// stdio support for GitHub Copilot
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

app.UseWebSockets();
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.Run();
```

**That's it!** Your MCP server is ready. 🎉

---

## ✨ Features (v1.2.0)

### Transport Support
- ✅ **HTTP** - Simple JSON-RPC over POST
- ✅ **WebSocket** - Full-duplex streaming
- ✅ **SSE** - Server-Sent Events
- ✅ **stdio** - GitHub Copilot integration

### Smart Filtering (v1.2.0+)
- ✅ **Transport-aware** - Tools filtered by transport capabilities
- ✅ **stdio/http** - Standard tools only
- ✅ **sse** - Standard + text streaming
- ✅ **ws** - All tools (including binary streaming)

### Developer Experience
- ✅ **Auto-discovery** - Tools found automatically via `[McpTool]`
- ✅ **Auto-naming** - Optional tool name generation from method names
- ✅ **Type-safe** - Strong typing with C# records
- ✅ **DI support** - Full dependency injection
- ✅ **Production-ready** - 70+ tests passing

### Performance
- ⚡ **ArrayPool buffers** - 90% GC reduction for WebSocket streaming
- ⚡ **Zero-copy** - Efficient buffer reuse
- ⚡ **Benchmarked** - Comprehensive testing with BenchmarkDotNet

---

## 📚 Documentation

### Getting Started
- **[Main Documentation](../README.md)** - Complete guide
- **[Code Examples](../docs/examples/)** - Real-world examples
  - [Client Examples](../docs/examples/client-examples.md) - HTTP, WebSocket, SSE clients
  - [ToolConnector Usage](../docs/examples/toolconnector-usage.md) - Streaming tools

### Reference
- **[MCP Protocol](../docs/MCP-Protocol.md)** - Protocol specification
- **[Streaming Protocol](../docs/StreamingProtocol.md)** - Binary/text streaming
- **[JSON-RPC 2.0](../docs/JSON-RPC-2.0-spec.md)** - JSON-RPC standard

---

## 💡 Examples

### Auto-Named Tool (v1.2+)

```csharp
[McpTool]  // Name: "add_numbers"
public JsonRpcMessage AddNumbers(JsonRpcMessage request)
{
    var a = request.GetParams().GetProperty("a").GetInt32();
    var b = request.GetParams().GetProperty("b").GetInt32();
    return ToolResponse.Success(request.Id, new { result = a + b });
}
```

### Streaming Tool

```csharp
[McpTool("stream_data", 
    Capabilities = ToolCapabilities.BinaryStreaming)]
public async Task StreamData(ToolConnector connector)
{
    var handle = connector.OpenWrite(
        new StreamMessageMeta("stream_data", Binary: true));
    
    var data = File.ReadAllBytes("file.bin");
    await handle.WriteAsync(data);
    await handle.CompleteAsync(new { size = data.Length });
}
```

### With Dependency Injection

```csharp
[McpTool("get_user")]
public JsonRpcMessage GetUser(
    JsonRpcMessage request, 
    IUserRepository repo)  // ← Injected!
{
    var userId = request.GetParams().GetProperty("userId").GetInt32();
    var user = repo.GetById(userId);
    return ToolResponse.Success(request.Id, user);
}
```

---

## ⚠️ Tool Naming Rules

Tool names **MUST** match: `^[a-zA-Z0-9_-]{1,128}$`

### ✅ Valid
```csharp
✅ "ping"
✅ "add_numbers"
✅ "get-user-id"
✅ "my_tool_v2"
```

### ❌ Invalid
```csharp
❌ "system.ping"        // NO dots!
❌ "get current time"   // NO spaces
❌ "hello@world"        // NO special chars
```

**Why?** GitHub Copilot and MCP clients enforce strict validation.

**Fix:** Use underscores (`_`) or hyphens (`-`) instead.

---

## 🏗️ Architecture

```
ToolInvoker (JSON-RPC)
    ↓
ToolService (Discovery)
    ↓
Your Tools (Auto-registered)
```

### Key Components

| Component | Purpose |
|-----------|---------|
| **ToolInvoker** | Routes requests to tools |
| **ToolService** | Scans and registers tools |
| **ToolConnector** | Manages streaming |
| **JsonRpcMessage** | Type-safe messages |

---

## 🧪 Testing

```bash
dotnet test
```

**70+ tests** covering all transports and protocols.

---

## 📦 What's Included

| Class/Type | Purpose |
|------------|---------|
| `McpToolAttribute` | Mark methods as MCP tools |
| `ToolInvoker` | Core invocation logic |
| `ToolService` | Tool registration and discovery |
| `ToolConnector` | Streaming support |
| `JsonRpcMessage` | JSON-RPC 2.0 messages |
| `ToolResponse` | Helper for responses |

---

## 🤝 Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md)

---

## 📜 License

MIT © 2024 ARKo AS - AHelse Development Team

---

## 📞 Support

- **NuGet**: [Mcp.Gateway.Tools](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
- **Issues**: [GitHub](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues)
- **Docs**: [Full Documentation](../README.md)

---

**Version:** 1.2.0-dev  
**Last Updated:** 7. desember 2025
