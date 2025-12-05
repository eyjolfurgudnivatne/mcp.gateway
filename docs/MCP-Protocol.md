# 🔌 Model Context Protocol (MCP) Implementation

**Version:** 2025-06-18  
**Status:** Implemented  
**Compliance:** Full MCP Protocol support  
**Last Updated:** 4. desember 2025

---

## 📋 Overview

MCP Gateway implements the **Model Context Protocol (MCP)** specification developed by Anthropic.  
MCP enables AI assistants (Claude Desktop, GitHub Copilot, etc.) to discover and invoke tools via standardized JSON-RPC 2.0 messages.

**Official Specification:** https://modelcontextprotocol.io/  
**Spec Version:** https://spec.modelcontextprotocol.io/specification/2025-06-18/

---

## 🎯 Protocol Version

```json
{
  "protocolVersion": "2025-06-18"
}
```

MCP Gateway supports protocol version **2025-06-18**, which includes:
- Standardized tool discovery via `tools/list`
- Tool invocation via `tools/call`
- JSON Schema-based input validation
- Content-wrapped responses
- Full GitHub Copilot compatibility

### Why 2025-06-18 instead of 2024-11-05?

The newer protocol version includes:
- ✅ **Stricter tool naming rules** - Only `[a-zA-Z0-9_-]` allowed (no dots!)
- ✅ **Better GitHub Copilot integration**
- ✅ **Improved error handling**
- ✅ **Enhanced capabilities negotiation**

---

## 🔧 MCP Methods Implemented

MCP Gateway implements **3 core MCP protocol methods**:

### 1️⃣ `initialize` - Protocol Handshake

Establishes connection and negotiates capabilities.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "initialize",
  "id": 1,
  "params": {
    "protocolVersion": "2025-06-18",
    "capabilities": {},
    "clientInfo": {
      "name": "github-copilot",
      "version": "1.0.0"
    }
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-06-18",
    "serverInfo": {
      "name": "mcp-gateway",
      "version": "2.0.0"
    },
    "capabilities": {
      "tools": {}
    }
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleInitialize()` in `Mcp.Gateway.Tools/ToolInvoker.cs`

---

### 2️⃣ `tools/list` - Tool Discovery

Lists all available tools with their schemas.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 2
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [
      {
        "name": "add_numbers",
        "description": "Adds two numbers and return result. Example: 5 + 3 = 8",
        "inputSchema": {
          "type": "object",
          "properties": {
            "number1": {
              "type": "number",
              "description": "First number to add"
            },
            "number2": {
              "type": "number",
              "description": "Second number to add"
            }
          },
          "required": ["number1", "number2"]
        }
      },
      {
        "name": "system_ping",
        "description": "Simple ping tool that returns pong with timestamp",
        "inputSchema": {
          "type": "object",
          "properties": {}
        }
      }
    ]
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleToolsList()` in `Mcp.Gateway.Tools/ToolInvoker.cs`

**Key Features:**
- Auto-discovery via `[McpTool]` attribute
- JSON Schema validation
- Runtime schema validation (warns about malformed schemas)

---

### 3️⃣ `tools/call` - Tool Invocation

Invokes a specific tool with arguments.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "id": 3,
  "params": {
    "name": "add_numbers",
    "arguments": {
      "number1": 5,
      "number2": 3
    }
  }
}
```

**Response (MCP Content Format):**
```json
{
  "jsonrpc": "2.0",
  "id": 3,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"result\":8}"
      }
    ]
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleToolsCallAsync()` in `Mcp.Gateway.Tools/ToolInvoker.cs`

**Key Features:**
- Wraps tool results in MCP `content` format
- Supports text-based tool results
- Error handling with JSON-RPC error codes

---

## 🛠️ Tool Registration

Tools are registered using the `[McpTool]` attribute:

```csharp
[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""number1"":{""type"":""number"",""description"":""First number to add""},
            ""number2"":{""type"":""number"",""description"":""Second number to add""}
        },
        ""required"":[""number1"",""number2""]
    }")]
public async Task<JsonRpcMessage> AddNumbersTool(JsonRpcMessage request, CalculatorService calculatorService)
{
    var args = request.GetParams<NumbersRequest>();
    
    // Validate params
    var paramsElement = request.GetParams();
    if (!paramsElement.TryGetProperty("number1", out _) || 
        !paramsElement.TryGetProperty("number2", out _))
    {
        throw new ToolInvalidParamsException(
            "Parameters 'number1' and 'number2' are required and must be numbers.");
    }

    double result = calculatorService.Add(args!.Number1, args.Number2);
    return ToolResponse.Success(request.Id, new NumbersResponse(result));
}
```

### Tool Naming Rules

**MCP-Compliant Pattern:**
```
^[a-zA-Z0-9_-]{1,128}$
```

**Valid Examples:**
- ✅ `add_numbers`
- ✅ `system_ping`
- ✅ `system_binary_streams_in`
- ✅ `calculator-add` (hyphens allowed)

**Invalid Examples:**
- ❌ `system.ping` (dots not allowed)
- ❌ `add numbers` (spaces not allowed)
- ❌ `add@numbers` (special chars not allowed)

**Validator:**  
See `ToolMethodNameValidator` in `Mcp.Gateway.Tools/ToolMethodNameValidator.cs`

---

## 🔀 Transport Support

MCP Gateway supports **4 transport methods**:

### 1. HTTP (JSON-RPC over HTTP POST)

**Endpoint:** `POST /rpc`

```bash
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 1
  }'
```

### 2. WebSocket (JSON-RPC over WebSocket)

**Endpoint:** `ws://localhost:5000/ws`

