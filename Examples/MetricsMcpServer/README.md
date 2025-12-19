# MetricsMcpServer - Tool Lifecycle Hooks Example (v1.8.0)

This example demonstrates the **Tool Lifecycle Hooks** feature introduced in v1.8.0.

## Features

- ✅ **LoggingToolLifecycleHook** - Logs all tool invocations to ILogger
- ✅ **MetricsToolLifecycleHook** - Tracks in-memory metrics per tool
- ✅ **Metrics endpoint** - `/metrics` endpoint exposes collected metrics

## Metrics Tracked

Per tool:
- **Invocation count** - Total number of calls (success + failures)
- **Success count** - Number of successful invocations
- **Failure count** - Number of failed invocations
- **Success rate** - Percentage of successful invocations
- **Average duration** - Mean execution time for successful calls
- **Min/Max duration** - Fastest/slowest execution times
- **Error types** - Count of errors by exception type

## Usage

### 1. Register Lifecycle Hooks

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register ToolService
builder.AddToolsService();

// Add lifecycle hooks (v1.8.0)
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();  // Logging
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();  // Metrics

var app = builder.Build();
```

### 2. Access Metrics

```csharp
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    var allMetrics = metricsHook.GetMetrics();
    
    return Results.Json(new
    {
        timestamp = DateTime.UtcNow,
        metrics = allMetrics.Select(kvp => new
        {
            tool = kvp.Key,
            invocations = kvp.Value.InvocationCount,
            successes = kvp.Value.SuccessCount,
            failures = kvp.Value.FailureCount,
            successRate = Math.Round(kvp.Value.SuccessRate * 100, 2),
            avgDuration = Math.Round(kvp.Value.AverageDuration.TotalMilliseconds, 2)
        })
    });
});
```

## Running the Example

### HTTP Mode

```bash
dotnet run
```

Server starts on http://localhost:5000

**Test metrics:**
```bash
# Call some tools
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","id":"1","params":{"name":"add","arguments":{"a":5,"b":3}}}'

# Get metrics
curl http://localhost:5000/metrics
```

### stdio Mode (GitHub Copilot)

```bash
dotnet run -- --stdio
```

Configure in `.github/copilot/extensions.json`:
```json
{
  "mcpServers": {
    "metrics-demo": {
      "command": "dotnet",
      "args": ["run", "--project", "Examples/MetricsMcpServer", "--", "--stdio"]
    }
  }
}
```

## Example Metrics Output

```json
{
  "timestamp": "2025-12-19T16:30:00Z",
  "totalTools": 3,
  "metrics": [
    {
      "tool": "add",
      "invocations": 10,
      "successes": 10,
      "failures": 0,
      "successRate": 100.0,
      "avgDuration": 1.23,
      "minDuration": 0.89,
      "maxDuration": 2.45,
      "errors": {}
    },
    {
      "tool": "divide",
      "invocations": 5,
      "successes": 4,
      "failures": 1,
      "successRate": 80.0,
      "avgDuration": 1.05,
      "minDuration": 0.92,
      "maxDuration": 1.34,
      "errors": {
        "ToolInvalidParamsException": 1
      }
    },
    {
      "tool": "slow_operation",
      "invocations": 2,
      "successes": 2,
      "failures": 0,
      "successRate": 100.0,
      "avgDuration": 103.45,
      "minDuration": 101.23,
      "maxDuration": 105.67,
      "errors": {}
    }
  ]
}
```

## Tools

### add
Adds two numbers.

**Parameters:**
- `a` (number) - First number
- `b` (number) - Second number

**Example:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "id": "1",
  "params": {
    "name": "add",
    "arguments": { "a": 5, "b": 3 }
  }
}
```

### divide
Divides two numbers (throws on divide by zero).

**Parameters:**
- `a` (number) - Numerator
- `b` (number) - Denominator

**Example:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "id": "2",
  "params": {
    "name": "divide",
    "arguments": { "a": 10, "b": 2 }
  }
}
```

### slow_operation
Simulates a slow operation (for testing duration metrics).

**Parameters:**
- `delayMs` (number, optional) - Delay in milliseconds (default: 100)

**Example:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/call",
  "id": "3",
  "params": {
    "name": "slow_operation",
    "arguments": { "delayMs": 200 }
  }
}
```

## Integration with Monitoring Systems

### Prometheus

You can expose metrics in Prometheus format by mapping the metrics to the expected format:

```csharp
app.MapGet("/metrics/prometheus", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    var allMetrics = metricsHook.GetMetrics();
    
    var prometheus = new StringBuilder();
    foreach (var (tool, metrics) in allMetrics)
    {
        prometheus.AppendLine($"# HELP mcp_tool_invocations_total Total tool invocations");
        prometheus.AppendLine($"# TYPE mcp_tool_invocations_total counter");
        prometheus.AppendLine($"mcp_tool_invocations_total{{tool=\"{tool}\"}} {metrics.InvocationCount}");
        
        prometheus.AppendLine($"# HELP mcp_tool_successes_total Successful tool invocations");
        prometheus.AppendLine($"# TYPE mcp_tool_successes_total counter");
        prometheus.AppendLine($"mcp_tool_successes_total{{tool=\"{tool}\"}} {metrics.SuccessCount}");
        
        prometheus.AppendLine($"# HELP mcp_tool_failures_total Failed tool invocations");
        prometheus.AppendLine($"# TYPE mcp_tool_failures_total counter");
        prometheus.AppendLine($"mcp_tool_failures_total{{tool=\"{tool}\"}} {metrics.FailureCount}");
        
        prometheus.AppendLine($"# HELP mcp_tool_duration_ms Average tool duration in milliseconds");
        prometheus.AppendLine($"# TYPE mcp_tool_duration_ms gauge");
        prometheus.AppendLine($"mcp_tool_duration_ms{{tool=\"{tool}\"}} {metrics.AverageDuration.TotalMilliseconds}");
    }
    
    return Results.Text(prometheus.ToString(), "text/plain");
});
```

### Application Insights

```csharp
builder.Services.AddApplicationInsightsTelemetry();

// Custom hook for Application Insights
public class ApplicationInsightsHook : IToolLifecycleHook
{
    private readonly TelemetryClient _telemetry;
    
    public ApplicationInsightsHook(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        _telemetry.TrackEvent("ToolInvoked", new Dictionary<string, string>
        {
            ["tool"] = toolName,
            ["requestId"] = request.IdAsString
        });
        return Task.CompletedTask;
    }
    
    // ... implement other methods
}
```

## See Also

- [Tool Lifecycle Hooks Documentation](../../docs/LifecycleHooks.md)
- [v1.8.0 Release Notes](../../.internal/notes/v1.8.0/v1.8.0-enhancements.md)
