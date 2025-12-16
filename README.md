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

## 💡 Features

- ✅ **MCP 2025‑06‑18** – up to date with the current MCP specification
- ✅ **Transports** – HTTP (`/rpc`), WebSocket (`/ws`), SSE (`/sse`), stdio
- ✅ **Auto‑discovery** – tools discovered via `[McpTool]`
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
- ✅ **Streaming** – text and binary streaming via `ToolConnector`
- ✅ **DI support** – tools can take services as parameters
- ✅ **Tested** – 70+ tests covering HTTP, WS, SSE and stdio

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

