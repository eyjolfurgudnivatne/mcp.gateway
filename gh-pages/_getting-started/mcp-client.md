---
layout: mcp-default
title: Mcp.Gateway.Client
description: Use MCP.Gateway.Client to access Mcp.Gateway.Tools server.
breadcrumbs:
  - title: Home
    url: /
  - title: Getting Started
    url: /getting-started/index/
  - title: Mcp.Gateway.Client
    url: /getting-started/mcp-client/
prev: false
next: false
toc: true
---

# Mcp.Gateway.Client

`Mcp.Gateway.Client` is a .NET library for communicating with Model Context Protocol (MCP) servers. It supports multiple transports (HTTP, WebSocket, Stdio) and provides a strongly-typed API for tools, resources, and prompts.

## 📦 Installation

Install the NuGet package:

```bash
dotnet add package Mcp.Gateway.Client
```

## 🚀 Quick Start

### 1. Choose a Transport

The client supports three types of transports:

#### HTTP Transport
Best for stateless request/response patterns. Supports Server-Sent Events (SSE) for notifications.

```csharp
using Mcp.Gateway.Client;

// Basic HTTP transport
await using var transport = new HttpMcpTransport("http://localhost:5000");

// HTTP with SSE (for notifications)
// await using var transport = new HttpMcpTransport("http://localhost:5000", enableSse: true);
```

#### WebSocket Transport
Ideal for persistent, bidirectional connections and streaming.

```csharp
using Mcp.Gateway.Client;

// Connect via WebSocket
await using var transport = new WebSocketMcpTransport("ws://localhost:5000/ws");
```

#### Stdio Transport
Used for communicating with local MCP servers running as subprocesses.

```csharp
using Mcp.Gateway.Client;
using System.Diagnostics;

// Start the server process
var process = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "run --project MyMcpServer -- --stdio",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        UseShellExecute = false
    }
};
process.Start();

// Connect via Stdio
await using var transport = new StdioMcpTransport(
    process.StandardOutput.BaseStream, 
    process.StandardInput.BaseStream
);
```

### 2. Initialize and Connect

Wrap the transport in an `McpClient` and call `ConnectAsync`.

```csharp
// Create client
await using var client = new McpClient(transport);

// Connect (performs handshake)
await client.ConnectAsync();
```

---

## 🛠️ Working with Tools

### List Tools
Discover available tools on the server.

```csharp
var toolsResult = await client.ListToolsAsync();
foreach (var tool in toolsResult.Tools)
{
    Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
}
```

### Call a Tool
Invoke a tool with parameters and get a typed response.

```csharp
// Define response model
public record AddResponse(int Result);

// Call tool
var result = await client.CallToolAsync<AddResponse>(
    "add_numbers", 
    new { number1 = 10, number2 = 20 }
);

Console.WriteLine($"Result: {result.Result}");
```

### Streaming Tool Calls
Consume a stream of results from a tool.

```csharp
await foreach (var item in client.CallToolStreamAsync<int>("count_to_10", new { }))
{
    Console.WriteLine($"Received: {item}");
}
```

---

## 📚 Working with Resources

### List Resources
Browse available resources.

```csharp
var resourcesResult = await client.ListResourcesAsync();
foreach (var resource in resourcesResult.Resources)
{
    Console.WriteLine($"Resource: {resource.Name} ({resource.Uri})");
}
```

### Read a Resource
Get the content of a specific resource.

```csharp
var readResourceResult = await client.ReadResourceAsync("file://logs/app.log");
Console.WriteLine($"Content: {readResourceResult.Contents[0].Text}");
```

---

## 📝 Working with Prompts

### List Prompts
See what prompts are available.

```csharp
var promptsResult = await client.ListPromptsAsync();
foreach (var prompt in promptsResult.Prompts)
{
    Console.WriteLine($"Prompt: {prompt.Name}");
}
```

### Get a Prompt
Retrieve a prompt with arguments.

```csharp
// Simple overload
var prompt = await client.GetPromptAsync("santa_report", new { name = "Alice", behavior = "Good" });

// Using PromptRequest object
var request = new PromptRequest 
{ 
    Name = "santa_report", 
    Arguments = new Dictionary<string, object> { { "name", "Bob" }, { "behavior", "Naughty" } } 
};
var prompt2 = await client.GetPromptAsync(request);

// Access messages
foreach (var message in prompt.Messages)
{
    if (message.Content is TextContent text)
    {
        Console.WriteLine($"{message.Role}: {text.Text}");
    }
}
```

---

## 🔔 Notifications

Listen for server-initiated events.

```csharp
client.NotificationReceived += (sender, e) =>
{
    Console.WriteLine($"Notification received: {e.Method}");
};
```

---

## ⚠️ Error Handling

The client throws `McpClientException` when operations fail.

```csharp
try
{
    await client.CallToolAsync("unknown_tool", new { });
}
catch (McpClientException ex)
{
    Console.WriteLine($"MCP Error: {ex.Message}");
    if (ex.RpcError != null)
    {
        Console.WriteLine($"Code: {ex.RpcError.Code}");
    }
}
```
