# Mcp.Gateway.Client

**Mcp.Gateway.Client** is a robust .NET client library for the **Model Context Protocol (MCP)**. It provides a unified interface to communicate with MCP servers over various transports, including HTTP (with SSE), WebSockets, and Standard Input/Output (stdio).

## 🚀 Features

- **Multiple Transports**:
  - **HTTP**: Standard JSON-RPC over HTTP POST.
  - **HTTP + SSE**: Server-Sent Events for server-to-client notifications (MCP v1.7.0+ session support).
  - **WebSocket**: Full bidirectional communication.
  - **Stdio**: Standard Input/Output for local process communication.
- **Type-Safe Tool Invocation**: Call tools with strongly-typed request and response models.
- **Notification Support**: Listen for server-initiated events (e.g., logs, resource updates).
- **Async/Await**: Fully asynchronous API designed for modern .NET applications.
- **.NET 10 Support**: Built for the latest .NET ecosystem.

## 📦 Installation

Install the NuGet package:

```bash
dotnet add package Mcp.Gateway.Client
```

Or via the Package Manager Console:

```powershell
Install-Package Mcp.Gateway.Client
```

## 🛠️ Usage

### 1. Choose a Transport

#### HTTP Transport (Basic RPC)
Best for stateless request/response patterns.

```csharp
using Mcp.Gateway.Client;

// Connect to an MCP server over HTTP
await using var transport = new HttpMcpTransport("http://localhost:5000", "/mcp");
```

#### HTTP Transport with SSE (Notifications)
Enables server-to-client notifications using Server-Sent Events and Session Management.

```csharp
// Enable SSE for notifications
await using var transport = new HttpMcpTransport("http://localhost:5000", "/mcp", enableSse: true);
```

#### WebSocket Transport
Ideal for persistent, bidirectional connections.

```csharp
// Connect via WebSocket
await using var transport = new WebSocketMcpTransport("ws://localhost:5000/ws");
```

#### Stdio Transport
Used for communicating with local MCP servers running as subprocesses.

```csharp
// Use Console.In and Console.Out (default)
await using var transport = new StdioMcpTransport();

// Or use custom streams (e.g., from a Process)
// await using var transport = new StdioMcpTransport(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
```

### 2. Initialize the Client

Wrap the transport in an `McpClient` and connect.

```csharp
using Mcp.Gateway.Client;

// Create client
await using var client = new McpClient(transport);

// Handle notifications (optional)
client.NotificationReceived += (sender, e) =>
{
    Console.WriteLine($"Notification received: {e.Method}");
};

// Connect (performs handshake if necessary)
await client.ConnectAsync();
```

### 3. Call Tools

Invoke tools defined on the MCP server.

```csharp
// Define response model
public record AddResponse(double Result);

// Call tool
var result = await client.CallToolAsync<AddResponse>(
    "add_numbers", 
    new { number1 = 10, number2 = 20 }
);

Console.WriteLine($"Result: {result.Result}"); // Output: 30
```

### 4. List Available Tools

Discover what the server offers.

```csharp
var tools = await client.ListToolsAsync();

foreach (var tool in tools.Tools)
{
    Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
}
```

## 🧩 Advanced Examples

### Using Stdio with a Subprocess

```csharp
using System.Diagnostics;
using Mcp.Gateway.Client;

// Start the MCP server process
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

await using var client = new McpClient(transport);
await client.ConnectAsync();

// ... use client ...
```

## 📄 License

MIT
