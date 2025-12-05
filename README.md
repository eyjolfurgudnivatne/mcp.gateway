# 🚀 MCP Gateway

> Production-ready Model Context Protocol (MCP) Gateway library for .NET 10

[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--06--18-green)](https://modelcontextprotocol.io/)
[![C# 14.0](https://img.shields.io/badge/C%23-14.0-blue)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

**MCP Gateway** is a high-performance, extensible library for building Model Context Protocol (MCP) servers in .NET. It enables AI assistants like GitHub Copilot and Claude Desktop to discover and invoke custom tools. The library provides a complete framework for JSON-RPC, MCP protocol implementation, and multiple transport protocols.

---

## 🎯 Quick Start

### 1. Use the example server

```powershell
# Clone repository
git clone https://github.com/eyjolfurgudnivatne/mcp.gateway.git
cd mcp.gateway

# Build and run example server
dotnet run --project Mcp.Gateway.Server

# Server starts on http://localhost:5000
```

### 2. Test with curl

```bash
# Discover available tools
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'

# Invoke a tool
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"tools/call",
    "id":2,
    "params":{
      "name":"add_numbers",
      "arguments":{"number1":5,"number2":3}
    }
  }'
```

### 3. Integrate with GitHub Copilot

Create `.mcp.json` in `%APPDATA%\Code\User\globalStorage\github.copilot-chat\`:

```json
{
  "mcpServers": {
    "mcp_gateway": {
      "command": "C:\\path\\to\\Mcp.Gateway.GCCServer.exe",
      "args": ["--stdio"],
      "env": {}
    }
  }
}
```

**Use in Copilot Chat:**
```
@mcp_gateway add 5 and 3
@mcp_gateway what is 4 times 3?
```

---

## ✨ Features

### 🔌 Full MCP Protocol Support
- **Protocol Version**: 2025-06-18
- **Methods**: `initialize`, `tools/list`, `tools/call`
- **JSON Schema**: Input validation with JSON Schema
- **Error Handling**: JSON-RPC 2.0 compliant error codes
- **Batch Requests**: Process multiple requests in single call

### 🌐 Multiple Transport Protocols
- **HTTP**: Standard JSON-RPC over HTTP POST (`/rpc`)
- **WebSocket**: Full-duplex streaming (`/ws`)
- **SSE**: Server-Sent Events for remote clients (`/sse`)
- **stdio**: Standard input/output for local integration (GitHub Copilot)

### 📡 Streaming Support
- **Binary Streaming**: Efficient transfer of large files
- **Text Streaming**: JSON-based streaming
- **Duplex Streaming**: Bi-directional communication
- **ToolConnector API**: High-level abstraction for streaming

### 🛠️ Developer-Friendly
- **Auto-Discovery**: Tools discovered via `[McpTool]` attribute
- **Type-Safe**: Strong typing with C# records
- **Dependency Injection**: Full ASP.NET Core DI support
- **Testable**: 100% test coverage with xUnit

### ⚡ Production-Ready
- **Performance**: Optimized for low-latency (<10ms)
- **Reliability**: Comprehensive error handling
- **Security**: Input validation and sanitization
- **Extensible**: Easy to add custom tools and transports

---

## 🏗️ Architecture Overview

```
┌──────────────────────────────────────────────────────────┐
│                      MCP Clients                         │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐           │
│  │  GitHub   │  │   Claude  │  │  Custom   │           │
│  │  Copilot  │  │  Desktop  │  │  Client   │           │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘           │
└────────┼──────────────┼──────────────┼──────────────────┘
         │              │              │
         │ stdio        │ SSE          │ HTTP/WS
         │              │              │
┌────────▼──────────────▼──────────────▼──────────────────┐
│         Your MCP Server (ASP.NET Core)                   │
│  ┌──────────────────────────────────────────────────┐   │
│  │             Transport Layer                      │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐   │   │
│  │  │  HTTP  │ │   WS   │ │  SSE   │ │ stdio  │   │   │
│  │  │  /rpc  │ │  /ws   │ │ /sse   │ │        │   │   │
│  │  └───┬────┘ └───┬────┘ └───┬────┘ └───┬────┘   │   │
│  └──────┼──────────┼──────────┼──────────┼─────────┘   │
│         │          │          │          │              │
│  ┌──────▼──────────▼──────────▼──────────▼─────────┐   │
│  │     Mcp.Gateway.Tools (Core Library)            │   │
│  │  ┌───────────────────────────────────────────┐  │   │
│  │  │ ToolInvoker - Core Logic                  │  │   │
│  │  │  - JSON-RPC parsing                       │  │   │
│  │  │  - MCP protocol implementation            │  │   │
│  │  │  - Tool invocation                        │  │   │
│  │  │  - Error handling                         │  │   │
│  │  └──────────────────┬────────────────────────┘  │   │
│  │                     │                            │   │
│  │  ┌──────────────────▼────────────────────────┐  │   │
│  │  │ ToolService - Discovery                   │  │   │
│  │  │  - Scans assemblies for [McpTool]        │  │   │
│  │  │  - Validates tool names                   │  │   │
│  │  │  - Manages tool metadata                  │  │   │
│  │  └───────────────────────────────────────────┘  │   │
│  └──────────────────────────────────────────────────┘   │
│                     │                                    │
│  ┌──────────────────▼──────────────────────────────┐   │
│  │         Your Custom Tool Implementations         │   │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐        │   │
│  │  │Calculator│ │  System  │ │ Streaming│        │   │
│  │  │  Tools   │ │  Tools   │ │  Tools   │        │   │
│  │  └──────────┘ └──────────┘ └──────────┘        │   │
│  └──────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
```

### Data Flow

```
1. Client Request (any transport)
   ↓
2. ToolInvoker receives JSON-RPC
   ↓
3. Parse and validate request
   ↓
4. MCP protocol handler?
   ├─ Yes: Handle initialize/tools/list/tools/call
   └─ No: Continue to tool invocation
   ↓
5. ToolService discovers tool
   ↓
6. Validate input against JSON Schema
   ↓
7. Invoke tool delegate
   ↓
8. Process result (sync/async/streaming)
   ↓
9. Wrap in JSON-RPC response
   ↓
10. Send back via transport
```

---

## 📦 Projects

| Project | Type | Purpose |
|---------|------|---------|
| **Mcp.Gateway.Tools** | 📚 Library | Core framework - Tool discovery, JSON-RPC, MCP protocol, ToolInvoker |
| **Mcp.Gateway.Server** | 📖 Example | Full-featured reference implementation with all transports |
| **Mcp.Gateway.GCCServer** | 📖 Example | Minimal GitHub Copilot integration example |
| **Mcp.Gateway.Tests** | 🧪 Tests | 45+ tests covering all protocols and transports |

---

## 🚀 Installation

### Option 1: Install from NuGet ✨

```bash
dotnet add package Mcp.Gateway.Tools
```

**Or via Package Manager Console:**
```powershell
Install-Package Mcp.Gateway.Tools
```

### Option 2: Build from Source

```bash
# Clone repository
git clone https://github.com/eyjolfurgudnivatne/mcp.gateway.git
cd mcp.gateway

# Build library
dotnet build Mcp.Gateway.Tools

# Reference in your project
<ProjectReference Include="..\Mcp.Gateway.Tools\Mcp.Gateway.Tools.csproj" />
```

### Option 3: Run Example Servers

```powershell
# Build and run full example server
dotnet run --project Mcp.Gateway.Server

# Publish GitHub Copilot example
dotnet publish Mcp.Gateway.GCCServer `
  -c Release `
  -r win-x64 `
  --self-contained `
  -p:PublishSingleFile=true `
  -o publishGCC

.\publishGCC\Mcp.Gateway.GCCServer.exe --stdio
```

---

## 💡 Building Your Own MCP Server

### 1. Create new ASP.NET Core project

```bash
dotnet new web -n MyMcpServer
cd MyMcpServer
dotnet add package Mcp.Gateway.Tools
```

### 2. Setup Program.cs

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Add MCP Gateway Tools
builder.AddToolsService();

var app = builder.Build();

// Check for stdio mode (for GitHub Copilot, Claude Desktop, etc.)
var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    // Run in stdio mode for local MCP clients
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// Enable WebSockets for streaming
app.UseWebSockets();

// Map endpoints (HTTP, WebSocket, SSE)
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");

app.Run();
```

### 3. Create your first tool

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("greet_user",
        Description = "Greets a user by name",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""name"":{""type"":""string"",""description"":""User's name""}
            },
            ""required"":[""name""]
        }")]
    public async Task<JsonRpcMessage> GreetUser(JsonRpcMessage request)
    {
        var name = request.GetParams().GetProperty("name").GetString();
        var greeting = $"Hello, {name}!";
        return ToolResponse.Success(request.Id, new { message = greeting });
    }
}
```

### 4. Run your server

```bash
dotnet run
```

That's it! Your MCP server is now running with full protocol support. 🎉

---

## 💡 Usage Examples

### HTTP JSON-RPC

```bash
# Initialize protocol
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"initialize",
    "id":1,
    "params":{
      "protocolVersion":"2025-06-18",
      "clientInfo":{"name":"curl","version":"1.0"}
    }
  }'

# List tools
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":2}'

# Call tool
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"tools/call",
    "id":3,
    "params":{
      "name":"add_numbers",
      "arguments":{"number1":10,"number2":5}
    }
  }'
```

### WebSocket

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');

ws.onopen = () => {
  // Initialize
  ws.send(JSON.stringify({
    jsonrpc: "2.0",
    method: "initialize",
    id: 1,
    params: {
      protocolVersion: "2025-06-18",
      clientInfo: { name: "browser-client", version: "1.0" }
    }
  }));
};

ws.onmessage = (event) => {
  const response = JSON.parse(event.data);
  console.log("Response:", response);
};

// List tools
ws.send(JSON.stringify({
  jsonrpc: "2.0",
  method: "tools/list",
  id: 2
}));
```

### SSE (Server-Sent Events)

```bash
# Useful for remote MCP clients (Claude Desktop, etc.)
curl -X POST http://localhost:5000/sse \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
```

**Claude Desktop Configuration** (`.mcp.json`):
```json
{
  "mcpServers": {
    "remote_mcp": {
      "url": "https://your-domain.com/sse",
      "transport": "sse"
    }
  }
}
```

### stdio (GitHub Copilot)

**Configuration** (`%APPDATA%\Code\User\globalStorage\github.copilot-chat\.mcp.json`):
```json
{
  "mcpServers": {
    "mcp_gcc": {
      "command": "C:\\publish\\Mcp.Gateway.GCCServer.exe",
      "args": ["--stdio"],
      "env": {}
    }
  }
}
```

**Development/Testing Configuration** (workspace `.mcp.json`):
```json
{
  "inputs": [],
  "servers": {
    "gcc_server_test_stdio": {
      "type": "stdio",
      "command": "Mcp.Gateway.GCCServer\\bin\\Debug\\net10.0\\Mcp.Gateway.GCCServer.exe",
      "args": ["--stdio"]
    }
  }
}
```

**Usage in VS Code:**
```
User: @mcp_gcc add 5 and 3
Copilot: The result is 8.

User: @mcp_gcc multiply 4 and 7
Copilot: The result is 28.
```

---

## 🛠️ Creating Custom Tools

### Basic Tool

```csharp
using Mcp.Gateway.Tools;

[McpTool("greet_user",
    Description = "Greets a user by name",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""name"":{""type"":""string"",""description"":""User's name""}
        },
        ""required"":[""name""]
    }")]
public async Task<JsonRpcMessage> GreetUser(JsonRpcMessage request)
{
    var name = request.GetParams().GetProperty("name").GetString();
    var greeting = $"Hello, {name}!";
    return ToolResponse.Success(request.Id, new { message = greeting });
}
```

### Streaming Tool

```csharp
[McpTool("stream_data")]
public async Task StreamData(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "stream_data",
        Binary: false);

    var handle = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);

    for (int i = 0; i < 10; i++)
    {
        await handle.WriteChunkAsync(new { index = i, data = $"Chunk {i}" });
        await Task.Delay(100);
    }

    await handle.CompleteAsync(new { totalChunks = 10 });
}
```

### Tool with Dependency Injection

```csharp
public class MyService
{
    public string ProcessData(string input) => input.ToUpper();
}

[McpTool("process_text")]
public async Task<JsonRpcMessage> ProcessText(
    JsonRpcMessage request,
    MyService service) // Injected automatically
{
    var input = request.GetParams().GetProperty("text").GetString();
    var result = service.ProcessData(input);
    return ToolResponse.Success(request.Id, new { result });
}
```

**Register service:**
```csharp
builder.Services.AddScoped<MyService>();
```

---

## 📚 Documentation

### Core Documentation
- **[MCP Protocol Implementation](docs/MCP-Protocol.md)** - Full MCP protocol specification and implementation details
- **[Streaming Protocol v1.0](docs/StreamingProtocol.md)** - WebSocket streaming protocol with binary support
- **[JSON-RPC 2.0 Specification](docs/JSON-RPC-2.0-spec.md)** - JSON-RPC standard reference

### Guides
- **[Tool Creation Guide](Mcp.Gateway.Tools/README.md)** - How to create custom tools
- **[GitHub Copilot Integration](docs/MCP-Protocol.md#-github-copilot-integration)** - Configure GitHub Copilot with MCP Gateway

### Example Implementations
- **Calculator Tools**: `Mcp.Gateway.GCCServer/Tools/Calculator.cs` - Basic arithmetic
- **System Tools**: `Mcp.Gateway.Server/Tools/Systems/` - Ping, echo, streaming
- **Secret Generator**: `Mcp.Gateway.GCCServer/Tools/SecretGenerator.cs` - Random secrets
- **Full Server**: `Mcp.Gateway.Server/` - Complete reference implementation

---

## 🧪 Testing

### Run All Tests

```bash
# All tests
dotnet test

# Specific test categories
dotnet test --filter "FullyQualifiedName~McpProtocolTests"
dotnet test --filter "FullyQualifiedName~BinaryStreams"
dotnet test --filter "FullyQualifiedName~StdioProtocolTests"
```

### Test Coverage

✅ **45+ tests** covering:
- MCP protocol methods (`initialize`, `tools/list`, `tools/call`)
- All transports (HTTP, WebSocket, SSE, stdio)
- Binary streaming (in, out, duplex)
- Error handling
- Tool discovery and validation
- GitHub Copilot integration

### Manual Testing

```bash
# Health check (example server)
curl http://localhost:5000/health

# Ping tool (example server)
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"system_ping","id":1}'

# Echo tool (example server)
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"system_echo",
    "id":1,
    "params":{"message":"Hello, MCP!"}
  }'
