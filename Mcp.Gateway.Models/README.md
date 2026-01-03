# Mcp.Gateway.Models

**Mcp.Gateway.Models** contains the shared data models and JSON-RPC types used by the **MCP Gateway** ecosystem.

This library provides the core contracts for communicating via the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/), including:

- JsonRpcMessage: Represents JSON-RPC 2.0 requests, responses, and notifications.
- JsonRpcError: Standard error structures.
- JsonOptions: Shared JSON serialization settings.

## 📦 Installation

Install the NuGet package:

```bash
dotnet add package Mcp.Gateway.Models
```

## 🛠️ Usage

This package is primarily intended as a dependency for Mcp.Gateway.Tools (server) and Mcp.Gateway.Client (client), but can be used independently if you need the raw models.

```csharp
using Mcp.Gateway.Models;

// Create a JSON-RPC request
var request = JsonRpcMessage.CreateRequest("ping", 1);

// Create a success response
var response = JsonRpcMessage.CreateSuccess(1, new { message = "pong" });
```

## 📄 License

MIT
