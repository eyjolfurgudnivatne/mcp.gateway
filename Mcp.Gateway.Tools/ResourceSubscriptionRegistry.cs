namespace Mcp.Gateway.Tools;

using System.Collections.Concurrent;

/// <summary>
/// Registry for tracking resource subscriptions per session (v1.8.0).
/// Supports exact URI matching only (wildcards deferred to v1.9.0).
/// </summary>
public class ResourceSubscriptionRegistry
{
    // sessionId → Set of subscribed resource URIs
    private readonly ConcurrentDictionary<string, HashSet<string>> _subscriptions = new();
    private readonly object _lock = new();

    /// <summary>
    /// Subscribe a session to a specific resource URI.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="uri">Resource URI (exact match only)</param>
    /// <returns>True if subscription was added, false if already subscribed</returns>
    public bool Subscribe(string sessionId, string uri)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("URI cannot be null or empty", nameof(uri));

        lock (_lock)
        {
            var subscriptions = _subscriptions.GetOrAdd(sessionId, _ => new HashSet<string>(StringComparer.Ordinal));
            return subscriptions.Add(uri);
        }
    }

    /// <summary>
    /// Unsubscribe a session from a specific resource URI.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="uri">Resource URI</param>
    /// <returns>True if subscription was removed, false if not subscribed</returns>
    public bool Unsubscribe(string sessionId, string uri)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID cannot be null or empty", nameof(sessionId));
        
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("URI cannot be null or empty", nameof(uri));

        lock (_lock)
        {
            if (_subscriptions.TryGetValue(sessionId, out var subscriptions))
            {
                var removed = subscriptions.Remove(uri);
                
                // Clean up empty subscription sets
                if (subscriptions.Count == 0)
                {
                    _subscriptions.TryRemove(sessionId, out _);
                }
                
                return removed;
            }
            
            return false;
        }
    }

    /// <summary>
    /// Check if a session is subscribed to a specific resource URI.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="uri">Resource URI (exact match)</param>
    /// <returns>True if subscribed, false otherwise</returns>
    public bool IsSubscribed(string sessionId, string uri)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(uri))
            return false;

        lock (_lock)
        {
            return _subscriptions.TryGetValue(sessionId, out var subscriptions) 
                && subscriptions.Contains(uri);
        }
    }

    /// <summary>
    /// Get all session IDs subscribed to a specific resource URI.
    /// </summary>
    /// <param name="uri">Resource URI (exact match)</param>
    /// <returns>List of session IDs subscribed to the URI</returns>
    public IReadOnlyList<string> GetSubscribedSessions(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return Array.Empty<string>();

        lock (_lock)
        {
            return _subscriptions
                .Where(kvp => kvp.Value.Contains(uri))
                .Select(kvp => kvp.Key)
                .ToList();
        }
    }

    /// <summary>
    /// Get all subscribed resource URIs for a session.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Set of subscribed URIs</returns>
    public IReadOnlySet<string> GetSubscriptions(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return new HashSet<string>();

        lock (_lock)
        {
            if (_subscriptions.TryGetValue(sessionId, out var subscriptions))
            {
                return new HashSet<string>(subscriptions);
            }
            
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// Remove all subscriptions for a session (e.g., on session expiry).
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Number of subscriptions removed</returns>
    public int ClearSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return 0;

        lock (_lock)
        {
            if (_subscriptions.TryRemove(sessionId, out var subscriptions))
            {
                return subscriptions.Count;
            }
            
            return 0;
        }
    }

    /// <summary>
    /// Get total number of active subscriptions across all sessions.
    /// </summary>
    public int TotalSubscriptions
    {
        get
        {
            lock (_lock)
            {
                return _subscriptions.Values.Sum(s => s.Count);
            }
        }
    }

    /// <summary>
    /// Get total number of sessions with active subscriptions.
    /// </summary>
    public int SessionCount
    {
        get
        {
            lock (_lock)
            {
                return _subscriptions.Count;
            }
        }
    }
}