```

---

## 🔧 Configuration

### Server Configuration

**appsettings.json** (example servers):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000"
}
```

### stdio Logging

When running in stdio mode, logs are written to:
```
%LOCALAPPDATA%\MCP-Gateway\stdio-{timestamp}.log
```

This prevents log output from interfering with JSON-RPC communication.

---

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Development Setup

```bash
# Clone
git clone https://github.com/eyjolfurgudnivatne/mcp.gateway.git
cd mcp.gateway

# Restore dependencies
dotnet restore

# Build library
dotnet build Mcp.Gateway.Tools

# Run tests
dotnet test

# Run example server
dotnet run --project Mcp.Gateway.Server
```

### Code Style
- Follow C# 14.0 / .NET 10 conventions
- Use XML documentation comments
- Maintain test coverage
- Follow existing patterns

---

## 📈 Roadmap

### v1.0.1 (Current) ✅
- ✅ Full MCP Protocol 2025-06-18
- ✅ HTTP, WebSocket, SSE, stdio transports
- ✅ Binary streaming
- ✅ GitHub Copilot integration
- ✅ 45+ tests (100% passing)
- ✅ Example servers (Server, GCCServer)
- ✅ **NuGet package published**
- ✅ **Performance optimizations:**
  - ArrayPool for WebSocket buffers (90% GC reduction)
  - SerializeToUtf8Bytes optimization (production throughput)

