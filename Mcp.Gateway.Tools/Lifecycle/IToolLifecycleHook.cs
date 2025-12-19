namespace Mcp.Gateway.Tools.Lifecycle;

/// <summary>
/// Hook interface for monitoring tool lifecycle events (v1.8.0).
/// Implement this interface to track tool invocations, performance, and errors.
/// </summary>
/// <remarks>
/// Hooks are invoked in order:
/// 1. OnToolInvoking (before tool execution)
/// 2. OnToolCompleted (on success) OR OnToolFailed (on error)
/// 
/// Multiple hooks can be registered and will be invoked in registration order.
/// Hooks should not throw exceptions - they are fire-and-forget.
/// </remarks>
public interface IToolLifecycleHook
{
    /// <summary>
    /// Called before a tool is invoked.
    /// </summary>
    /// <param name="toolName">Name of the tool being invoked</param>
    /// <param name="request">JSON-RPC request message</param>
    Task OnToolInvokingAsync(string toolName, JsonRpcMessage request);
    
    /// <summary>
    /// Called after a tool completes successfully.
    /// </summary>
    /// <param name="toolName">Name of the tool that completed</param>
    /// <param name="response">JSON-RPC response message</param>
    /// <param name="duration">Time taken to execute the tool</param>
    Task OnToolCompletedAsync(string toolName, JsonRpcMessage response, TimeSpan duration);
    
    /// <summary>
    /// Called when a tool invocation fails with an exception.
    /// </summary>
    /// <param name="toolName">Name of the tool that failed</param>
    /// <param name="error">Exception that caused the failure</param>
    /// <param name="duration">Time elapsed before failure</param>
    Task OnToolFailedAsync(string toolName, Exception error, TimeSpan duration);
}
