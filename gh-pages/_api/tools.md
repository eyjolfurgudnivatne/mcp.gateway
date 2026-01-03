---
layout: mcp-default
title: Tools API Reference
description: Complete reference for the Tools API
breadcrumbs:
  - title: Home
    url: /
  - title: API Reference
    url: /api/tools/
  - title: Tools API
    url: /api/tools/
toc: true
---

# Tools API Reference

Complete reference for MCP Gateway Tools API.

## Overview

Tools are the core building blocks of MCP servers. They allow AI assistants to:
- Execute custom logic
- Access external APIs
- Manipulate data
- Perform calculations
- And much more!

## Quick Reference

| Method | Description |
|--------|-------------|
| `tools/list` | List all available tools |
| `tools/call` | Invoke a specific tool |

## tools/list

List all tools available on the server.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "optional-pagination-cursor"
  },
  "id": 1
}
```

**Parameters:**
- `cursor` (string, optional) - Pagination cursor

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [
      {
        "name": "add_numbers",
        "description": "Adds two numbers",
        "inputSchema": {
          "type": "object",
          "properties": {
            "number1": {
              "type": "number",
              "description": "First number"
            },
            "number2": {
              "type": "number",
              "description": "Second number"
            }
          },
          "required": ["number1", "number2"]
        }
      }
    ],
    "nextCursor": "optional-next-cursor"
  },
  "id": 1
}
```

**Response fields:**
- `tools` (array) - List of tool definitions
- `nextCursor` (string, optional) - Pagination cursor

### Tool Schema

Each tool has:

```typescript
{
  name: string;           // Tool identifier (e.g., "add_numbers")
  description?: string;   // Human-readable description
  inputSchema: {          // JSON Schema for parameters
    type: "object";
    properties: { ... };
    required?: string[];
  };
}
```

## tools/call

Invoke a specific tool with parameters.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "params": {
    "name": "add_numbers",
    "arguments": {
      "number1": 5,
      "number2": 3
    }
  },
  "id": 2
}
```

**Parameters:**
- `name` (string, required) - Tool name
- `arguments` (object, optional) - Tool-specific parameters

### Response (Success)

```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"result\":8}"
      }
    ]
  },
  "id": 2
}
```

**Response fields:**
- `content` (array) - Tool output

### Response (Error)

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": {
      "detail": "Parameter 'number1' is required"
    }
  },
  "id": 2
}
```

## Defining Tools

### Basic Tool

```csharp
using Mcp.Gateway.Tools;

public class MyTools
{
    [McpTool("hello_world")]
    public JsonRpcMessage HelloWorld(JsonRpcMessage request)
    {
        return ToolResponse.Success(
            request.Id,
            new { message = "Hello, World!" });
    }
}
```

### Tool with Parameters (Typed Return)

```csharp
[McpTool("greet")]
public TypedJsonRpc<GreetResponse> Greet(TypedJsonRpc<GreetParams> request)
{
    var args = request.GetParams()
        ?? throw new ToolInvalidParamsException("Name required");

    return TypedJsonRpc<GreetResponse>.Success(
        request.Id,
        new GreetResponse($"Hello, {args.Name}!"));
}

public record GreetParams(string Name);
public record GreetResponse(string Greeting);
```

### Tool with Metadata

```csharp
[McpTool("add_numbers",
    Title = "Add Numbers",
    Description = "Adds two numbers and returns the sum",
    Icon = "https://example.com/icon.png")]
// OutputSchema is automatically generated from AddResponse!
public TypedJsonRpc<AddResponse> Add(TypedJsonRpc<AddParams> request)
{
    var args = request.GetParams()!;
    var result = args.A + args.B;
    
    return TypedJsonRpc<AddResponse>.Success(
        request.Id, 
        new AddResponse(result));
}

public record AddParams(double A, double B);
public record AddResponse(double Result);
```

## Tool Attributes

### [McpTool]

Marks a method as an MCP tool.

```csharp
[McpTool(
    string name,                    // Required: Tool name
    string? Title = null,           // Optional: Display title
    string? Description = null,     // Optional: Description
    string? Icon = null,            // Optional: Icon URL
    string? InputSchema = null,     // Optional: Input JSON Schema (auto-generated if not provided)
    string? OutputSchema = null)]   // Optional: Output JSON Schema (auto-generated if not provided)
```

**Attribute properties:**

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `name` | string | Yes | Tool identifier (a-z, 0-9, _, -, .) |
| `Title` | string | No | Human-readable title |
| `Description` | string | No | Tool description for AI |
| `Icon` | string | No | Icon URL (v1.6.5+) |
| `InputSchema` | string | No | Input JSON Schema (auto-generated from parameters if not provided) |
| `OutputSchema` | string | No | Output JSON Schema (auto-generated from return type if not provided) |

**InputSchema auto-generation:**
- If not provided, MCP Gateway automatically generates `inputSchema` from the tool's parameter type
- Uses `TypedJsonRpc<T>` parameter type to infer JSON Schema

**OutputSchema auto-generation (v1.8.0+):**
- If not provided, MCP Gateway automatically generates `outputSchema` from the tool's return type
- Uses `TypedJsonRpc<T>` return type to infer JSON Schema
- Example: `TypedJsonRpc<AddResponse>` generates schema with `number` property for `Result`

