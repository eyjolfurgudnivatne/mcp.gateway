namespace Mcp.Gateway.Tools;

/// <summary>
/// Generates globally unique event IDs for SSE messages.
/// Thread-safe for concurrent access.
/// </summary>
public sealed class EventIdGenerator
{
    private long _globalCounter = 0;
    
    /// <summary>
    /// Generates a globally unique event ID.
    /// </summary>
    /// <param name="sessionId">Optional session ID for scoping event IDs to a session</param>
    /// <returns>Event ID string (e.g., "42" or "session123-42")</returns>
    public string GenerateEventId(string? sessionId = null)
    {
        var id = Interlocked.Increment(ref _globalCounter);
        
        return !string.IsNullOrEmpty(sessionId)
            ? $"{sessionId}-{id}"  // Session-scoped: "session123-42"
            : $"{id}";             // Global: "42"
    }
    
    /// <summary>
    /// Resets the global counter (primarily for testing).
    /// </summary>
    internal void Reset()
    {
        Interlocked.Exchange(ref _globalCounter, 0);
    }
}
