# 🚀 MCP Gateway

> Build MCP servers in .NET 10 - Production-ready in 5 minutes

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![Tests](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml/badge.svg)](https://github.com/eyjolfurgudnivatne/mcp.gateway/actions/workflows/test.yml)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--06--18-green)](https://modelcontextprotocol.io/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**MCP Gateway** enables AI assistants like **GitHub Copilot** and **Claude Desktop** to discover and invoke your custom tools. High-performance, extensible library for building Model Context Protocol (MCP) servers in .NET.

---

## ⚡ Quick Start

### 1. Install Package

```bash
dotnet new web -n MyMcpServer
cd MyMcpServer
dotnet add package Mcp.Gateway.Tools
```

### 2. Setup Server (`Program.cs`)

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.AddToolsService();

var app = builder.Build();

// Support stdio for GitHub Copilot
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

### 3. Create Your First Tool

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

### 4. Run & Test

```bash
dotnet run

# Test
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

**That's it!** 🎉 Your MCP server is running.

---

## 🔌 Connect to AI Assistants

### GitHub Copilot

Create `.mcp.json` in `%APPDATA%\Code\User\globalStorage\github.copilot-chat\`:

```json
{
  "mcpServers": {
    "my_server": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\path\\to\\MyMcpServer", "--", "--stdio"]
    }
  }
}
```

**Use in VS Code:**
```
@my_server greet me with name "Alice"
```

### Claude Desktop

```json
{
  "mcpServers": {
    "my_server": {
      "url": "https://your-server.com/rpc",
      "transport": "http"
    }
  }
}
```

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
[McpTool("stream_data")]
public async Task StreamData(ToolConnector connector)
{
    var handle = connector.OpenWrite(new StreamMessageMeta("stream_data", Binary: false));
    
    for (int i = 0; i < 10; i++)
        await handle.WriteChunkAsync(new { chunk = i });
    
    await handle.CompleteAsync(new { done = true });
}
```

### With Dependency Injection

```csharp
[McpTool("process")]
public JsonRpcMessage Process(JsonRpcMessage request, MyService service)
{
    var result = service.DoWork(request.GetParams());
    return ToolResponse.Success(request.Id, result);
}
```

**Register service:**
```csharp
builder.Services.AddScoped<MyService>();
```

---

## ✨ Features

- ✅ **Auto-discovery** - Tools found via `[McpTool]` attribute
- ✅ **Multiple transports** - HTTP, WebSocket, SSE, stdio
- ✅ **Streaming** - Binary and text streaming support
- ✅ **Type-safe** - Full C# type checking
- ✅ **DI support** - ASP.NET Core dependency injection
- ✅ **Production-ready** - 70+ tests, optimized performance

---

## 📚 Documentation

### Getting Started
- **[Complete Guide](docs/MCP-Protocol.md)** - Full walkthrough and protocol details
- **[Tool Creation](Mcp.Gateway.Tools/README.md)** - How to create tools
- **[Code Examples](docs/examples/)** - Real-world examples
  - [Client Examples](docs/examples/client-examples.md) - HTTP, WebSocket, SSE clients
  - [ToolConnector Usage](docs/examples/toolconnector-usage.md) - Streaming tools

### Reference
- **[MCP Protocol](docs/MCP-Protocol.md)** - Protocol specification (2025-06-18)
- **[Streaming Protocol](docs/StreamingProtocol.md)** - Binary/text streaming
- **[JSON-RPC 2.0](docs/JSON-RPC-2.0-spec.md)** - JSON-RPC standard

### Examples
- **Calculator** - `Mcp.Gateway.GCCServer/Tools/Calculator.cs`
- **System Tools** - `Mcp.Gateway.Server/Tools/Systems/`
- **Full Server** - `Mcp.Gateway.Server/Program.cs`

---

## 🏗️ Architecture

```
MCP Clients (Copilot, Claude)
         ↓
  Transport (stdio/HTTP/WS/SSE)
         ↓
    ToolInvoker (JSON-RPC)
         ↓
    ToolService (Discovery)
         ↓
   Your Custom Tools
```

**See [MCP Protocol](docs/MCP-Protocol.md) for detailed architecture.**

---

## 🧪 Testing

```bash
# All tests
dotnet test

# Specific transport
dotnet test --filter "FullyQualifiedName~Http"
```

**70+ tests** covering all transports and protocols.

---

## 🚀 Try Example Server

```bash
git clone https://github.com/eyjolfurgudnivatne/mcp.gateway.git
cd mcp.gateway
dotnet run --project Mcp.Gateway.Server

# Test: http://localhost:5000/rpc
```

---

## 🤝 Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) - We welcome contributions!

---

## 📦 Projects

| Project | Purpose |
|---------|---------|
| **Mcp.Gateway.Tools** | Core library (NuGet package) |
| **Mcp.Gateway.Server** | Full example server |
| **Mcp.Gateway.GCCServer** | GitHub Copilot example |
| **Mcp.Gateway.Tests** | Test suite (70+ tests) |

---

## 📈 Roadmap

### v1.2.0 (In Development)
- ✅ **Transport filtering** - Tools filtered by transport capabilities
- 🔜 **Ollama provider** - Local LLM integration

### v2.0 (Planned)
- 🔮 **MCP Resources** - Full Resources support
- 🔮 **MCP Prompts** - Full Prompts support
- 🔮 **Hybrid Tool API** - Simplified tool authoring

**See [full roadmap](.internal/notes/v.1.2.0/README.md) for details.**

---

## 📜 License

MIT © 2024 ARKo AS - AHelse Development Team

---

## 📞 Support

- **NuGet**: [Mcp.Gateway.Tools](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
- **Issues**: [GitHub Issues](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues)
- **Docs**: [Full Documentation](docs/)

---

**Built with ❤️ using .NET 10 and C# 14.0**

**Version:** 1.2.0-dev  
**Last Updated:** 7. desember 2025  
**License:** MIT  

