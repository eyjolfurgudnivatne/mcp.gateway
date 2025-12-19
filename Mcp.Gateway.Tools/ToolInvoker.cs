namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mcp.Gateway.Tools.Lifecycle;
using System.Diagnostics;

/// <summary>
/// Handles JSON-RPC tool invocation over multiple transports (HTTP, WebSocket, SSE, stdio).
/// This is the core partial class - see other ToolInvoker.*.cs files for transport-specific functionality.
/// </summary>
/// <remarks>
/// Partial class files:
/// - ToolInvoker.cs (this file) - Core infrastructure and utilities
/// - ToolInvoker.Http.cs - HTTP transport
/// - ToolInvoker.WebSocket.cs - WebSocket transport
/// - ToolInvoker.Sse.cs - Server-Sent Events transport (v1.5.0+, event IDs v1.7.0+)
/// - ToolInvoker.Protocol.cs - MCP protocol handlers
/// - ToolInvoker.Resources.cs - MCP Resources support (v1.5.0)
/// - ToolInvoker.Notifications.cs - Notifications support (v1.6.0)
/// </remarks>
public partial class ToolInvoker(
    ToolService _toolService, 
    ILogger<ToolInvoker> _logger,
    Notifications.INotificationSender? _notificationSender = null,
    EventIdGenerator? _eventIdGenerator = null,  // Optional EventIdGenerator for SSE event IDs (v1.7.0)
    IEnumerable<IToolLifecycleHook>? _lifecycleHooks = null)  // NEW: Optional lifecycle hooks (v1.8.0)
{
    /// <summary>
    /// Default buffer size for WebSocket frame accumulation (64KB)
    /// </summary>
    protected const int DefaultBufferSize = 64 * 1024;

    /// <summary>
    /// Detects the transport type from HttpContext.
    /// Used for capability-based tool filtering.
    /// </summary>
    /// <param name="context">HttpContext (null for stdio)</param>
    /// <returns>Transport type: "stdio", "http", "ws", or "sse"</returns>
    protected static string DetectTransport(HttpContext? context)
    {
        if (context == null) return "stdio";
        if (context.WebSockets.IsWebSocketRequest) return "ws";
        if (context.Request.Headers.Accept.ToString().Contains("text/event-stream")) return "sse";
        return "http";
    }

    /// <summary>
    /// Invokes lifecycle hooks for tool invocation (v1.8.0).
    /// Fire-and-forget - hooks should not throw exceptions.
    /// EXCEPTION: ToolInvalidParamsException is re-thrown (used for authorization).
    /// </summary>
    protected async Task InvokeLifecycleHooksAsync(
        Func<IToolLifecycleHook, Task> hookAction)
    {
        if (_lifecycleHooks == null) return;

        foreach (var hook in _lifecycleHooks)
        {
            try
            {
                await hookAction(hook).ConfigureAwait(false);
            }
            catch (ToolInvalidParamsException)
            {
                // Re-throw ToolInvalidParamsException (used for authorization)
                throw;
            }
            catch (Exception ex)
            {
                // Log but don't propagate other hook errors
                _logger.LogWarning(ex, "Lifecycle hook {HookType} threw exception", hook.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Invokes a tool with lifecycle hook support (v1.8.0).
    /// Wraps tool invocation with OnToolInvoking, OnToolCompleted, and OnToolFailed hooks.
    /// </summary>
    protected async Task<object?> InvokeToolWithHooksAsync(
        string toolName,
        JsonRpcMessage request,
        Func<Task<object?>> toolInvocation)
    {
        var stopwatch = Stopwatch.StartNew();

        // Fire OnToolInvoking hook
        await InvokeLifecycleHooksAsync(hook => 
            hook.OnToolInvokingAsync(toolName, request));

        try
        {
            // Invoke tool
            var result = await toolInvocation().ConfigureAwait(false);

            stopwatch.Stop();

            // Fire OnToolCompleted hook
            // Extract JsonRpcMessage for hook (if available)
            JsonRpcMessage? responseMessage = null;
            if (result is JsonRpcMessage directMessage)
            {
                responseMessage = directMessage;
            }
            else if (result != null)
            {
                // Create a synthetic response for hooks
                responseMessage = ToolResponse.Success(request.Id, result);
            }

            if (responseMessage != null)
            {
                await InvokeLifecycleHooksAsync(hook => 
                    hook.OnToolCompletedAsync(toolName, responseMessage, stopwatch.Elapsed));
            }

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Fire OnToolFailed hook
            await InvokeLifecycleHooksAsync(hook => 
                hook.OnToolFailedAsync(toolName, ex, stopwatch.Elapsed));

            // Re-throw exception (don't swallow it!)
            throw;
        }
    }
}
