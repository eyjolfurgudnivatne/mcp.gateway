namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
    EventIdGenerator? _eventIdGenerator = null)  // NEW: Optional EventIdGenerator for SSE event IDs (v1.7.0)
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
}
