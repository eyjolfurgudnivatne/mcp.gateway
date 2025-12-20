---
layout: mcp-default
title: Metrics Server Example
description: Add production-ready metrics tracking to your MCP server
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/
  - title: Metrics Server
    url: /examples/metrics/
toc: true
---

# Metrics Server Example

Add production-ready metrics tracking using Lifecycle Hooks.

## Overview

The Metrics server demonstrates:
- ✅ **Lifecycle hooks** - Monitor tool invocations
- ✅ **Metrics collection** - Track success/failure rates
- ✅ **Duration tracking** - Monitor performance
- ✅ **HTTP endpoint** - Expose metrics as JSON
- ✅ **Production-ready** - Real-world monitoring patterns

## Complete Code

### Program.cs

```csharp
using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Lifecycle;

var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway
builder.AddToolsService();

// Add lifecycle hooks (v1.8.0)
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();

var app = builder.Build();

// stdio mode
if (args.Contains("--stdio"))
{
    await ToolInvoker.RunStdioModeAsync(app.Services);
    return;
}

// HTTP mode
app.UseWebSockets();
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

// Metrics endpoint (v1.8.0)
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    
    if (metricsHook == null)
    {
        return Results.Json(new
        {
            error = "MetricsToolLifecycleHook not registered"
        });
    }
    
    var allMetrics = metricsHook.GetMetrics();
    
    return Results.Json(new
    {
        timestamp = DateTime.UtcNow,
        totalTools = allMetrics.Count,
        metrics = allMetrics.Select(kvp => new
        {
            tool = kvp.Key,
            invocations = kvp.Value.InvocationCount,
            successes = kvp.Value.SuccessCount,
            failures = kvp.Value.FailureCount,
            successRate = Math.Round(kvp.Value.SuccessRate * 100, 2),
            avgDuration = Math.Round(kvp.Value.AverageDuration.TotalMilliseconds, 2),
            minDuration = Math.Round(kvp.Value.MinDuration.TotalMilliseconds, 2),
            maxDuration = Math.Round(kvp.Value.MaxDuration.TotalMilliseconds, 2),
            errors = kvp.Value.ErrorCounts.ToDictionary(e => e.Key, e => e.Value)
        })
    });
});

app.Run();
```

### CalculatorTools.cs

```csharp
using Mcp.Gateway.Tools;

namespace MetricsMcpServer.Tools;

public class CalculatorTools
{
    [McpTool("add_numbers", Description = "Adds two numbers")]
    public JsonRpcMessage AddNumbers(TypedJsonRpc<AddParams> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException("Parameters required");

        var result = args.A + args.B;
        
        return ToolResponse.Success(request.Id, new { result });
    }

    [McpTool("divide", Description = "Divides two numbers")]
    public JsonRpcMessage Divide(TypedJsonRpc<DivideParams> request)
    {
        var args = request.GetParams()
            ?? throw new ToolInvalidParamsException("Parameters required");

        if (args.Divisor == 0)
        {
            throw new ToolInvalidParamsException("Cannot divide by zero");
        }

        var result = args.Dividend / args.Divisor;
        
        return ToolResponse.Success(request.Id, new { result });
    }

    [McpTool("slow_operation", Description = "Simulates a slow operation")]
    public async Task<JsonRpcMessage> SlowOperation(JsonRpcMessage request)
    {
        // Simulate slow operation
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        
        return ToolResponse.Success(request.Id, 
            new { message = "Operation completed" });
    }
}

public record AddParams(double A, double B);
public record DivideParams(double Dividend, double Divisor);
```

## Running the Server

```bash
dotnet run
```

Server runs at:
- **MCP endpoint:** `http://localhost:5000/mcp`
- **Metrics endpoint:** `http://localhost:5000/metrics`

## Testing

### 1. Call Some Tools

```bash
# Call add_numbers (success)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "add_numbers",
      "arguments": { "A": 5, "B": 3 }
    },
    "id": 1
  }'

# Call divide (success)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "divide",
      "arguments": { "Dividend": 10, "Divisor": 2 }
    },
    "id": 2
  }'

# Call divide (failure - divide by zero)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "divide",
      "arguments": { "Dividend": 10, "Divisor": 0 }
    },
    "id": 3
  }'
```

### 2. View Metrics

```bash
curl http://localhost:5000/metrics
```

**Example response:**

```json
{
  "timestamp": "2025-12-20T10:30:00Z",
  "totalTools": 3,
  "metrics": [
    {
      "tool": "add_numbers",
      "invocations": 150,
      "successes": 150,
      "failures": 0,
      "successRate": 100.0,
      "avgDuration": 1.23,
      "minDuration": 0.89,
      "maxDuration": 2.45,
      "errors": {}
    },
    {
      "tool": "divide",
      "invocations": 50,
      "successes": 48,
      "failures": 2,
      "successRate": 96.0,
      "avgDuration": 1.15,
      "minDuration": 0.92,
      "maxDuration": 1.78,
      "errors": {
        "ToolInvalidParamsException": 2
      }
    },
    {
      "tool": "slow_operation",
      "invocations": 10,
      "successes": 10,
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

## Key Concepts

### 1. Lifecycle Hooks

Register hooks in `Program.cs`:

```csharp
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();  // Logs to ILogger
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();  // Collects metrics
```

Hooks are invoked automatically:
- `OnToolInvokingAsync` - Before tool execution
- `OnToolCompletedAsync` - After successful execution
- `OnToolFailedAsync` - After failed execution

### 2. Metrics Collection

`MetricsToolLifecycleHook` tracks:
- **Invocation count** - Total calls
- **Success/Failure counts** - Reliability metrics
- **Duration** - Min/Max/Average execution time
- **Error types** - Count by exception type

### 3. HTTP Metrics Endpoint

Expose metrics via HTTP GET:

```csharp
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    var metrics = metricsHook?.GetMetrics();
    return Results.Json(metrics);
});
```

## Production Patterns

### 1. Prometheus Integration

Export metrics in Prometheus format:

```csharp
using Prometheus;

