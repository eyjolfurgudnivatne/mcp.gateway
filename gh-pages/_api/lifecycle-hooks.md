---
layout: mcp-default
title: Lifecycle Hooks API Reference
description: Complete API reference for tool lifecycle hooks
breadcrumbs:
  - title: Home
    url: /
  - title: API Reference
    url: /api/
  - title: Lifecycle Hooks API
    url: /api/lifecycle-hooks/
toc: true
---

# Lifecycle Hooks API Reference

Complete API reference for monitoring and tracking tool invocations.

## Overview

**Added in:** v1.8.0  
**Namespace:** `Mcp.Gateway.Tools.Lifecycle`

Lifecycle hooks allow you to intercept tool invocations for:
- 📊 **Metrics** - Track invocation counts, success rates, duration
- 📝 **Logging** - Structured logging with ILogger
- 🔐 **Authorization** - Role-based access control
- 🔍 **Debugging** - Track tool behavior in production
- 📈 **Monitoring** - SLA tracking and alerting

## Quick Reference

| Interface/Class | Description |
|-----------------|-------------|
| `IToolLifecycleHook` | Core interface for lifecycle hooks |
| `LoggingToolLifecycleHook` | Built-in logging hook |
| `MetricsToolLifecycleHook` | Built-in metrics hook |
| `ToolMetrics` | Metrics data structure |

## IToolLifecycleHook

Core interface for implementing lifecycle hooks.

### Interface Definition

```csharp
namespace Mcp.Gateway.Tools.Lifecycle;

public interface IToolLifecycleHook
{
    /// <summary>
    /// Called before a tool is invoked.
    /// </summary>
    Task OnToolInvokingAsync(string toolName, JsonRpcMessage request);
    
    /// <summary>
    /// Called after a tool completes successfully.
    /// </summary>
    Task OnToolCompletedAsync(
        string toolName,
        JsonRpcMessage response,
        TimeSpan duration);
    
    /// <summary>
    /// Called when a tool invocation fails with an exception.
    /// </summary>
    Task OnToolFailedAsync(
        string toolName,
        Exception error,
        TimeSpan duration);
}
```

### Method Parameters

#### OnToolInvokingAsync

| Parameter | Type | Description |
|-----------|------|-------------|
| `toolName` | string | Name of the tool being invoked |
| `request` | JsonRpcMessage | Full JSON-RPC request message |

**When called:** Before tool execution starts

#### OnToolCompletedAsync

| Parameter | Type | Description |
|-----------|------|-------------|
| `toolName` | string | Name of the tool that completed |
| `response` | JsonRpcMessage | Full JSON-RPC response message |
| `duration` | TimeSpan | Execution time |

**When called:** After successful tool execution

#### OnToolFailedAsync

| Parameter | Type | Description |
|-----------|------|-------------|
| `toolName` | string | Name of the tool that failed |
| `error` | Exception | Exception that was thrown |
| `duration` | TimeSpan | Execution time before failure |

**When called:** After tool execution fails with exception

## Registration

### AddToolLifecycleHook Extension

```csharp
namespace Mcp.Gateway.Tools;

public static class ToolExtensions
{
    /// <summary>
    /// Adds a tool lifecycle hook for monitoring tool invocations (v1.8.0).
    /// Multiple hooks can be registered and will be invoked in registration order.
    /// </summary>
    public static WebApplicationBuilder AddToolLifecycleHook<T>(
        this WebApplicationBuilder builder)
        where T : class, Lifecycle.IToolLifecycleHook;
}
```

### Usage

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register MCP Gateway
builder.AddToolsService();

// Add lifecycle hooks (invoked in registration order)
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();
builder.AddToolLifecycleHook<CustomHook>();

