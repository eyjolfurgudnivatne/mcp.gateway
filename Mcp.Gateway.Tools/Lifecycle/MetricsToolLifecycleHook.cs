namespace Mcp.Gateway.Tools.Lifecycle;

using System.Collections.Concurrent;

/// <summary>
/// In-memory metrics hook for tool lifecycle events (v1.8.0).
/// Tracks invocation count, success/failure rates, and average duration.
/// 
/// Use this for simple monitoring or as a base for Prometheus/Application Insights integration.
/// </summary>
/// <remarks>
/// Metrics are stored in-memory and reset on application restart.
/// For persistent metrics, integrate with Prometheus, Application Insights, or similar.
/// </remarks>
public class MetricsToolLifecycleHook : IToolLifecycleHook
{
    private readonly ConcurrentDictionary<string, ToolMetrics> _metrics = new();

    /// <summary>
    /// Gets current metrics for all tools.
    /// </summary>
    public IReadOnlyDictionary<string, ToolMetrics> GetMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// Gets metrics for a specific tool.
    /// </summary>
    public ToolMetrics? GetMetrics(string toolName)
    {
        return _metrics.TryGetValue(toolName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Resets all metrics (useful for testing).
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
    }

    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        var metrics = _metrics.GetOrAdd(toolName, _ => new ToolMetrics(toolName));
        Interlocked.Increment(ref metrics.InvocationCount);
        
        return Task.CompletedTask;
    }

    public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
    {
        var metrics = _metrics.GetOrAdd(toolName, _ => new ToolMetrics(toolName));
        
        Interlocked.Increment(ref metrics.SuccessCount);
        
        // Update average duration (simple moving average)
        lock (metrics)
        {
            var totalDuration = metrics.AverageDuration.TotalMilliseconds * (metrics.SuccessCount - 1);
            metrics.AverageDuration = TimeSpan.FromMilliseconds(
                (totalDuration + duration.TotalMilliseconds) / metrics.SuccessCount);
            
            // Track min/max duration
            if (duration < metrics.MinDuration || metrics.MinDuration == TimeSpan.Zero)
                metrics.MinDuration = duration;
            
            if (duration > metrics.MaxDuration)
                metrics.MaxDuration = duration;
        }
        
        return Task.CompletedTask;
    }

    public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
    {
        var metrics = _metrics.GetOrAdd(toolName, _ => new ToolMetrics(toolName));
        
        Interlocked.Increment(ref metrics.FailureCount);
        
        // Track error types
        var errorType = error.GetType().Name;
        metrics.ErrorCounts.AddOrUpdate(errorType, 1, (_, count) => count + 1);
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Metrics for a single tool.
/// </summary>
public class ToolMetrics
{
    public ToolMetrics(string toolName)
    {
        ToolName = toolName;
    }

    /// <summary>
    /// Name of the tool.
    /// </summary>
    public string ToolName { get; }
    
    /// <summary>
    /// Total number of invocations (success + failure).
    /// </summary>
    public long InvocationCount;
    
    /// <summary>
    /// Number of successful invocations.
    /// </summary>
    public long SuccessCount;
    
    /// <summary>
    /// Number of failed invocations.
    /// </summary>
    public long FailureCount;
    
    /// <summary>
    /// Average duration of successful invocations.
    /// </summary>
    public TimeSpan AverageDuration { get; set; }
    
    /// <summary>
    /// Minimum duration of successful invocations.
    /// </summary>
    public TimeSpan MinDuration { get; set; }
    
    /// <summary>
    /// Maximum duration of successful invocations.
    /// </summary>
    public TimeSpan MaxDuration { get; set; }
    
    /// <summary>
    /// Count of errors by exception type.
    /// </summary>
    public ConcurrentDictionary<string, long> ErrorCounts { get; } = new();
    
    /// <summary>
    /// Success rate (0.0 to 1.0).
    /// </summary>
    public double SuccessRate => InvocationCount > 0 
        ? (double)SuccessCount / InvocationCount 
        : 0.0;
}
