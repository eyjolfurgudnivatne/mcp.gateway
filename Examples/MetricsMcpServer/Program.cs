using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Lifecycle;

var builder = WebApplication.CreateBuilder(args);

// Detect stdio mode
var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Register ToolService + ToolInvoker
builder.AddToolsService();

// NEW (v1.8.0): Add lifecycle hooks for monitoring
builder.AddToolLifecycleHook<LoggingToolLifecycleHook>();  // Log all tool invocations
builder.AddToolLifecycleHook<MetricsToolLifecycleHook>();  // Track metrics

var app = builder.Build();

// stdio mode for GitHub Copilot
if (isStdioMode)
{
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MetricsMcpServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// WebSockets for streaming
app.UseWebSockets();

// MCP 2025-11-25 Streamable HTTP (v1.7.0+)
app.MapStreamableHttpEndpoint("/mcp");

// Legacy endpoints
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");

// NEW (v1.8.0): Metrics endpoint
app.MapGet("/metrics", (IEnumerable<IToolLifecycleHook> hooks) =>
{
    // Find MetricsToolLifecycleHook
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

app.Run();