var app = builder.Build();
```

## Built-in Hooks

### LoggingToolLifecycleHook

Simple logging to `ILogger` for debugging.

```csharp
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();
```

**Output:**
```
[Information] Tool 'add_numbers' invoked with ID: req-123
[Information] Tool 'add_numbers' completed in 1.23ms with ID: req-123
[Warning] Tool 'divide' failed after 0.89ms: Cannot divide by zero
```

**Log Levels:**
- `Information` - Tool invoked, completed
- `Warning` - Tool failed

### MetricsToolLifecycleHook

In-memory metrics tracking per tool.

```csharp
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();
```

**Tracked metrics:**
- `InvocationCount` - Total calls
- `SuccessCount` - Successful invocations
- `FailureCount` - Failed invocations
- `SuccessRate` - Percentage (0.0 to 1.0)
- `AverageDuration` - Mean execution time
- `MinDuration` - Fastest execution
- `MaxDuration` - Slowest execution
- `ErrorCounts` - Dictionary of error types

**Accessing metrics:**

```csharp
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    var metricsHook = hooks.OfType<MetricsToolLifecycleHook>().FirstOrDefault();
    
    if (metricsHook == null)
    {
        return Results.Json(new { error = "Hook not registered" });
    }
    
    var metrics = metricsHook.GetMetrics();
    
    return Results.Json(new
    {
        timestamp = DateTime.UtcNow,
        metrics = metrics.Select(kvp => new
        {
            tool = kvp.Key,
            invocations = kvp.Value.InvocationCount,
            successes = kvp.Value.SuccessCount,
            failures = kvp.Value.FailureCount,
            successRate = kvp.Value.SuccessRate,
            avgDuration = kvp.Value.AverageDuration.TotalMilliseconds
        })
    });
});
```

## ToolMetrics

Data structure for tool metrics.

### Class Definition

```csharp
namespace Mcp.Gateway.Tools.Lifecycle;

public class ToolMetrics
{
    public string ToolName { get; }
    public long InvocationCount { get; }
    public long SuccessCount { get; }
    public long FailureCount { get; }
    public TimeSpan AverageDuration { get; }
    public TimeSpan MinDuration { get; }
    public TimeSpan MaxDuration { get; }
    public ConcurrentDictionary<string, long> ErrorCounts { get; }
    
    /// <summary>
    /// Success rate as a value between 0.0 and 1.0
    /// </summary>
    public double SuccessRate => InvocationCount > 0 
        ? (double)SuccessCount / InvocationCount 
        : 0.0;
}
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ToolName` | string | Name of the tool |
| `InvocationCount` | long | Total invocations |
| `SuccessCount` | long | Successful invocations |
| `FailureCount` | long | Failed invocations |
| `AverageDuration` | TimeSpan | Average execution time (successes only) |
| `MinDuration` | TimeSpan | Fastest execution time |
| `MaxDuration` | TimeSpan | Slowest execution time |
| `ErrorCounts` | Dictionary | Count by error type |
| `SuccessRate` | double | Success rate (0.0 to 1.0) |

## Custom Hooks

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
    
    public Task OnToolCompletedAsync(
        string toolName,
        JsonRpcMessage response,
        TimeSpan duration)
    {
        _successes.WithLabels(toolName).Inc();
        _duration.WithLabels(toolName).Observe(duration.TotalSeconds);
        return Task.CompletedTask;
    }
    
    public Task OnToolFailedAsync(
        string toolName,
        Exception error,
        TimeSpan duration)
    {
        _failures.WithLabels(toolName, error.GetType().Name).Inc();
        return Task.CompletedTask;
    }
}

// Register
builder.AddToolLifecycleHook<PrometheusHook>();

// Expose endpoint
app.MapMetrics();  // /metrics
```

### Example: Authorization Hook

