---
layout: mcp-default
title: Calculator Server Example
description: Build a simple calculator MCP server with basic arithmetic operations
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/calculator/
  - title: Calculator Server
    url: /examples/calculator/
toc: true
---

# Calculator Server Example

A complete MCP server implementing basic arithmetic operations.

## Overview

The Calculator server demonstrates:
- ✅ **Basic tool definition** - Simple arithmetic operations
- ✅ **Typed parameters** - Using `TypedJsonRpc<T>`
- ✅ **Error handling** - Division by zero handling
- ✅ **Parameter validation** - Required parameter checking
- ✅ **Structured output** - JSON responses with operation details

## Complete Code

### Program.cs

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

// HTTP mode
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

app.Run();
```

### CalculatorTools.cs

```csharp
using Mcp.Gateway.Tools;

namespace CalculatorMcpServer.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and returns result. Example: 5 + 3 = 8")]
    public JsonRpcMessage AddNumbers(TypedJsonRpc<AddNumbersRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required.");

        var result = args.Number1 + args.Number2;
        
        return ToolResponse.Success(
            request.Id,
            new
            {
                result,
                operation = "addition"
            });
    }

    [McpTool("multiply_numbers",
        Title = "Multiply Numbers",
        Description = "Multiplies two numbers. Example: 5 * 3 = 15")]
    public JsonRpcMessage Multiply(TypedJsonRpc<MultiplyRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required.");
        
        var result = args.Number1 * args.Number2;
        
        return ToolResponse.Success(
            request.Id,
            new MultiplyResponse(result));
    }

    [McpTool("divide_numbers",
        Title = "Divide Numbers",
        Description = "Divides two numbers. Example: 10 / 2 = 5")]
    public JsonRpcMessage Divide(TypedJsonRpc<DivideRequest> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required.");

        if (args.Number2 == 0)
        {
            throw new ToolInvalidParamsException("Cannot divide by zero.");
        }
        
        var result = args.Number1 / args.Number2;
        
        return ToolResponse.Success(
            request.Id,
            new { result, operation = "division" });
    }
}

// Request/Response models
public record AddNumbersRequest(double Number1, double Number2);
public record MultiplyRequest(double Number1, double Number2);
public record MultiplyResponse(double Result);
public record DivideRequest(double Number1, double Number2);
```

## Running the Server

### HTTP Mode

```bash
dotnet run
```

Server runs at: `http://localhost:5000/mcp`

### stdio Mode (GitHub Copilot)

```bash
dotnet run -- --stdio
```

## Testing

### Using curl

**List tools:**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 1
  }'
```

**Call add_numbers:**

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "add_numbers",
      "arguments": {
        "Number1": 5,
        "Number2": 3
      }
    },
    "id": 2
  }'
```

**Response:**

```json
{
  "jsonrpc": "2.0",
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{\"result\":8,\"operation\":\"addition\"}"
      }
    ]
  },
  "id": 2
}
```

### Using GitHub Copilot

Configure `.mcp.json`:

```json
{
  "mcpServers": {
    "calculator": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\CalculatorMcpServer",
        "--",
        "--stdio"
      ]
    }
  }
}
```

Then in Copilot Chat:

```
@calculator add 5 and 3
@calculator multiply 10 by 7
@calculator divide 100 by 4
```

## Key Concepts

### 1. TypedJsonRpc<T>

Type-safe parameter handling:

```csharp
public JsonRpcMessage AddNumbers(TypedJsonRpc<AddNumbersRequest> request)
{
    var args = request.GetParams();  // Returns AddNumbersRequest
    // args.Number1 and args.Number2 are strongly typed
}
```

### 2. Parameter Validation

Always validate required parameters:

```csharp
var args = request.GetParams()
    ?? throw new ToolInvalidParamsException(
        "Parameters 'number1' and 'number2' are required.");
```

### 3. Error Handling

Use `ToolInvalidParamsException` for invalid input:

```csharp
if (args.Number2 == 0)
{
    throw new ToolInvalidParamsException("Cannot divide by zero.");
}
```

This automatically returns a proper JSON-RPC error:

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32602,
    "message": "Invalid params",
    "data": {
      "detail": "Cannot divide by zero."
    }
  },
  "id": 3
}
```

## Integration Tests

### CalculatorToolsTests.cs

```csharp
using Xunit;
using Mcp.Gateway.Tests;

public class CalculatorToolsTests
{
    [Fact]
    public async Task AddNumbers_ValidInput_ReturnsSum()
    {
        // Arrange
        using var server = new McpGatewayFixture();
        var client = server.CreateClient();
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "add_numbers",
                arguments = new { Number1 = 5, Number2 = 3 }
            },
            id = 1
        };
        
        // Act
        var response = await client.PostAsJsonAsync("/mcp", request);
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        
        // Assert
        Assert.Equal(8, result.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString()
            .Deserialize<dynamic>().result);
    }
    
    [Fact]
    public async Task Divide_ByZero_ReturnsError()
    {
        // Arrange
        using var server = new McpGatewayFixture();
        var client = server.CreateClient();
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            @params = new
            {
                name = "divide_numbers",
                arguments = new { Number1 = 10, Number2 = 0 }
            },
            id = 2
        };
        
        // Act
        var response = await client.PostAsJsonAsync("/mcp", request);
        var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
        
        // Assert
        Assert.True(result.RootElement.TryGetProperty("error", out var error));
        Assert.Equal(-32602, error.GetProperty("code").GetInt32());
        Assert.Contains("Cannot divide by zero", 
            error.GetProperty("data").GetProperty("detail").GetString());
    }
}
```

## Enhancements

### 1. Add More Operations

```csharp
[McpTool("subtract_numbers")]
public JsonRpcMessage Subtract(TypedJsonRpc<SubtractRequest> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, 
        new { result = args.Number1 - args.Number2 });
}

[McpTool("power")]
public JsonRpcMessage Power(TypedJsonRpc<PowerRequest> request)
{
    var args = request.GetParams()!;
    return ToolResponse.Success(request.Id, 
        new { result = Math.Pow(args.Base, args.Exponent) });
}
```

### 2. Add History Tracking

```csharp
public class CalculatorTools
{
    private readonly List<CalculationHistory> _history = new();
    
    [McpTool("add_numbers")]
    public JsonRpcMessage AddNumbers(TypedJsonRpc<AddNumbersRequest> request)
    {
        var args = request.GetParams()!;
        var result = args.Number1 + args.Number2;
        
        _history.Add(new CalculationHistory
        {
            Operation = "add",
            Operand1 = args.Number1,
            Operand2 = args.Number2,
            Result = result,
            Timestamp = DateTime.UtcNow
        });
        
        return ToolResponse.Success(request.Id, new { result });
    }
    
    [McpTool("get_history")]
    public JsonRpcMessage GetHistory(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, _history);
    }
}
```

### 3. Add Lifecycle Hooks

```csharp
// In Program.cs
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();

// Expose metrics endpoint
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    return Results.Json(metricsHook?.GetMetrics());
});
```

## Source Code

Full source code available at:
- **GitHub:** [Examples/CalculatorMcpServer](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/CalculatorMcpServer)
- **Tests:** [Examples/CalculatorMcpServerTests](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/CalculatorMcpServerTests)

## See Also

- [Getting Started](/mcp.gateway/getting-started/index/) - Basic tutorial
- [Tools API](/mcp.gateway/api/tools/) - Complete Tools API reference
- [Metrics Example](/mcp.gateway/examples/metrics/) - Add metrics to calculator
- [Authorization Example](/mcp.gateway/examples/authorization/) - Add role-based access
