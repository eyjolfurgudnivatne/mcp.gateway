---
layout: default
title: Getting Started with MCP Gateway
description: Quick start guide for building your first MCP server with MCP Gateway
---

# 🚀 Getting Started

Welcome to MCP Gateway! This guide will help you build your first MCP server in minutes.

## Prerequisites

- .NET 10 SDK
- Visual Studio 2022 / VS Code / Rider
- Basic C# knowledge

## Installation

### 1. Create a New Project

```bash
dotnet new web -n MyMcpServer
cd MyMcpServer
```

### 2. Install MCP Gateway

```bash
dotnet add package Mcp.Gateway.Tools --version 1.8.0
```

## Quick Start

### 1. Configure the Server

Update `Program.cs`:

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway services
builder.AddToolsService();

var app = builder.Build();

// stdio mode for GitHub Copilot (optional)
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// Enable WebSockets
app.UseWebSockets();

// MCP 2025-11-25 Streamable HTTP (recommended)
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

// Legacy endpoints (optional, deprecated)
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");

app.Run();
```

### 2. Define Your First Tool

Create `MyTools.cs`:

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

    [McpTool("add_numbers")]
    public JsonRpcMessage AddNumbers(TypedJsonRpc<AddParams> request)
    {
        var result = request.Params.A + request.Params.B;
        return ToolResponse.Success(
            request.Id,
            new { result });
    }
}

public record GreetParams(string Name);
public record AddParams(double A, double B);
```

### 3. Run Your Server

```bash
# HTTP mode
dotnet run

# stdio mode (for GitHub Copilot)
dotnet run -- --stdio
```

## Test Your Server

### Using curl

```bash
# List available tools
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 1
  }'

# Call a tool
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "greet",
      "arguments": {
        "Name": "World"
      }
    },
    "id": 2
  }'
```

### Using GitHub Copilot

1. Create `.mcp.json` (global or per workspace):

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

2. In GitHub Copilot Chat:

```
@my_server call greet with name = "Alice"
```

## Next Steps

Now that you have a working MCP server, explore more features:

- [📊 Lifecycle Hooks]({{ site.baseurl }}/features/lifecycle-hooks/) - Monitor tool invocations
- [📦 Resource Subscriptions]({{ site.baseurl }}/features/resource-subscriptions/) - Subscribe to resources
- [🔐 Authorization]({{ site.baseurl }}/features/authorization/) - Secure your tools
- [🔔 Notifications]({{ site.baseurl }}/features/notifications/) - Real-time updates

## Examples

Check out complete example servers:

- [Calculator Server]({{ site.baseurl }}/examples/calculator/) - Basic arithmetic operations
- [DateTime Server]({{ site.baseurl }}/examples/datetime/) - Date and time utilities
- [Metrics Server]({{ site.baseurl }}/examples/metrics/) - Lifecycle hooks with metrics
- [Authorization Server]({{ site.baseurl }}/examples/authorization/) - Role-based access control

## Troubleshooting

### Port Already in Use

Change the port in `appsettings.json`:

```json
{
  "Urls": "http://localhost:5001"
}
```

### Tools Not Discovered

Ensure your tool class is public and in the same assembly as `Program.cs`.

### stdio Mode Not Working

Check that GitHub Copilot is configured correctly in `.mcp.json` and the path is absolute.

## Need Help?

- [GitHub Issues](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues)
- [GitHub Discussions](https://github.com/eyjolfurgudnivatne/mcp.gateway/discussions)
- [API Reference]({{ site.baseurl }}/api-reference/)

---

**Ready to build more?** Check out our [Feature Guides]({{ site.baseurl }}/features/) for advanced topics!
