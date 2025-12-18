namespace Mcp.Gateway.Tools;

/// <summary>
/// Represents an SSE (Server-Sent Events) message with event ID for resumability.
/// Used for MCP 2025-11-25 Streamable HTTP transport.
/// </summary>
/// <param name="Id">Globally unique event ID (e.g., "session123-42" or "42")</param>
/// <param name="Event">Optional event type (defaults to "message"). Examples: "message", "done", "error"</param>
/// <param name="Data">JSON-RPC message or notification payload</param>
/// <param name="Retry">Optional retry interval in milliseconds for client reconnection</param>
public sealed record SseEventMessage(
    string Id,
    string? Event,
    object Data,
    int? Retry = null)
{
    /// <summary>
    /// Creates a standard message event (most common case).
    /// </summary>
    public static SseEventMessage CreateMessage(string id, object data) =>
        new(id, "message", data);
    
    /// <summary>
    /// Creates a "done" event to signal completion of a stream.
    /// </summary>
    public static SseEventMessage CreateDone(string id) =>
        new(id, "done", new { });
    
    /// <summary>
    /// Creates an "error" event for error notifications.
    /// </summary>
    public static SseEventMessage CreateError(string id, JsonRpcError error) =>
        new(id, "error", new { error });
    
    /// <summary>
    /// Creates a keep-alive ping (no event ID, just a comment).
    /// </summary>
    public static SseEventMessage CreateKeepAlive() =>
        new(string.Empty, null, new { });
}
