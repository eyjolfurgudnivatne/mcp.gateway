# 🔌 Model Context Protocol (MCP) Implementation

**Version:** 2025-06-18  
**Status:** Implemented  
**Compliance:** Full MCP Protocol support (Tools, Prompts, Resources)  
**Last Updated:** 16. desember 2025  
**Protocol Version:** 2025-06-18  
**MCP Gateway Version:** v1.5.0 (Tools + Prompts + Resources)

---

## 📋 Overview

MCP Gateway implements the **Model Context Protocol (MCP)** specification developed by Anthropic.  
MCP enables AI assistants (Claude Desktop, GitHub Copilot, etc.) to discover and invoke tools, prompts, and resources via standardized JSON-RPC 2.0 messages.

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
- Prompt discovery via `prompts/list` (v1.4.0+)
- Prompt retrieval via `prompts/get` (v1.4.0+)
- Resource discovery via `resources/list` (v1.5.0+)
- Resource reading via `resources/read` (v1.5.0+)
- JSON Schema-based input validation
- Content-wrapped responses
- Full GitHub Copilot compatibility

### Why 2025-06-18 instead of 2024-11-05?

The newer protocol version includes:
- ✅ **Stricter tool naming rules** - Only `[a-zA-Z0-9_-]` allowed (no dots!)
- ✅ **Better GitHub Copilot integration**
- ✅ **Improved error handling**
- ✅ **Enhanced capabilities negotiation**
- ✅ **Prompts and Resources support**

---

## 🔧 MCP Methods Implemented

MCP Gateway implements **9 core MCP protocol methods** across three categories:

### Tools (v1.0+)
- `initialize` - Protocol handshake
- `tools/list` - Tool discovery
- `tools/call` - Tool invocation

### Prompts (v1.4.0+)
- `prompts/list` - Prompt discovery
- `prompts/get` - Prompt retrieval

### Resources (v1.5.0+)
- `resources/list` - Resource discovery
- `resources/read` - Resource content retrieval

---

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

**Response (with all capabilities):**
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
      "tools": {},
      "prompts": {},
      "resources": {}
    }
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleInitialize()` in `Mcp.Gateway.Tools/ToolInvoker.Protocol.cs`

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
      }
    ]
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleFunctionsList()` (unified handler for both `tools/list` and `prompts/list`) in `Mcp.Gateway.Tools/ToolInvoker.Protocol.cs`

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
See `ToolInvoker.HandleFunctionsCallAsync()` in `Mcp.Gateway.Tools/ToolInvoker.Protocol.cs` (handles both `tools/call` and `prompts/get`)

---

### 4️⃣ `prompts/list` - Prompt Discovery (v1.4.0+)

