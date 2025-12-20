---
layout: mcp-default
title: Lifecycle Hooks
description: Monitor and track tool invocations for metrics, logging, and production monitoring
breadcrumbs:
  - title: Home
    url: /
  - title: Features
    url: /features/
  - title: Lifecycle Hooks
    url: /features/lifecycle-hooks/
toc: true
---

# Tool Lifecycle Hooks

**Added in:** v1.8.0  
**Status:** Production-ready  
**Purpose:** Monitor and track tool invocations for metrics, logging, and production monitoring

## Overview

Lifecycle hooks allow you to intercept tool invocations and track key metrics:
- **Invocation count** - How many times each tool is called
- **Success/Failure rate** - Track tool reliability
- **Duration** - Monitor performance
- **Error types** - Identify common failure patterns

Perfect for:
- 📊 **Metrics** - Prometheus, Application Insights, DataDog
- 📝 **Logging** - Structured logging with ILogger
- 🔍 **Debugging** - Track tool behavior in production
- 📈 **Monitoring** - SLA tracking and alerting

## Quick Start

### 1. Register Built-in Hooks

```csharp
using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Lifecycle;

var builder = WebApplication.CreateBuilder(args);

// Register ToolService
builder.AddToolsService();

// Add lifecycle hooks (v1.8.0)
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();  // ILogger integration
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();  // In-memory metrics

var app = builder.Build();
```

### 2. Expose Metrics Endpoint

```csharp
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    
    if (metricsHook == null)
    {
        return Results.Json(new { error = "MetricsToolLifecycleHook not registered" });
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
```

## Built-in Hooks

### LoggingToolLifecycleHook

Simple logging to `ILogger` for debugging:

```csharp
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();
```

**Output:**
```
[2025-12-19 16:30:00] Tool 'add_numbers' invoked with ID: req-123
[2025-12-19 16:30:00] Tool 'add_numbers' completed in 1.23ms with ID: req-123
[2025-12-19 16:30:01] Tool 'divide' failed after 0.89ms: Cannot divide by zero
```

### MetricsToolLifecycleHook

In-memory metrics tracking per tool:

```csharp
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();
```

**Tracked metrics:**
- `InvocationCount` - Total number of calls
- `SuccessCount` - Successful invocations
- `FailureCount` - Failed invocations
- `SuccessRate` - Percentage of successful invocations (0.0 to 1.0)
- `AverageDuration` - Mean execution time for successful calls
- `MinDuration` - Fastest execution time
- `MaxDuration` - Slowest execution time
- `ErrorCounts` - Dictionary of error types with counts

## Custom Hooks

Implement `IToolLifecycleHook` for custom behavior:

```csharp
using Mcp.Gateway.Tools.Lifecycle;

public interface IToolLifecycleHook
{
    Task OnToolInvokingAsync(string toolName, JsonRpcMessage request);
    Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration);
    Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration);
}
```

### Example: Prometheus Integration

```csharp
using Mcp.Gateway.Tools.Lifecycle;
using Prometheus;

public class PrometheusHook : IToolLifecycleHook
{
    private readonly Counter _invocations;
    private readonly Counter _successes;
    private readonly Counter _failures;
    private readonly Histogram _duration;
    
    public PrometheusHook()
    {
        _invocations = Metrics.CreateCounter(
            "mcp_tool_invocations_total",
            "Total tool invocations",
            new CounterConfiguration { LabelNames = new[] { "tool" } });
        
        _successes = Metrics.CreateCounter(
            "mcp_tool_successes_total",
            "Successful tool invocations",
            new CounterConfiguration { LabelNames = new[] { "tool" } });
        
        _failures = Metrics.CreateCounter(
            "mcp_tool_failures_total",
            "Failed tool invocations",
            new CounterConfiguration { LabelNames = new[] { "tool", "error_type" } });
        
        _duration = Metrics.CreateHistogram(
            "mcp_tool_duration_seconds",
            "Tool execution duration in seconds",
            new HistogramConfiguration { LabelNames = new[] { "tool" } });
    }
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        _invocations.WithLabels(toolName).Inc();
        return Task.CompletedTask;
    }
    
    public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
    {
        _successes.WithLabels(toolName).Inc();
        _duration.WithLabels(toolName).Observe(duration.TotalSeconds);
        return Task.CompletedTask;
    }
    
    public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
    {
        _failures.WithLabels(toolName, error.GetType().Name).Inc();
        return Task.CompletedTask;
    }
}

// Register
builder.AddToolLifecycleHook<PrometheusHook>();

// Expose Prometheus metrics
app.MapMetrics();  // Requires Prometheus.AspNetCore package
```

