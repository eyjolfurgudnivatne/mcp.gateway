---
layout: mcp-default
title: Getting Started with MCP Gateway
description: Get started with MCP Gateway in minutes - installation, first tool, and examples
breadcrumbs:
  - title: Home
    url: /
  - title: Getting Started
    url: /getting-started/
toc: true
---

# Getting Started

Build your first MCP server with MCP Gateway in minutes.

## What You'll Learn

By following this guide, you'll learn how to:
- ✅ Install MCP Gateway via NuGet
- ✅ Create your first MCP server
- ✅ Define tools with typed parameters
- ✅ Test with curl and GitHub Copilot
- ✅ Handle errors and validate input
- ✅ Deploy to production

**Time required:** ~15 minutes

## Prerequisites

Before you begin, make sure you have:
- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Code editor** - Visual Studio 2022, VS Code, or Rider
- **Basic C# knowledge** - Familiarity with C# syntax
- **Command line** - Basic terminal/PowerShell knowledge

## Quick Start

### 1. Install MCP Gateway

Create a new project and install the package:

```bash
# Create new web project
dotnet new web -n MyMcpServer
cd MyMcpServer

# Install MCP Gateway
dotnet add package Mcp.Gateway.Tools --version 1.8.0
```

### 2. Create Your First Tool

Update `Program.cs`:

```csharp
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway
builder.AddToolsService();

var app = builder.Build();

// stdio mode for GitHub Copilot
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// HTTP mode for testing
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();

// Define your first tool
public class GreetingTools
{
    [McpTool("greet", Description = "Greets a user by name")]
    public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException("Name is required");

        var greeting = $"Hello, {args.Name}! Welcome to MCP Gateway!";
        
        return ToolResponse.Success(
            request.Id,
            new { message = greeting });
    }
}

public record GreetParams(string Name);
```

### 3. Run Your Server

```bash
dotnet run
```

Server runs at: `http://localhost:5000/mcp`

### 4. Test with curl

```bash
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
    "id": 1
  }'
```

**Expected response:**

```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"message\":\"Hello, World! Welcome to MCP Gateway!\"}"
      }
    ]
  },
  "id": 1
}
```

## What's Next?

### Step-by-Step Guides

<div class="guide-cards">
  <a href="/mcp.gateway/getting-started/installation/" class="guide-card">
    <div class="guide-icon">📦</div>
    <h3>Installation</h3>
    <p>Detailed installation guide, configuration, and troubleshooting</p>
  </a>

  <a href="/mcp.gateway/getting-started/first-tool/" class="guide-card">
    <div class="guide-icon">🔧</div>
    <h3>Your First Tool</h3>
    <p>Build a complete tool step-by-step with validation and error handling</p>
  </a>
</div>

### Explore Features

<div class="feature-cards">
  <a href="/mcp.gateway/features/lifecycle-hooks/" class="feature-card">
    <div class="feature-icon">📊</div>
    <h3>Lifecycle Hooks</h3>
    <p>Monitor tool invocations with metrics and logging</p>
  </a>

  <a href="/mcp.gateway/features/authorization/" class="feature-card">
    <div class="feature-icon">🔐</div>
    <h3>Authorization</h3>
    <p>Implement role-based access control</p>
  </a>

  <a href="/mcp.gateway/features/resource-subscriptions/" class="feature-card">
    <div class="feature-icon">📦</div>
    <h3>Resource Subscriptions</h3>
    <p>Real-time notifications for resource updates</p>
  </a>
</div>

### Browse Examples

<div class="example-cards">
  <a href="/mcp.gateway/examples/calculator/" class="example-card">
    <div class="example-icon">🧮</div>
    <h3>Calculator Server</h3>
    <p>Basic arithmetic operations with error handling</p>
  </a>

  <a href="/mcp.gateway/examples/metrics/" class="example-card">
    <div class="example-icon">📈</div>
    <h3>Metrics Server</h3>
    <p>Production-ready metrics and monitoring</p>
  </a>

  <a href="/mcp.gateway/examples/authorization/" class="example-card">
    <div class="example-icon">🔒</div>
    <h3>Authorization Server</h3>
    <p>Role-based access control with JWT</p>
  </a>