### v1.1 (Planned)
- 🔜 More example tools
- 🔜 Parameter caching (proper design)
- 🔜 Additional documentation
- 🔜 GitHub Actions automation

### v2.0 (Future)
- 🔮 **Hybrid Tool API** - Simplified tool authoring (see [Hybrid Tool API Plan](docs/HybridToolAPI-Plan.md))
- 🔮 **MCP Resources support** - Full implementation of MCP Resources specification
- 🔮 **MCP Prompts support** - Full implementation of MCP Prompts specification
- 🔮 **JSON Source Generators** - Hybrid approach for performance
- 🔮 **Tool lifecycle hooks** - Events for monitoring tool invocations (opt-in)
- 🔮 **Custom transport providers** - Extensibility API for custom transports
- 🔮 **Enhanced streaming** - Compression, flow control, multiplexing

---

## 📜 License

MIT License

Copyright (c) 2024 ARKo AS - AHelse Development Team

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---

## 🙏 Acknowledgments

**Built by:** ARKo AS - AHelse Development Team

**Special thanks to:**
- **Anthropic** - For creating the MCP specification
- **GitHub** - For Copilot and MCP client support
- **Microsoft** - For .NET 10 and ASP.NET Core

---

## 📞 Support

- **NuGet Package**: [Mcp.Gateway.Tools](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
- **GitHub Issues**: [Report bugs or request features](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues)
- **Documentation**: [Full docs](docs/)
- **Examples**: See `Mcp.Gateway.Server` and `Mcp.Gateway.GCCServer` for reference implementations

---

**Built with ❤️ using .NET 10 and C# 14.0**

**Version:** 1.0.1  
**Last Updated:** 5. desember 2025  
**License:** MIT  
**NuGet:** [Mcp.Gateway.Tools](https://www.nuget.org/packages/Mcp.Gateway.Tools/)

