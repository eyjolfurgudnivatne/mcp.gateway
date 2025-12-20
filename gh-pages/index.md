---
layout: mcp-default
title: MCP Gateway
description: Production-ready Model Context Protocol (MCP) Gateway for .NET 10
---

# MCP Gateway

> Production-ready Model Context Protocol (MCP) Gateway for .NET 10

Build MCP servers that work with **GitHub Copilot**, **Claude Desktop**, and other AI assistants.

## Quick Start

```bash
# Install package
dotnet add package Mcp.Gateway.Tools --version 1.8.0

# Create server
dotnet new web -n MyMcpServer
```

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.AddToolsService();

var app = builder.Build();
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");
app.Run();

// Define your first tool
public class MyTools
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

**That's it!** Your MCP server is ready.

## Features

### ⚡ Quick to Get Started
Create your first MCP server in minutes with our simple API and comprehensive examples.

### 📊 Production Ready
Built-in lifecycle hooks, metrics, authorization, and monitoring for production deployments.

### 🔌 Multiple Transports
Support for HTTP, WebSocket, SSE, and stdio transports. Works with all MCP clients.

### 🎯 MCP 2025-11-25 Compliant
100% compliant with the latest MCP specification. Streamable HTTP, SSE notifications, session management.

### 📦 Resource Subscriptions
Optional MCP feature for targeted notifications. Subscribe to specific resources, reduce bandwidth.

### 📊 Lifecycle Hooks
Monitor tool invocations with built-in metrics, logging, and authorization support.

### 🔐 Authorization
Role-based access control via lifecycle hooks. Declarative security with attributes.

### 🧪 273 Tests
Comprehensive test coverage. All transports tested (HTTP, WebSocket, SSE, stdio).

## Installation

Install via NuGet:

```bash
dotnet add package Mcp.Gateway.Tools
```

Or clone from source:

```bash
git clone https://github.com/eyjolfurgudnivatne/mcp.gateway
cd mcp.gateway
dotnet build
```

## Connect from MCP Clients

### GitHub Copilot

Create `.mcp.json`:

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

Then in Copilot Chat:

```
@my_server call greet with name = "Alice"
```

### Claude Desktop

Configure HTTP endpoint:

```json
{
  "mcpServers": {
    "my_server": {
      "transport": "http",
      "url": "https://your-server.example.com/mcp"
    }
  }
}
```

## Next Steps

<div class="next-steps">
  <a href="{{ site.baseurl }}/getting-started/" class="next-step-card">
    <div class="card-icon">📚</div>
    <div class="card-title">Getting Started</div>
    <div class="card-description">Learn the basics and create your first MCP server</div>
  </a>

  <a href="{{ site.baseurl }}/features/lifecycle-hooks/" class="next-step-card">
    <div class="card-icon">📊</div>
    <div class="card-title">Lifecycle Hooks</div>
    <div class="card-description">Monitor and track tool invocations in production</div>
  </a>

  <a href="{{ site.baseurl }}/examples/calculator/" class="next-step-card">
    <div class="card-icon">💻</div>
    <div class="card-title">Examples</div>
    <div class="card-description">Explore complete example servers and patterns</div>
  </a>
</div>

<style>
.next-steps {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1rem;
  margin: 2rem 0;
}

.next-step-card {
  padding: 1.5rem;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  text-decoration: none;
  color: inherit;
  transition: all 150ms;
}

.next-step-card:hover {
  border-color: var(--accent-color);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  transform: translateY(-2px);
}

.card-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.card-title {
  font-size: 1.1rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: var(--text-primary);
}

.card-description {
  font-size: 0.9rem;
  color: var(--text-secondary);
}
</style>