</div>

## Key Concepts

### Tools

Tools are the core of MCP servers. They allow AI assistants to:
- Execute custom logic
- Access external APIs
- Manipulate data
- Perform calculations

**Learn more:** [Tools API Reference](/mcp.gateway/api/tools/)

### TypedJsonRpc<T>

Type-safe parameter handling with automatic deserialization:

```csharp
public JsonRpcMessage Greet(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams();  // Strongly typed!
    // args.Name is a string
}
```

### Error Handling

Use `ToolInvalidParamsException` for invalid input:

```csharp
if (string.IsNullOrWhiteSpace(args.Name))
{
    throw new ToolInvalidParamsException("Name cannot be empty");
}
```

## GitHub Copilot Integration

Configure GitHub Copilot to use your server:

### 1. Create `.mcp.json`

In your home directory or workspace:

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

**Important:** Use absolute paths!

### 2. Use in Copilot Chat

```
@my_server greet Alice
@my_server help me with...
```

## Common Questions

### How do I add more tools?

Just add more methods with `[McpTool]` attribute:

```csharp
[McpTool("add_numbers")]
public JsonRpcMessage Add(TypedJsonRpc<AddParams> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, 
        new { result = args.A + args.B });
}
```

### How do I handle async operations?

Use `async` methods:

```csharp
[McpTool("fetch_data")]
public async Task<JsonRpcMessage> FetchData(JsonRpcMessage request)
{
    var data = await _httpClient.GetStringAsync("...");
    return ToolResponse.Success(request.Id, new { data });
}
```

### How do I add logging?

Use dependency injection:

```csharp
public class MyTools
{
    private readonly ILogger<MyTools> _logger;
    
    public MyTools(ILogger<MyTools> logger)
    {
        _logger = logger;
    }
    
    [McpTool("my_tool")]
    public JsonRpcMessage MyTool(JsonRpcMessage request)
    {
        _logger.LogInformation("Tool invoked");
        // ...
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

## Need Help?

- 📚 [API Reference](/mcp.gateway/api/tools/) - Complete API documentation
- 💻 [Examples](/mcp.gateway/examples/) - Working example servers
- 🐛 [GitHub Issues](https://github.com/eyjolfurgudnivatne/mcp.gateway/issues) - Report bugs
- 💬 [GitHub Discussions](https://github.com/eyjolfurgudnivatne/mcp.gateway/discussions) - Ask questions

## Next Steps

Ready to dive deeper? Here's what to explore next:

1. **[Installation Guide](/mcp.gateway/getting-started/installation/)** - Detailed setup and configuration
2. **[Build Your First Tool](/mcp.gateway/getting-started/first-tool/)** - Complete step-by-step tutorial
3. **[Features Overview](/mcp.gateway/features/)** - Explore lifecycle hooks, authorization, and more
4. **[Examples](/mcp.gateway/examples/)** - Learn from complete working servers
5. **[API Reference](/mcp.gateway/api/tools/)** - Deep dive into the API

<style>
.guide-cards,
.feature-cards,
.example-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin: 2rem 0;
}

.guide-card,
.feature-card,
.example-card {
  padding: 1.5rem;
  border: 1px solid var(--border-color);
  border-radius: 8px;
  text-decoration: none;
  color: inherit;
  transition: all 0.3s;
  background: var(--bg-secondary);
}

.guide-card:hover,
.feature-card:hover,
.example-card:hover {
  border-color: var(--accent-color);
  box-shadow: 0 4px 12px rgba(0,0,0,0.1);
  transform: translateY(-2px);
}

.guide-icon,
.feature-icon,
.example-icon {
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.guide-card h3,
.feature-card h3,
.example-card h3 {
  margin: 0.5rem 0;
  font-size: 1.2rem;
  color: var(--text-primary);
}

.guide-card p,
.feature-card p,
.example-card p {
  margin: 0;
  color: var(--text-secondary);
  font-size: 0.9rem;
}
</style>
