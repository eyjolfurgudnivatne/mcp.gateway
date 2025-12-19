namespace Mcp.Gateway.Tools.Lifecycle;

using Microsoft.Extensions.Logging;

/// <summary>
/// Simple logging hook that writes tool lifecycle events to ILogger (v1.8.0).
/// Useful for debugging and basic monitoring.
/// </summary>
public class LoggingToolLifecycleHook : IToolLifecycleHook
{
    private readonly ILogger<LoggingToolLifecycleHook> _logger;

    public LoggingToolLifecycleHook(ILogger<LoggingToolLifecycleHook> logger)
    {
        _logger = logger;
    }

    public Task OnToolInvokingAsync(string toolName, JsonRpcMessage request)
    {
        _logger.LogInformation(
            "Tool '{ToolName}' invoked with ID: {RequestId}",
            toolName,
            request.IdAsString);
        
        return Task.CompletedTask;
    }

    public Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration)
    {
        _logger.LogInformation(
            "Tool '{ToolName}' completed in {Duration}ms with ID: {RequestId}",
            toolName,
            duration.TotalMilliseconds,
            response.IdAsString);
        
        return Task.CompletedTask;
    }

    public Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration)
    {
        _logger.LogError(
            error,
            "Tool '{ToolName}' failed after {Duration}ms: {ErrorMessage}",
            toolName,
            duration.TotalMilliseconds,
            error.Message);
        
        return Task.CompletedTask;
    }
}