### Example: Application Insights

```csharp
using Mcp.Gateway.Tools.Lifecycle;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

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
    
    public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
    {
        var metric = new MetricTelemetry(
            "ToolDuration",
            duration.TotalMilliseconds);
        
        metric.Properties["tool"] = toolName;
        metric.Properties["status"] = "success";
        
        _telemetry.TrackMetric(metric);
        
        return Task.CompletedTask;
    }
    
    public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
    {
        var exception = new ExceptionTelemetry(error);
        exception.Properties["tool"] = toolName;
        exception.Properties["duration"] = duration.TotalMilliseconds.ToString();
        
        _telemetry.TrackException(exception);
        
        return Task.CompletedTask;
    }
}

// Register
builder.Services.AddApplicationInsightsTelemetry();
builder.AddToolLifecycleHook<ApplicationInsightsHook>();
```

## Hook Behavior

### Invocation Order

Hooks are invoked in the order they are registered:

```csharp
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();   // Called first
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();   // Called second
builder.AddToolLifecycleHook<PrometheusHook>();             // Called third
```

### Fire-and-Forget Pattern

Hooks are executed asynchronously and **do not block** tool execution:

```csharp
// Pseudo-code showing execution flow:
await OnToolInvokingAsync(toolName, request);  // Fire hooks (don't await result)
var result = await InvokeTool();                // Execute tool
await OnToolCompletedAsync(toolName, response); // Fire hooks (don't await result)
```

### Exception Handling

Hook exceptions are **caught and logged** - they never propagate to the tool invocation:

```csharp
try
{
    await hook.OnToolInvokingAsync(toolName, request);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Lifecycle hook {HookType} threw exception", hook.GetType().Name);
    // Tool execution continues normally
}
```

**Best practice:** Hooks should be defensive and never throw exceptions.

## Smart Filtering

Hooks are **only invoked for user-defined tools**, not MCP protocol methods:

✅ **Tracked:**
- `add_numbers` (user tool)
- `divide` (user tool)
- `slow_operation` (user tool)

❌ **Not tracked:**
- `initialize` (MCP protocol)
- `tools/list` (MCP protocol)
- `tools/call` (MCP protocol wrapper)
- `resources/list` (MCP protocol)

This prevents noise in metrics and keeps tracking focused on actual business logic.

## Example Metrics Output

### JSON Format

```json
{
  "timestamp": "2025-12-19T16:30:00Z",
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
        "TargetInvocationException": 2
      }
    }
  ]
}
```

## Performance

### Overhead

Lifecycle hooks add minimal overhead:
- **Fire-and-forget** - Hooks don't block tool execution
- **Async execution** - Hooks run in parallel with tool logic
- **No allocations** - Metrics use lock-free `Interlocked` operations

**Benchmark results** (v1.8.0):
```
Tool without hooks:     1.23ms
Tool with LoggingHook:  1.25ms (+2%)
Tool with MetricsHook:  1.24ms (+1%)
Tool with both hooks:   1.26ms (+2.5%)
```

### Memory

**MetricsToolLifecycleHook** memory usage:
- ~200 bytes per tool (ToolMetrics object)
- ~50 bytes per unique error type
- Example: 100 tools with 5 error types each = ~45 KB

## See Also

- [Authorization](/features/authorization/) - Use lifecycle hooks for role-based access control
- [Examples: Metrics Server](/examples/metrics/) - Complete example with metrics endpoint
- [API Reference: Lifecycle Hooks](/api/lifecycle-hooks/) - Complete API documentation
