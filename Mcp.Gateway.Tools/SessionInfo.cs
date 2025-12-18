namespace Mcp.Gateway.Tools;

/// <summary>
/// Represents session information for MCP session management (v1.7.0).
/// Used to track active sessions and their state.
/// </summary>
public sealed class SessionInfo
{
    /// <summary>
    /// Unique session identifier (GUID format without dashes).
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// Timestamp when the session was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Timestamp of the last activity on this session (UTC).
    /// Updated on every request to prevent timeout.
    /// </summary>
    public DateTime LastActivity { get; set; }
    
    /// <summary>
    /// Event ID counter for session-scoped SSE events.
    /// Atomically incremented for each SSE event in this session.
    /// </summary>
    public long EventIdCounter;
    
    /// <summary>
    /// Message buffer for SSE notification replay (v1.7.0 Phase 2).
    /// Stores recent messages for Last-Event-ID resumption.
    /// </summary>
    public MessageBuffer MessageBuffer { get; } = new(100);
}