// Create Prometheus metrics
var invocations = Metrics.CreateCounter(
    "mcp_tool_invocations_total",
    "Total tool invocations",
    new CounterConfiguration { LabelNames = new[] { "tool" } });

// Create custom hook
public class PrometheusHook : IToolLifecycleHook
{
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        invocations.WithLabels(toolName).Inc();
        return Task.CompletedTask;
    }
    
    // ... other methods
}

// Register hook
builder.AddToolLifecycleHook<PrometheusHook>();

// Expose Prometheus endpoint
app.MapMetrics();  // /metrics in Prometheus format
```

### 2. Application Insights

Send metrics to Azure:

```csharp
using Microsoft.ApplicationInsights;

public class ApplicationInsightsHook : IToolLifecycleHook
{
    private readonly TelemetryClient _telemetry;
    
    public ApplicationInsightsHook(TelemetryClient telemetry)
    {
        _telemetry = telemetry;
    }
    
    public Task OnToolCompletedAsync(
        string toolName,
        JsonRpcMessage response,
        TimeSpan duration)
    {
        var metric = new MetricTelemetry(
            "ToolDuration",
            duration.TotalMilliseconds);
        
        metric.Properties["tool"] = toolName;
        metric.Properties["status"] = "success";
        
        _telemetry.TrackMetric(metric);
        
        return Task.CompletedTask;
    }
    
    // ... other methods
}

// Register
builder.Services.AddApplicationInsightsTelemetry();
builder.AddToolLifecycleHook<ApplicationInsightsHook>();
```

### 3. Custom Metrics Storage

Store metrics in database:

```csharp
public class DatabaseMetricsHook : IToolLifecycleHook
{
    private readonly IMetricsRepository _repository;
    
    public async Task OnToolCompletedAsync(
        string toolName,
        JsonRpcMessage response,
        TimeSpan duration)
    {
        await _repository.SaveMetricAsync(new ToolMetric
        {
            ToolName = toolName,
            Status = "success",
            Duration = duration,
            Timestamp = DateTime.UtcNow
        });
    }
    
    // ... other methods
}
```

## Monitoring Dashboard

Create a simple dashboard:

```html
<!DOCTYPE html>
<html>
<head>
    <title>MCP Metrics Dashboard</title>
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
</head>
<body>
    <h1>MCP Tool Metrics</h1>
    <canvas id="successRateChart"></canvas>
    <canvas id="durationChart"></canvas>
    
    <script>
        async function loadMetrics() {
            const response = await fetch('http://localhost:5000/metrics');
            const data = await response.json();
            
            // Success rate chart
            new Chart(document.getElementById('successRateChart'), {
                type: 'bar',
                data: {
                    labels: data.metrics.map(m => m.tool),
                    datasets: [{
                        label: 'Success Rate (%)',
                        data: data.metrics.map(m => m.successRate),
                        backgroundColor: 'rgba(75, 192, 192, 0.2)',
                        borderColor: 'rgba(75, 192, 192, 1)',
                        borderWidth: 1
                    }]
                }
            });
            
            // Duration chart
            new Chart(document.getElementById('durationChart'), {
                type: 'bar',
                data: {
                    labels: data.metrics.map(m => m.tool),
                    datasets: [{
                        label: 'Avg Duration (ms)',
                        data: data.metrics.map(m => m.avgDuration),
                        backgroundColor: 'rgba(153, 102, 255, 0.2)',
                        borderColor: 'rgba(153, 102, 255, 1)',
                        borderWidth: 1
                    }]
                }
            });
        }
        
        loadMetrics();
        setInterval(loadMetrics, 5000);  // Refresh every 5 seconds
    </script>
</body>
</html>
```

## Integration Tests

```csharp
using Xunit;

public class MetricsTests
{
    [Fact]
    public async Task Metrics_AfterToolInvocation_TracksInvocationCount()
    {
        // Arrange
        using var server = new McpGatewayFixture();
        var client = server.CreateClient();
        
        // Act - Call tool 3 times
        for (int i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/mcp", new
            {
                jsonrpc = "2.0",
                method = "tools/call",
                @params = new
                {
                    name = "add_numbers",
                    arguments = new { A = 5, B = 3 }
                },
                id = i
            });
        }
        
        // Get metrics
        var response = await client.GetAsync("/metrics");
        var metrics = await response.Content.ReadFromJsonAsync<MetricsResponse>();
        
        // Assert
        var toolMetrics = metrics.Metrics.FirstOrDefault(m => m.Tool == "add_numbers");
        Assert.NotNull(toolMetrics);
        Assert.Equal(3, toolMetrics.Invocations);
        Assert.Equal(3, toolMetrics.Successes);
        Assert.Equal(0, toolMetrics.Failures);
        Assert.Equal(100.0, toolMetrics.SuccessRate);
    }
}
```

## Source Code

Full source code available at:
- **GitHub:** [Examples/MetricsMcpServer](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/MetricsMcpServer)
- **Tests:** [Examples/MetricsMcpServerTests](https://github.com/eyjolfurgudnivatne/mcp.gateway/tree/main/Examples/MetricsMcpServerTests)

## See Also

- [Lifecycle Hooks](/features/lifecycle-hooks/) - Complete lifecycle hooks guide
- [Authorization Example](/examples/authorization/) - Add role-based access control
- [API Reference: Lifecycle Hooks](/api/lifecycle-hooks/) - Complete API docs
