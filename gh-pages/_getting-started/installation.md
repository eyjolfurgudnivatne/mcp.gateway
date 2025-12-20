---
layout: mcp-default
title: Installation
description: Install MCP Gateway and set up your first project
breadcrumbs:
  - title: Home
    url: /
  - title: Getting Started
    url: /getting-started/index/
  - title: Installation
    url: /getting-started/installation/
toc: true
---

# Installation

Get started with MCP Gateway in minutes.

## Prerequisites

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Code editor** - Visual Studio 2025, Visual Studio 2022 v17.13+, VS Code, or Rider
- **Basic C# knowledge**

## Install via NuGet

### Using .NET CLI

```bash
dotnet add package Mcp.Gateway.Tools --version 1.8.0
```

### Using Package Manager Console

```powershell
Install-Package Mcp.Gateway.Tools -Version 1.8.0
```

### Using Visual Studio

1. Right-click project → **Manage NuGet Packages**
2. Search for `Mcp.Gateway.Tools`
3. Click **Install**

## Create New Project

### Quick Start (Recommended)

```bash
# Create new web project
dotnet new web -n MyMcpServer
cd MyMcpServer

# Install MCP Gateway
dotnet add package Mcp.Gateway.Tools

# Run
dotnet run
```

### From Template

```bash
# Install MCP Gateway template (coming soon!)
dotnet new install Mcp.Gateway.Templates

# Create from template
dotnet new mcp-server -n MyMcpServer
```

## Verify Installation

Create a simple server to verify everything works:

```csharp
// Program.cs
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway
builder.AddToolsService();

var app = builder.Build();

// Configure endpoints
app.UseWebSockets();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();

// Define your first tool
public class GreetingTools
{
    [McpTool("greet")]
    public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
    {
        var name = request.GetParams().Name;
        return ToolResponse.Success(
            request.Id,
            new { message = $"Hello, {name}!" });
    }
}

public record GreetParams(string Name);
```

Run the server:

```bash
dotnet run
```

Test with curl:

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "greet",
      "arguments": { "Name": "World" }
    },
    "id": 1
  }'
```

Expected response:

```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"message\":\"Hello, World!\"}"
      }
    ]
  },
  "id": 1
}
```

## Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Mcp.Gateway": "Debug"
    }
  },
  "Urls": "http://localhost:5000",
  "AllowedHosts": "*"
}
```

### launchSettings.json

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "stdio": {
      "commandName": "Project",
      "dotnetRunMessages": false,
      "launchBrowser": false,
      "commandLineArgs": "--stdio",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

## Troubleshooting

### Port Already in Use

Change the port in `appsettings.json`:

```json
{
  "Urls": "http://localhost:5001"
}
```

### Tools Not Discovered

Ensure your tool class is:
- **Public** class
- **Public** methods with `[McpTool]` attribute
- In the same assembly as `Program.cs`

### Package Not Found

Clear NuGet cache:

```bash
dotnet nuget locals all --clear
dotnet restore
```

## Protocol Version Compatibility

MCP Gateway **reports protocol version 2025-11-25** by default, but some clients don't support this yet.

### Why This Matters

**GitHub Copilot** (as of Dec 2025) expects `2025-06-18` and will fail to connect if server reports `2025-11-25`.

### Solution: Environment Variable

Set `MCP_PROTOCOL_VERSION` to advertise an older version:

**PowerShell:**
```powershell
$env:MCP_PROTOCOL_VERSION = "2025-06-18"
dotnet run
```

**Bash:**
```bash
export MCP_PROTOCOL_VERSION="2025-06-18"
dotnet run
```

**launchSettings.json:**
```json
{
  "profiles": {
    "stdio": {
      "commandName": "Project",
      "commandLineArgs": "--stdio",
      "environmentVariables": {
        "MCP_PROTOCOL_VERSION": "2025-06-18"
      }
    }
  }
}
```

**Important:** This only changes what version is **reported** in the `initialize` response. The server still **accepts** all versions: `2025-11-25`, `2025-06-18`, `2025-03-26`.

### Supported Versions

| Version | Status | Clients |
|---------|--------|---------|
| `2025-11-25` | Latest (default) | Future MCP clients |
| `2025-06-18` | Stable | GitHub Copilot, Claude Desktop |
| `2025-03-26` | Legacy | Older MCP clients |

**Tip:** Use `2025-06-18` for maximum compatibility with current MCP clients.

## Next Steps

Now that you have MCP Gateway installed:

1. **[Create your first tool](/mcp.gateway/getting-started/first-tool/)** - Learn tool development
2. **[Lifecycle Hooks](/mcp.gateway/features/lifecycle-hooks/)** - Monitor tool invocations
3. **[Calculator Example](/mcp.gateway/examples/calculator/)** - Complete working server

## See Also

- [Getting Started Overview](/mcp.gateway/getting-started/index/) - Overview and quick start
- [Your First Tool](/mcp.gateway/getting-started/first-tool/) - Build a complete tool step-by-step
- Examples - [Calculator](/mcp.gateway/examples/calculator/), [DateTime](/mcp.gateway/examples/datetime/), [Metrics](/mcp.gateway/examples/metrics/)

## IDE Extensions

### Visual Studio 2022

- **REST Client** - Test endpoints directly in VS

### VS Code

- **C# Dev Kit** - Official C# extension
- **REST Client** - Test HTTP endpoints
- **Thunder Client** - Alternative API testing

### Rider

Built-in MCP support (coming soon!)

## System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| .NET SDK | 10.0 | 10.0 |
| RAM | 2 GB | 4 GB |
| Disk | 500 MB | 1 GB |
| OS | Windows 10, Linux, macOS | Latest |