```javascript
const ws = new WebSocket('ws://localhost:5000/ws');
ws.send(JSON.stringify({
  jsonrpc: "2.0",
  method: "tools/list",
  id: 1
}));
```

### 3. SSE (Server-Sent Events over HTTP)

**Endpoint:** `POST /sse`

**For remote MCP clients (Claude Desktop, etc.):**

```bash
curl -X POST http://localhost:5000/sse \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "initialize",
    "id": 1
  }'
```

**Response (SSE format):**
```
Content-Type: text/event-stream

data: {"jsonrpc":"2.0","id":1,"result":{"protocolVersion":"2025-06-18"}}

event: done
data: {}

```

**Configuration Example:**
```json
// .mcp.json configuration for remote SSE server
{
  "mcpServers": {
    "remote_mcp": {
      "url": "https://your-server.com/sse",
      "transport": "sse"
    }
  }
}
```

**Key Features:**
- ✅ Remote MCP server support
- ✅ Cloud-ready (deploy to Azure, AWS, etc.)
- ✅ Firewall-friendly (HTTP/HTTPS)
- ✅ Multi-client support
- ✅ One-way streaming (server → client)

**Implementation:**  
See `ToolInvoker.InvokeSseAsync()` in `Mcp.Gateway.Tools/ToolInvoker.cs`

### 4. stdio (Standard Input/Output)

**For local MCP clients like GitHub Copilot:**

**Production Configuration** (`%APPDATA%\Code\User\globalStorage\github.copilot-chat\.mcp.json`):
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

**Implementation:**  
See `StdioMode` in `Mcp.Gateway.Tools/StdioMode.cs`

---

## 📊 Transport Comparison

| Feature | HTTP/RPC | WebSocket | **SSE** | stdio |
|---------|----------|-----------|---------|-------|
| **Direction** | Request/Response | Duplex | Server→Client | Duplex |
| **Use Case** | Simple calls | Streaming | Remote streaming | Local tools |
| **Latency** | Medium | Low | Low | Lowest |
| **Firewall** | ✅ Friendly | ⚠️ May be blocked | ✅ Friendly | N/A |
| **Remote** | ✅ Yes | ✅ Yes | ✅ Yes | ❌ Local only |
| **MCP Clients** | All | Most | Claude Desktop | GitHub Copilot |

---

## 📊 Error Handling

MCP Gateway uses **JSON-RPC 2.0 error codes**:

| Code | Message | Description |
|------|---------|-------------|
| -32700 | Parse error | Invalid JSON received |
| -32600 | Invalid Request | Not a valid JSON-RPC 2.0 message |
| -32601 | Method not found | Tool does not exist |
| -32602 | Invalid params | Missing or invalid parameters |
| -32603 | Internal error | Server-side error |

**Custom Tool Exceptions:**

```csharp
// Throws -32602
throw new ToolInvalidParamsException("Missing required parameter");

// Throws -32601
throw new ToolNotFoundException("Tool 'xyz' not found");

// Throws -32603
throw new ToolInternalErrorException("Database connection failed");
```

---

## 🧪 Testing MCP Protocol

### Test Coverage

All MCP methods are tested:

**HTTP Transport:**
- `McpProtocolTests.cs` - Tests `initialize`, `tools/list`, `tools/call`

**stdio Transport:**
- `StdioProtocolTests.cs` - Tests stdio communication

**Example Test:**
```csharp
[Fact]
public async Task ToolsList_OverHttp_ReturnsTools()
{
    var request = new
    {
        jsonrpc = "2.0",
        method = "tools/list",
        id = "test-1"
    };

    var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request);
    
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var jsonDoc = JsonDocument.Parse(content);
    
    Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
    Assert.True(result.TryGetProperty("tools", out var tools));
    Assert.NotEmpty(tools.EnumerateArray());
}
```

---

## 🔄 Capabilities

Current capabilities advertised by MCP Gateway:

```json
{
  "capabilities": {
    "tools": {}
  }
}
```

**Supported:**
- ✅ Tool discovery (`tools/list`)
- ✅ Tool invocation (`tools/call`)
- ✅ JSON Schema validation
- ✅ Batch requests (JSON-RPC)
- ✅ Notifications (JSON-RPC)

**Future:**
- 🔜 Resources support
- 🔜 Prompts support
- 🔜 Sampling support

---

## 🚀 GitHub Copilot Integration

MCP Gateway is **fully compatible** with GitHub Copilot's MCP client.

**Configuration Example:**

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

**Verified Tools:**
- ✅ `add_numbers` - Arithmetic operations
- ✅ `multiply_numbers` - Multiplication
- ✅ `system_ping` - Health check
- ✅ `system_echo` - Echo test

**Example Copilot Prompt:**
```
@mcp_gcc add 5 and 3
@mcp_gcc what is 4 times 3?
```

---

## 📚 Related Documentation

- [JSON-RPC 2.0 Specification](JSON-RPC-2.0-spec.md)
- [Streaming Protocol v1.1](StreamingProtocol_v1.1.md)
- [Tool Creation Guide](../Mcp.Gateway.Tools/README.md)

---

## 🔗 External References

- **MCP Official Site:** https://modelcontextprotocol.io/
- **MCP Specification:** https://spec.modelcontextprotocol.io/
- **GitHub Copilot MCP Docs:** https://docs.github.com/en/copilot/using-github-copilot/using-extensions/using-mcp-servers-with-github-copilot
- **Anthropic MCP GitHub:** https://github.com/anthropics/anthropic-mcp

---

## ⚖️ License

MIT License (same as parent project)

---

**Last Updated:** 4. desember 2025  
**Protocol Version:** 2025-06-18  
**MCP Gateway Version:** 2.0.0