```csharp
using Mcp.Gateway.Tools.Lifecycle;
using System.Reflection;

public class AuthorizationHook : IToolLifecycleHook
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Dictionary<string, MethodInfo> _toolMethods = new();
    
    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return Task.CompletedTask;
        
        var method = GetToolMethod(toolName);
        if (method == null) return Task.CompletedTask;
        
        // Check [RequireRole] attribute
        var requiredRoles = method.GetCustomAttributes<RequireRoleAttribute>()
            .Select(attr => attr.Role)
            .ToList();
        
        if (!requiredRoles.Any()) return Task.CompletedTask;
        
        // Check user roles
        var userRoles = httpContext.Items["UserRoles"] as List<string> ?? new();
        
        if (!requiredRoles.Any(role => userRoles.Contains(role)))
        {
            throw new ToolInvalidParamsException(
                $"Insufficient permissions. Required: {string.Join(" or ", requiredRoles)}",
                toolName);
        }
        
        return Task.CompletedTask;
    }
    
    // ... other methods
    
    private MethodInfo? GetToolMethod(string toolName)
    {
        // Implementation: scan assemblies, cache results
    }
}
```

## Hook Behavior

### Invocation Order

Hooks are invoked in registration order:

```csharp
builder.AddToolLifecycleHook<LoggingHook>();    // Called 1st
builder.AddToolLifecycleHook<MetricsHook>();    // Called 2nd
builder.AddToolLifecycleHook<AuthHook>();       // Called 3rd
```

### Fire-and-Forget Pattern

Hooks are executed asynchronously and **do not block** tool execution:

```csharp
// Pseudo-code
await OnToolInvokingAsync(...);  // Fire hooks (don't await completion)
var result = await InvokeTool();  // Execute tool
await OnToolCompletedAsync(...);  // Fire hooks (don't await completion)
```

### Exception Handling

Hook exceptions are **caught and logged** - they never propagate:

```csharp
try
{
    await hook.OnToolInvokingAsync(toolName, request);
}
catch (Exception ex)
{
    _logger.LogWarning(ex, 
        "Lifecycle hook {HookType} threw exception", 
        hook.GetType().Name);
    // Tool execution continues
}
```

### Smart Filtering

Hooks are **only invoked for user-defined tools**, not MCP protocol methods:

✅ **Tracked:**
- `add_numbers`
- `divide`
- `custom_tool`

❌ **Not tracked:**
- `initialize`
- `tools/list`
- `tools/call`
- `resources/list`

## Performance

### Overhead

Minimal impact on tool execution:

| Scenario | Duration | Overhead |
|----------|----------|----------|
| Tool without hooks | 1.23ms | - |
| Tool + LoggingHook | 1.25ms | +2% |
| Tool + MetricsHook | 1.24ms | +1% |
| Tool + both hooks | 1.26ms | +2.5% |

### Memory

**MetricsToolLifecycleHook:**
- ~200 bytes per tool
- ~50 bytes per unique error type
- Example: 100 tools = ~20 KB

## Best Practices

### 1. Keep Hooks Fast

```csharp
// ✅ GOOD - Fast operation
public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
{
    _counter.Increment();
    return Task.CompletedTask;
}

// ❌ BAD - Slow operation
public async Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
{
    await _database.SaveAsync(...);  // Blocks tool execution!
}
```

### 2. Never Throw Exceptions

```csharp
// ✅ GOOD - Defensive
public Task OnToolCompletedAsync(...)
{
    try
    {
        // Hook logic
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Hook failed");
    }
    return Task.CompletedTask;
}
```

### 3. Use Dependency Injection

```csharp
public class CustomHook : IToolLifecycleHook
{
    private readonly ILogger<CustomHook> _logger;
    private readonly IMyService _service;
    
    public CustomHook(ILogger<CustomHook> logger, IMyService service)
    {
        _logger = logger;
        _service = service;
    }
    
    // ... interface methods
}

// Register dependencies
builder.Services.AddSingleton<IMyService, MyService>();
builder.AddToolLifecycleHook<CustomHook>();
```

## See Also

- [Lifecycle Hooks Guide](/features/lifecycle-hooks/) - Complete guide with examples
- [Authorization](/features/authorization/) - Role-based access control using hooks
- [Examples: Metrics Server](/examples/metrics/) - Complete working example
- [Examples: Authorization Server](/examples/authorization/) - Authorization with hooks
