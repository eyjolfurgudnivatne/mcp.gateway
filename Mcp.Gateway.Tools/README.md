# 🛠️ Mcp.Gateway.Tools

> Core library for building MCP servers in .NET 10

[![NuGet](https://img.shields.io/nuget/v/Mcp.Gateway.Tools.svg)](https://www.nuget.org/packages/Mcp.Gateway.Tools/)
[![.NET 10](https://img.shields.io/badge/.NET-10-purple)](https://dotnet.microsoft.com/)
[![MCP Protocol](https://img.shields.io/badge/MCP-2025--06--18-green)](https://modelcontextprotocol.io/)

`Mcp.Gateway.Tools` contains the infrastructure for MCP tools:

- JSON‑RPC models (`JsonRpcMessage`, `JsonRpcError`)
- Attributes (`McpToolAttribute`)
- Tool registration (`ToolService`)
- Invocation and protocol implementation (`ToolInvoker`)
- Streaming (`ToolConnector`, `ToolCapabilities`)
- ASP.NET Core extensions for endpoints (`MapHttpRpcEndpoint`, `MapWsRpcEndpoint`, `MapSseRpcEndpoint`, `AddToolsService`)

This README focuses on **how to use the library in your own server**.  
See the root `README.md` for a high‑level overview and client integration.

---

## 🔧 Register tool infrastructure

In your `Program.cs`:

```
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register ToolService + ToolInvoker
builder.AddToolsService();

var app = builder.Build();

// Custom: stdio, logging, etc. (see DevTestServer for a full example)

// WebSockets must be enabled before WS/SSE routes
app.UseWebSockets();

// MCP endpoints
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");

app.Run();
```

`AddToolsService` registers:

- `ToolService` as a singleton (discovers/validates tools)
- `ToolInvoker` as scoped (handles JSON‑RPC and MCP methods)

---

## 🧩 Defining tools

### Basic tool

Based on `Examples/CalculatorMcpServer/Tools/CalculatorTools.cs`:

```
using CalculatorMcpServer.Models;
using Mcp.Gateway.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and returns the result.",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""number1"":{""type"":""number"",""description"":""First number""},
                ""number2"":{""type"":""number"",""description"":""Second number""}
            },
            ""required"": [""number1"",""number2""]
        }")]
    public JsonRpcMessage AddNumbersTool(JsonRpcMessage request)
    {
        var args = request.GetParams<AddNumbersRequest>()
            ?? throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");

        return ToolResponse.Success(
            request.Id,
            new AddNumbersResponse(args.Number1 + args.Number2));
    }
}
```

### With validation and DI (from `DevTestServer/Tools/Calculator.cs`)

```
using DevTestServer.MyServices;
using Mcp.Gateway.Tools;
using System.Text.Json;

public class Calculator
{
    public sealed record NumbersRequest(double Number1, double Number2);
    public sealed record NumbersResponse(double Result);

    [McpTool("add_numbers",
        Title = "Add Numbers",
        Description = "Adds two numbers and return result. Example: 5 + 3 = 8",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""number1"":{""type"":""number"",""description"":""First number to add""},
                ""number2"":{""type"":""number"",""description"":""Second number to add""}
            },
            ""required"": [""number1"",""number2""]
        }")]
    public async Task<JsonRpcMessage> AddNumbersTool(
        JsonRpcMessage request,
        CalculatorService calculatorService)
    {
        await Task.CompletedTask; // placeholder for async work

        var @params = request.GetParams();

        if (@params.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ||
            !@params.TryGetProperty("number1", out _) ||
            !@params.TryGetProperty("number2", out _))
        {
            throw new ToolInvalidParamsException(
                "Parameters 'number1' and 'number2' are required and must be numbers.");
        }

        var args = request.GetParams<NumbersRequest>()!;
        var result = calculatorService.Add(args.Number1, args.Number2);

        return ToolResponse.Success(request.Id, new NumbersResponse(result));
    }
}
```

Register the DI service in `Program.cs`:

```
builder.Services.AddScoped<CalculatorService>();
```

---

## 🕒 Date/time tools (example)

From `Examples/DateTimeMcpServer/Tools/DateTimeTools.cs`:

```
using DateTimeMcpServer.Models;
using Mcp.Gateway.Tools;

public class DateTimeTools
{
    [McpTool("get_current_datetime",
        Title = "Get current date and time",
        Description = "Get current date and time in specified timezone (default: local).",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""timezoneName"":{
                    ""type"":""string"",
                    ""description"":""Timezone name (e.g., 'Europe/Oslo', 'UTC')"",
                    ""default"":""UTC""
                }
            }
        }")]
    public JsonRpcMessage GetCurrentDateTime(JsonRpcMessage message)
    {
        var request = message.GetParams<CurrentDateTimeRequest>();
        TimeZoneInfo tz;

        try
        {
            tz = string.IsNullOrWhiteSpace(request?.TimezoneName)
                ? TimeZoneInfo.Local
                : TimeZoneInfo.FindSystemTimeZoneById(request.TimezoneName);
        }
        catch
        {
            tz = TimeZoneInfo.Local;
        }

        var now = TimeZoneInfo.ConvertTime(DateTime.Now, tz);

        return ToolResponse.Success(
            message.Id,
            new CurrentDateTimeResponse(
                now.ToString("o"),
                now.ToString("yyyy-MM-dd"),
                now.ToString("HH:mm:ss"),
                tz.Id,
                now.ToString("dddd"),
                System.Globalization.ISOWeek.GetWeekOfYear(now),
                now.Year,
                now.Month,
                now.Day));
    }
}
```

---

## 🧵 Streaming and `ToolCapabilities`

### Capabilities

`ToolCapabilities` is used to filter tools per transport:

```
[Flags]
public enum ToolCapabilities
{
    Standard        = 1,
    TextStreaming   = 2,
    BinaryStreaming = 4,
    RequiresWebSocket = 8
}
```

- HTTP/stdio: `Standard` only
- SSE: `Standard` + `TextStreaming`
- WebSocket: all (incl. `BinaryStreaming` and `RequiresWebSocket`)

### Simple text streaming tool

```
[McpTool("stream_data",
    Description = "Streams incremental data to the client.",
    Capabilities = ToolCapabilities.TextStreaming)]
public async Task StreamData(ToolConnector connector)
{
    var meta = new StreamMessageMeta(
        Method: "stream_data",
        Binary: false);

    using var handle = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);

    for (int i = 0; i < 5; i++)
    {
        await handle.WriteAsync(new { chunk = i });
    }

    await handle.CompleteAsync(new { done = true });
}
```

See `docs/StreamingProtocol.md` and `docs/examples/toolconnector-usage.md` for more details.

---

## 🧱 JSON models

Core models live in `ToolModels.cs`:

- `JsonRpcMessage` – JSON‑RPC 2.0 messages
- `JsonRpcError` – structured errors
- `JsonOptions.Default` – shared `JsonSerializerOptions` (camelCase, etc.)

Typical usage:

```
public JsonRpcMessage Echo(JsonRpcMessage message)
{
    var raw = message.GetParams();
    return JsonRpcMessage.CreateSuccess(message.Id, raw);
}
```

---

## 🧪 Verification and tests

The library itself is tested via `DevTestServer` + `Mcp.Gateway.Tests`:

- Protocol tests: `Mcp.Gateway.Tests/Endpoints/Http/McpProtocolTests.cs`
- Streaming tests: `Mcp.Gateway.Tests/Endpoints/Ws/*`, `.../Sse/*`
- stdio tests: `Mcp.Gateway.Tests/Endpoints/Stdio/*`

For your own development:

```
dotnet test
```

---

## 📌 Summary

To use `Mcp.Gateway.Tools` in your project:

1. Add the NuGet package
2. Call `builder.AddToolsService()` in `Program.cs`
3. Map `MapHttpRpcEndpoint`, `MapWsRpcEndpoint`, `MapSseRpcEndpoint` as needed
4. Define tools by annotating methods with `[McpTool]`
5. Connect the server to an MCP client (GitHub Copilot, Claude, etc.)

For complete examples, see:

- `Examples/CalculatorMcpServer`
- `Examples/DateTimeMcpServer`
- `DevTestServer` (used by the tests)

---

**License:** MIT – see root `LICENSE`.