Lists all available prompts with their metadata.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "prompts/list",
  "id": 4
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 4,
  "result": {
    "prompts": [
      {
        "name": "santa_report_prompt",
        "description": "A prompt that reports to Santa Claus",
        "arguments": [
          {
            "name": "name",
            "description": "Name of the child",
            "required": true
          },
          {
            "name": "behavior",
            "description": "Behavior of the child",
            "required": true
          }
        ]
      }
    ]
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleFunctionsList()` in `Mcp.Gateway.Tools/ToolInvoker.Protocol.cs`

---

### 5️⃣ `prompts/get` - Prompt Retrieval (v1.4.0+)

Retrieves a specific prompt by name.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "prompts/get",
  "id": 5,
  "params": {
    "name": "santa_report_prompt"
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 5,
  "result": {
    "name": "santa_report_prompt",
    "description": "A prompt that reports to Santa Claus",
    "messages": [
      {
        "role": "system",
        "content": "You are a helpful assistant for Santa Claus."
      },
      {
        "role": "user",
        "content": "Send a letter to Santa Claus and tell him that {{name}} has been {{behavior}}."
      }
    ],
    "arguments": {
      "name": {
        "type": "string",
        "description": "Name of the child"
      },
      "behavior": {
        "type": "string",
        "description": "Behavior of the child (e.g., Good, Naughty)",
        "enum": ["Good", "Naughty"]
      }
    }
  }
}
```

**Note:** The prompt template uses `{{name}}` and `{{behavior}}` as placeholders. The `arguments` object describes the required parameters using JSON Schema format. The MCP client is responsible for substituting these values in the message templates before sending them to the LLM.

**Implementation:**  
See `ToolInvoker.HandleFunctionsCallAsync()` in `Mcp.Gateway.Tools/ToolInvoker.Protocol.cs`

---

### 6️⃣ `resources/list` - Resource Discovery (v1.5.0+)

Lists all available resources with their metadata.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/list",
  "id": 6
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 6,
  "result": {
    "resources": [
      {
        "uri": "file://logs/app.log",
        "name": "Application Logs",
        "description": "Recent application log entries",
        "mimeType": "text/plain"
      },
      {
        "uri": "db://users/example",
        "name": "Example User",
        "description": "Example user profile data",
        "mimeType": "application/json"
      },
      {
        "uri": "system://status",
        "name": "System Status",
        "description": "Current system health metrics",
        "mimeType": "application/json"
      }
    ]
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleResourcesList()` in `Mcp.Gateway.Tools/ToolInvoker.Resources.cs`

---

### 7️⃣ `resources/read` - Resource Content Retrieval (v1.5.0+)

Retrieves the content of a specific resource by URI.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/read",
  "id": 7,
  "params": {
    "uri": "system://status"
  }
}
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "id": 7,
  "result": {
    "contents": [
      {
        "uri": "system://status",
        "mimeType": "application/json",
        "text": "{\"uptime\":12345,\"memoryUsed\":512,\"timestamp\":\"2025-12-16T10:00:00Z\"}"
      }
    ]
  }
}
```

**Implementation:**  
See `ToolInvoker.HandleResourcesReadAsync()` in `Mcp.Gateway.Tools/ToolInvoker.Resources.cs`

---

## 🔄 Capabilities

Current capabilities advertised by MCP Gateway:

```json
{
  "capabilities": {
    "tools": {},
    "prompts": {},
    "resources": {}
  }
}
```

**Supported:**
- ✅ Tool discovery (`tools/list`)
- ✅ Tool invocation (`tools/call`)
- ✅ Prompt discovery (`prompts/list`) - v1.4.0+
- ✅ Prompt retrieval (`prompts/get`) - v1.4.0+
- ✅ Resource discovery (`resources/list`) - v1.5.0+
- ✅ Resource reading (`resources/read`) - v1.5.0+
- ✅ JSON Schema validation
- ✅ Batch requests (JSON-RPC)
- ✅ Notifications (JSON-RPC)

**Future (v1.6+):**
- 🔜 Resource subscriptions (`resources/subscribe`)
- 🔜 Resource templates with URI variables
- 🔜 Sampling support

---

## 🛠️ Registration

### Tool Registration

Tools are registered using the `[McpTool]` attribute:

```csharp
[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and return result",
    Icon = "https://example.com/icons/calculator.png",  // NEW: v1.6.5+
    InputSchema = @"{...}")]
public async Task<JsonRpcMessage> AddNumbers(JsonRpcMessage request)
{
    var a = request.GetParams().GetProperty("number1").GetDouble();
    var b = request.GetParams().GetProperty("number2").GetDouble();
    return ToolResponse.Success(request.Id, new { result = a + b });
}
```

**Icons (v1.6.5+):**

All three types (tools, prompts, resources) support optional icons for visual representation:

```csharp
// Tool with icon
[McpTool("calculator", Icon = "https://example.com/calc.png")]

// Prompt with icon
[McpPrompt("summarize", Icon = "https://example.com/document.png")]

// Resource with icon
[McpResource("file://config", Icon = "https://example.com/config.png")]
```

Icons are serialized in the MCP protocol as:
```json
{
  "name": "add_numbers",
  "icons": [{"src": "https://example.com/icons/calculator.png", "mimeType": null, "sizes": null}]
}
```

Supported formats:
- ✅ HTTPS URLs: `"https://example.com/icon.png"`
- ✅ Data URIs: `"data:image/svg+xml;base64,..."`
- ℹ️ When omitted, the `icons` field is not included in the response

### Prompt Registration (v1.4.0+)

Prompts are registered using the `[McpPrompt]` attribute:

```csharp
[McpPrompt(Description = "Report to Santa Claus")]
public JsonRpcMessage SantaReportPrompt(JsonRpcMessage request)
{
    return ToolResponse.Success(request.Id, new PromptResponse(
        Name: "santa_report_prompt",
        Description: "A prompt that reports to Santa Claus",
        Messages: [
            new(PromptRole.System, "You are a helpful assistant for Santa Claus."),
            new(PromptRole.User, "Tell Santa that {{name}} has been {{behavior}}.")
        ],
        Arguments: new { name = "...", behavior = "..." }
    ));
}
```

### Resource Registration (v1.5.0+)

Resources are registered using the `[McpResource]` attribute:

```csharp
[McpResource("system://status",
    Name = "System Status",
    Description = "Current system health metrics",
    MimeType = "application/json")]
public JsonRpcMessage SystemStatus(JsonRpcMessage request)
{
    var status = new { uptime = Environment.TickCount64, ... };
    var json = JsonSerializer.Serialize(status);
    
    return ToolResponse.Success(request.Id, new ResourceContent(
        Uri: "system://status",
        MimeType: "application/json",
        Text: json
    ));
}
```

### Naming Rules

**Tools & Prompts - MCP-Compliant Pattern:**
```
^[a-zA-Z0-9_.-]{1,128}$
```

**MCP 2025-11-25 Update:** Tool names can now include dots (`.`) for namespacing!

**Examples:**
- ✅ `admin.tools.list` - Namespaced tool
- ✅ `user.get_profile` - Mixed style
- ✅ `db.users.create` - Multi-level namespace
- ✅ `add_numbers` - Traditional underscore style
- ✅ `fetch-data` - Hyphen style

**Resources - URI Format:**
```
scheme://path
```

**Valid Resource URIs:**
- ✅ `file://logs/app.log`
- ✅ `db://users/123`
- ✅ `system://status`
- ✅ `http://api.example.com/data`

**Validator:**  
See `ToolMethodNameValidator` and `IsValidResourceUri()` in `Mcp.Gateway.Tools/`
