namespace Mcp.Gateway.Tools;

using System.Collections.Concurrent;

/// <summary>
/// Manages MCP sessions for the Streamable HTTP transport (v1.7.0).
/// Thread-safe session lifecycle management with configurable timeout.
/// </summary>
public sealed class SessionService
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
    private readonly TimeSpan _sessionTimeout;

    /// <summary>
    /// Initializes a new instance of SessionService with configurable timeout.
    /// </summary>
    /// <param name="sessionTimeout">Session timeout duration. Default: 30 minutes.</param>
    public SessionService(TimeSpan? sessionTimeout = null)
    {
        _sessionTimeout = sessionTimeout ?? TimeSpan.FromMinutes(30);
    }

    /// <summary>
    /// Creates a new session and returns the session ID.
    /// Session ID is a GUID in "N" format (32 hex digits without dashes).
    /// </summary>
    /// <returns>New session ID</returns>
    public string CreateSession()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new SessionInfo
        {
            Id = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            EventIdCounter = 0
        };

        _sessions[sessionId] = session;
        return sessionId;
    }

    /// <summary>
    /// Validates a session ID and updates last activity timestamp.
    /// Returns false if session doesn't exist or has expired.
    /// </summary>
    /// <param name="sessionId">Session ID to validate</param>
    /// <returns>True if session is valid and active, false otherwise</returns>
    public bool ValidateSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;

        // Check timeout
        if (DateTime.UtcNow - session.LastActivity > _sessionTimeout)
        {
            _sessions.TryRemove(sessionId, out _);
            return false;
        }

        // Update last activity
        session.LastActivity = DateTime.UtcNow;
        return true;
    }

    /// <summary>
    /// Deletes a session and removes it from the active sessions dictionary.
    /// </summary>
    /// <param name="sessionId">Session ID to delete</param>
    /// <returns>True if session was deleted, false if not found</returns>
    public bool DeleteSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return false;

        return _sessions.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Gets the next event ID for a session (atomically increments counter).
    /// Used for session-scoped SSE event IDs.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Next event ID for this session</returns>
    /// <exception cref="InvalidOperationException">If session not found</exception>
    public long GetNextEventId(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Interlocked.Increment(ref session.EventIdCounter);
        }

        throw new InvalidOperationException($"Session '{sessionId}' not found");
    }

    /// <summary>
    /// Gets session information by ID (for debugging/monitoring).
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Session info if found, null otherwise</returns>
    public SessionInfo? GetSession(string sessionId)
    {
        _sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    /// <summary>
    /// Gets the count of active sessions (for monitoring).
    /// </summary>
    public int ActiveSessionCount => _sessions.Count;

    /// <summary>
    /// Cleans up expired sessions (can be called periodically).
    /// Returns the number of sessions removed.
    /// </summary>
    public int CleanupExpiredSessions()
    {
        var expiredSessions = _sessions
            .Where(kvp => DateTime.UtcNow - kvp.Value.LastActivity > _sessionTimeout)
            .Select(kvp => kvp.Key)
            .ToList();

        var removedCount = 0;
        foreach (var sessionId in expiredSessions)
        {
            if (_sessions.TryRemove(sessionId, out _))
                removedCount++;
        }

        return removedCount;
    }
}
