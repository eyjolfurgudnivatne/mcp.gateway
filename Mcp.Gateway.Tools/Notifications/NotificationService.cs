namespace Mcp.Gateway.Tools.Notifications;

using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

/// <summary>
/// Service for managing notification subscribers and sending notifications (v1.6.0+, v1.7.0 Phase 2)
/// Thread-safe implementation for SSE and WebSocket-based notifications.
/// </summary>
public class NotificationService : INotificationSender
{
    private readonly EventIdGenerator _eventIdGenerator;
    private readonly SessionService _sessionService;
    private readonly SseStreamRegistry _sseRegistry;
    private readonly ILogger<NotificationService> _logger;
    private readonly ResourceSubscriptionRegistry? _subscriptionRegistry;  // v1.8.0 Phase 4

    // Legacy WebSocket support
    private readonly ConcurrentBag<WebSocket> _subscribers = [];

    public NotificationService(
        EventIdGenerator eventIdGenerator,
        SessionService sessionService,
        SseStreamRegistry sseRegistry,
        ILogger<NotificationService> logger,
        ResourceSubscriptionRegistry? subscriptionRegistry = null)  // v1.8.0 Phase 4 (optional)
    {
        _eventIdGenerator = eventIdGenerator;
        _sessionService = sessionService;
        _sseRegistry = sseRegistry;
        _logger = logger;
        _subscriptionRegistry = subscriptionRegistry;
    }

    /// <summary>
    /// Adds a WebSocket subscriber for notifications
    /// </summary>
    public void AddSubscriber(WebSocket webSocket)
    {
        if (webSocket.State == WebSocketState.Open)
        {
            _subscribers.Add(webSocket);
        }
    }

    /// <summary>
    /// Removes a WebSocket subscriber
    /// </summary>
    public void RemoveSubscriber(WebSocket webSocket)
    {
        // ConcurrentBag doesn't have Remove, but we filter out closed connections when sending
        _logger.LogDebug("WebSocket subscriber removed");
    }

    /// <summary>
    /// Gets the count of active WebSocket subscribers
    /// </summary>
    public int SubscriberCount => _subscribers.Count(ws => ws.State == WebSocketState.Open);

    /// <summary>
    /// Sends a notification to all subscribed clients (SSE + WebSocket for backward compat)
    /// </summary>
    /// <param name="notification">Notification message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        // 1. SSE broadcast (MCP 2025-11-25 compliant) - broadcast to ALL sessions
        await BroadcastToAllSseSessionsAsync(notification, cancellationToken);

        // 2. WebSocket broadcast (legacy, deprecated but still functional)
        await BroadcastToWebSocketAsync(notification, cancellationToken);
    }

    /// <summary>
    /// Broadcasts notification via SSE to all active sessions
    /// </summary>
    private async Task BroadcastToAllSseSessionsAsync(NotificationMessage notification, CancellationToken cancellationToken)
    {
        var sessions = _sessionService.GetAllSessions();

        // Check if this is a resource notification with URI (v1.8.0 Phase 4)
        string? notificationUri = null;
        if (notification.Method == "notifications/resources/updated" && 
            notification.Params is JsonElement paramsElement &&
            paramsElement.ValueKind == JsonValueKind.Object &&
            paramsElement.TryGetProperty("uri", out var uriElement))
        {
            notificationUri = uriElement.GetString();
        }

        var sentCount = 0;

        foreach (var session in sessions)
        {
            try
            {
                // Filter by subscription (v1.8.0 Phase 4)
                if (notificationUri != null)
                {
                    // This is a resource notification - check subscription
                    var subscriptionRegistry = GetResourceSubscriptionRegistry();
                    
                    if (subscriptionRegistry != null && 
                        !subscriptionRegistry.IsSubscribed(session.Id, notificationUri))
                    {
                        _logger.LogDebug(
                            "Skipping resource notification to session {SessionId} (not subscribed to {Uri})",
                            session.Id,
                            notificationUri);
                        continue; // Skip this session
                    }
                }

                // Generate event ID for this session
                var eventId = _eventIdGenerator.GenerateEventId(session.Id);

                // Buffer message for replay
                session.MessageBuffer.Add(eventId, notification);

                // Broadcast to SSE streams for this session
                var sseMessage = SseEventMessage.CreateMessage(eventId, notification);
                await _sseRegistry.BroadcastAsync(session.Id, sseMessage);

                sentCount++;

                _logger.LogDebug(
                    "Notification sent via SSE to session {SessionId}: {Method}",
                    session.Id,
                    notification.Method);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to send SSE notification to session {SessionId}",
                    session.Id);
            }
        }

        if (sentCount > 0)
        {
            _logger.LogInformation(
                "SSE notification broadcasted: {Method} to {Count} sessions{UriInfo}",
                notification.Method,
                sentCount,
                notificationUri != null ? $" (URI: {notificationUri})" : "");
        }
    }

    /// <summary>
    /// Broadcasts notification via WebSocket
    /// </summary>
    private async Task BroadcastToWebSocketAsync(NotificationMessage notification, CancellationToken cancellationToken)
    {
        var activeSubscribers = _subscribers.Where(ws => ws.State == WebSocketState.Open).ToList();

        if (!activeSubscribers.Any())
            return;

        _logger.LogWarning(
            "Sending notification via WebSocket (deprecated): {Method} to {Count} subscribers",
            notification.Method,
            activeSubscribers.Count);

        var json = JsonSerializer.Serialize(notification, JsonOptions.Default);
        var bytes = Encoding.UTF8.GetBytes(json);
        var buffer = new ArraySegment<byte>(bytes);

        var tasks = new List<Task>();

        foreach (var subscriber in activeSubscribers)
        {
            tasks.Add(SendToWebSocketSubscriberAsync(subscriber, buffer, cancellationToken));
        }

        await Task.WhenAll(tasks);

        // Clean up closed connections
        CleanupClosedConnections();
    }

    private async Task SendToWebSocketSubscriberAsync(
        WebSocket webSocket,
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "Failed to send WebSocket notification (connection closed)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending WebSocket notification");
        }
    }

    private void CleanupClosedConnections()
    {
        // ConcurrentBag doesn't support removal
        // Closed connections are filtered out during SendNotificationAsync
        // This is acceptable since notifications are infrequent
    }

    /// <summary>
    /// Helper method to get ResourceSubscriptionRegistry (v1.8.0 Phase 4)
    /// Returns null if not available (subscriptions not enabled)
    /// </summary>
    private ResourceSubscriptionRegistry? GetResourceSubscriptionRegistry()
    {
        return _subscriptionRegistry;
    }
}
