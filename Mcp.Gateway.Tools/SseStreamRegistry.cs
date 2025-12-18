namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>
/// Registry for managing active SSE streams per session (v1.7.0 Phase 2).
/// Handles registration, broadcasting, and cleanup of SSE connections.
/// </summary>
public sealed class SseStreamRegistry
{
    private readonly ConcurrentDictionary<string, List<ActiveSseStream>> _streams = new();
    private readonly object _lock = new();

    /// <summary>
    /// Registers an active SSE stream for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="response">HTTP response for SSE stream</param>
    /// <param name="ct">Cancellation token for stream lifetime</param>
    public void Register(string sessionId, HttpResponse response, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sessionId))
            throw new ArgumentNullException(nameof(sessionId));

        if (response == null)
            throw new ArgumentNullException(nameof(response));

        lock (_lock)
        {
            if (!_streams.ContainsKey(sessionId))
            {
                _streams[sessionId] = new List<ActiveSseStream>();
            }

            _streams[sessionId].Add(new ActiveSseStream(response, ct));
        }
    }

    /// <summary>
    /// Unregisters an SSE stream from a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="response">HTTP response to remove</param>
    public void Unregister(string sessionId, HttpResponse response)
    {
        if (string.IsNullOrEmpty(sessionId) || response == null)
            return;

        lock (_lock)
        {
            if (_streams.TryGetValue(sessionId, out var streams))
            {
                streams.RemoveAll(s => s.Response == response);

                // Remove session entry if no streams left
                if (streams.Count == 0)
                {
                    _streams.TryRemove(sessionId, out _);
                }
            }
        }
    }

    /// <summary>
    /// Broadcasts an SSE event to all active streams for a session.
    /// Dead streams are automatically removed.
    /// </summary>
    /// <param name="sessionId">Session ID to broadcast to</param>
    /// <param name="message">SSE event message</param>
    public async Task BroadcastAsync(string sessionId, SseEventMessage message)
    {
        if (string.IsNullOrEmpty(sessionId) || message == null)
            return;

        // Get snapshot of streams (avoid lock during I/O)
        List<ActiveSseStream> streams;
        lock (_lock)
        {
            if (!_streams.TryGetValue(sessionId, out var s))
                return;

            streams = s.ToList(); // Copy to avoid lock during async operations
        }

        // Broadcast to all active streams
        var deadStreams = new List<HttpResponse>();
        foreach (var stream in streams)
        {
            if (stream.CancellationToken.IsCancellationRequested)
            {
                deadStreams.Add(stream.Response);
                continue;
            }

            try
            {
                await WriteSseEventAsync(stream.Response, message, stream.CancellationToken);
            }
            catch (Exception)
            {
                // Mark as dead (will be removed after loop)
                deadStreams.Add(stream.Response);
            }
        }

        // Clean up dead streams
        foreach (var deadResponse in deadStreams)
        {
            Unregister(sessionId, deadResponse);
        }
    }

    /// <summary>
    /// Writes an SSE event to an HTTP response stream.
    /// </summary>
    private async Task WriteSseEventAsync(
        HttpResponse response,
        SseEventMessage message,
        CancellationToken ct)
    {
        // Write event ID
        if (!string.IsNullOrEmpty(message.Id))
        {
            await response.WriteAsync($"id: {message.Id}\n", ct);
        }

        // Write event type (optional, defaults to "message")
        if (!string.IsNullOrEmpty(message.Event))
        {
            await response.WriteAsync($"event: {message.Event}\n", ct);
        }

        // Write retry interval (optional)
        if (message.Retry.HasValue)
        {
            await response.WriteAsync($"retry: {message.Retry.Value}\n", ct);
        }

        // Write data
        var json = JsonSerializer.Serialize(message.Data, JsonOptions.Default);
        await response.WriteAsync($"data: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }

    /// <summary>
    /// Gets the count of active streams for a session.
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <returns>Number of active SSE streams</returns>
    public int GetStreamCount(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return 0;

        lock (_lock)
        {
            if (_streams.TryGetValue(sessionId, out var streams))
            {
                return streams.Count;
            }
            return 0;
        }
    }

    /// <summary>
    /// Gets the total count of active sessions with SSE streams.
    /// </summary>
    public int ActiveSessionCount
    {
        get
        {
            lock (_lock)
            {
                return _streams.Count;
            }
        }
    }

    /// <summary>
    /// Cleans up all streams for a session (e.g., on session deletion).
    /// </summary>
    /// <param name="sessionId">Session ID to clean up</param>
    public void CleanupSession(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId))
            return;

        lock (_lock)
        {
            _streams.TryRemove(sessionId, out _);
        }
    }
}

/// <summary>
/// Represents an active SSE stream with its HTTP response and cancellation token.
/// </summary>
public sealed record ActiveSseStream(
    HttpResponse Response,
    CancellationToken CancellationToken
);