## Tool Responses

### Success Response (Typed)

```csharp
return TypedJsonRpc<MyResponse>.Success(
    request.Id,
    new MyResponse(42));
```

**Output:**
```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"result\":42}"
    }
  ],
  "structuredContent": {
    "result": 42
  }
}
```

### Success Response (Legacy)

```csharp
return ToolResponse.Success(
    request.Id,
    new { result = 42 });
```

**Output:**
```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"result\":42}"
    }
  ]
}
```

### Error Response

```csharp
throw new ToolInvalidParamsException("Invalid input");
```

**Output:**
```json
{
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": {
      "detail": "Invalid input"
    }
  }
}
```

## Parameter Validation

### Required Parameters

```csharp
var args = request.GetParams()
    ?? throw new ToolInvalidParamsException("Parameters required");
```

### Type Validation

```csharp
if (args.Count < 0)
{
    throw new ToolInvalidParamsException("Count must be positive");
}
```

### Range Validation

```csharp
if (args.Age < 0 || args.Age > 150)
{
    throw new ToolInvalidParamsException("Age must be between 0 and 150");
}
```

## Async Tools

Tools can be async:

```csharp
[McpTool("fetch_data")]
public async Task<JsonRpcMessage> FetchData(JsonRpcMessage request)
{
    var data = await _httpClient.GetStringAsync("https://api.example.com/data");
    
    return ToolResponse.Success(request.Id, new { data });
}
```

## Dependency Injection

Tools support both constructor injection and method parameter injection:

### Option 1: Constructor Injection

Requires class registration in DI container:

```csharp
public class MyTools
{
    private readonly ILogger<MyTools> _logger;
    private readonly IMyService _service;
    
    public MyTools(
        ILogger<MyTools> logger,
        IMyService service)
    {
        _logger = logger;
        _service = service;
    }
    
    [McpTool("my_tool")]
    public async Task<JsonRpcMessage> MyTool(JsonRpcMessage request)
    {
        _logger.LogInformation("Tool invoked");
        var result = await _service.DoSomethingAsync();
        return ToolResponse.Success(request.Id, result);
    }
}

// Register in Program.cs
builder.Services.AddScoped<MyTools>();
builder.Services.AddSingleton<IMyService, MyService>();
```

### Option 2: Method Parameter Injection

No class registration needed - parameters resolved from DI:

```csharp
public class MyTools
{
    [McpTool("my_tool")]
    public async Task<JsonRpcMessage> MyTool(
        JsonRpcMessage request,
        ILogger<MyTools> logger,        // ← Automatically injected!
        IMyService service)             // ← Automatically injected!
    {
        logger.LogInformation("Tool invoked");
        var result = await service.DoSomethingAsync();
        return ToolResponse.Success(request.Id, result);
    }
}

// Only register services (class auto-discovered)
builder.Services.AddSingleton<IMyService, MyService>();
```

**Parameter resolution order:**
1. `JsonRpcMessage` or `TypedJsonRpc<T>` - The request (must be first parameter)
2. Additional parameters - Resolved from DI container (in order)

**Benefits of method parameter injection:**
- ✅ No class registration needed
- ✅ Simpler for tools with few dependencies
- ✅ Clear what each tool needs
- ✅ Easier testing (mock parameters directly)

## Error Codes

| Code | Name | Description |
|------|------|-------------|
| -32700 | Parse error | Invalid JSON |
| -32600 | Invalid request | Not a valid JSON-RPC request |
| -32601 | Method not found | Tool does not exist |
| -32602 | Invalid params | Invalid or missing parameters |
| -32603 | Internal error | Server-side error |

## Best Practices

### 1. Always Validate Parameters

```csharp
var args = request.GetParams()
    ?? throw new ToolInvalidParamsException("Parameters required");
```

### 2. Use Descriptive Names

```csharp
// ✅ GOOD
[McpTool("calculate_fibonacci")]

// ❌ BAD
[McpTool("calc_fib")]
```

### 3. Provide Clear Descriptions

```csharp
[McpTool("send_email",
    Description = "Sends an email to the specified recipient with subject and body")]
```

### 4. Handle Exceptions

```csharp
[McpTool("divide")]
public JsonRpcMessage Divide(TypedJsonRpc<DivideParams> request)
{
    var args = request.GetParams()!;
    
    if (args.Divisor == 0)
    {
        throw new ToolInvalidParamsException("Cannot divide by zero");
    }
    
    return ToolResponse.Success(request.Id, 
        new { result = args.Dividend / args.Divisor });
}
```

### 5. Use Structured Responses

```csharp
// ✅ GOOD - Structured
return ToolResponse.Success(request.Id, new
{
    result = 42,
    timestamp = DateTime.UtcNow,
    status = "success"
});

// ❌ BAD - Plain string
return ToolResponse.Success(request.Id, "42");
```

## See Also

- [Getting Started](/mcp.gateway/getting-started/index/) - Build your first tool
- [Calculator Example](/mcp.gateway/examples/calculator/) - Complete tool examples
- [Lifecycle Hooks](/mcp.gateway/features/lifecycle-hooks/) - Monitor tool invocations
- [Authorization](/mcp.gateway/features/authorization/) - Secure your tools
