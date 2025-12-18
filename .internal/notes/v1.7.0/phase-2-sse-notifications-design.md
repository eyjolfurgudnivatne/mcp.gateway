# 🚀 Phase 2: SSE-based Notifications - Design Document

**Created:** 18. desember 2025, kl. 23:05  
**Branch:** feat/v1.7.0-to-2025-11-25  
**Status:** Design & Planning  
**Target:** v1.7.0 (full MCP 2025-11-25 compliance)

---

## 📋 Executive Summary

**Mål:** Implementere SSE-baserte notifications for MCP 2025-11-25 compliance.

**Hovedendring:** Fra WebSocket-only til **SSE-first** notifications (med WebSocket backward compat).

**Key features:**
1. ✅ Message buffering per session (FIFO queue)
2. ✅ Notification broadcast to SSE streams
3. ✅ `Last-Event-ID` message replay
4. ✅ Subscription channels (tools, prompts, resources)
5. ✅ Backward compatibility (WebSocket still works)

---

## 🏗️ Current Architecture (v1.7.0 Phase 1)

### Notification Infrastructure Today:

```csharp
// NotificationService.cs (WebSocket-only):
public class NotificationService : INotificationSender
{
    private readonly ConcurrentBag<WebSocket> _subscribers = new();
    
    public async Task SendNotificationAsync(NotificationMessage notification)
    {
        // Broadcasts to WebSocket subscribers only!
        foreach (var subscriber in _subscribers.Where(ws => ws.State == WebSocketState.Open))
        {
            await subscriber.SendAsync(...);
        }
    }
}
```

### Problem med dagens løsning:

| Issue | Impact | MCP 2025-11-25 |
|-------|--------|----------------|
| **WebSocket-only** | HTTP clients can't receive notifications | MUST support SSE |
| **No message buffering** | Lost messages on disconnect | SHOULD buffer for replay |
| **No event IDs** | Can't resume after disconnect | MUST support `Last-Event-ID` |
| **No SSE support** | Not MCP 2025-11-25 compliant | MUST send via SSE |

---

## 🎯 Target Architecture (v1.7.0 Phase 2)

### New notification pattern:

```csharp
// v1.7.0 Phase 2 (NEW):
public class NotificationService : INotificationSender
{
    // SSE subscribers per session
    private readonly ConcurrentDictionary<string, List<SseStream>> _sseSubscribers = new();
    
    // Message buffer per session (for Last-Event-ID replay)
    private readonly ConcurrentDictionary<string, MessageBuffer> _messageBuffers = new();
    
    // Legacy WebSocket support (deprecated)
    private readonly ConcurrentBag<WebSocket> _wsSubscribers = new();
    
    public async Task SendNotificationAsync(NotificationMessage notification)
    {
        // 1. Generate event ID
        var eventId = _eventIdGenerator.GenerateEventId(sessionId);
        
        // 2. Buffer message for replay
        _messageBuffers[sessionId].Add(eventId, notification);
        
        // 3. Broadcast to SSE streams
        await BroadcastToSseAsync(sessionId, eventId, notification);
        
        // 4. Broadcast to WebSocket (deprecated, still works)
        await BroadcastToWebSocketAsync(notification);
    }
}
```

---

## 📊 Wire Format

### SSE Notification Example:

**Client opens SSE stream:**
```http
GET /mcp HTTP/1.1
Accept: text/event-stream
MCP-Protocol-Version: 2025-11-25
MCP-Session-Id: abc123
Last-Event-ID: 42  # Optional: resume from event 42
```

**Server sends notifications:**
```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
MCP-Session-Id: abc123

id: 43
event: message
data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}

id: 44
event: message
data: {"jsonrpc":"2.0","method":"notifications/prompts/list_changed","params":{}}

id: 45
event: message
data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"uri":"file:///test.txt"}}

: keep-alive

```

---

## 🔧 Implementation Plan

### Task 2.1: Message Buffer Infrastructure

**Goal:** Store recent messages per session for replay.

**Components:**
```csharp
// New class: MessageBuffer.cs
public sealed class MessageBuffer
{
    private readonly int _maxSize;
    private readonly Queue<BufferedMessage> _messages = new();
    private readonly object _lock = new();
    
    public MessageBuffer(int maxSize = 100)
    {
        _maxSize = maxSize;
    }
    
    public void Add(string eventId, object message)
    {
        lock (_lock)
        {
            _messages.Enqueue(new BufferedMessage(eventId, message, DateTime.UtcNow));
            
            // Remove oldest if over limit
            while (_messages.Count > _maxSize)
            {
                _messages.Dequeue();
            }
        }
    }
    
    public IEnumerable<BufferedMessage> GetMessagesAfter(string? lastEventId)
    {
        lock (_lock)
        {
            if (string.IsNullOrEmpty(lastEventId))
                return _messages.ToList();
            
            // Find position of lastEventId
            var skipCount = 0;
            foreach (var msg in _messages)
            {
                if (msg.EventId == lastEventId)
                    break;
                skipCount++;
            }
            
            // Return messages after lastEventId
            return _messages.Skip(skipCount + 1).ToList();
        }
    }
}

public sealed record BufferedMessage(
    string EventId,
    object Message,
    DateTime Timestamp
);
```

**DI Registration:**
```csharp
// SessionInfo.cs - Add message buffer
public sealed class SessionInfo
{
    public required string Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivity { get; set; }
    public long EventIdCounter;
    public MessageBuffer MessageBuffer { get; } = new(100);  // NEW
}
```

---

### Task 2.2: SSE Subscription Management

**Goal:** Track active SSE streams and broadcast to them.

**Components:**
```csharp
// New class: SseStreamRegistry.cs
public sealed class SseStreamRegistry
{
    private readonly ConcurrentDictionary<string, List<ActiveSseStream>> _streams = new();
    private readonly object _lock = new();
    
    public void Register(string sessionId, HttpResponse response, CancellationToken ct)
    {
        lock (_lock)
        {
            if (!_streams.ContainsKey(sessionId))
            {
                _streams[sessionId] = new List<ActiveSseStream>();
            }
            
            _streams[sessionId].Add(new ActiveSseStream(response, ct));
        }
    }
    
    public void Unregister(string sessionId, HttpResponse response)
    {
        lock (_lock)
        {
            if (_streams.TryGetValue(sessionId, out var streams))
            {
                streams.RemoveAll(s => s.Response == response);
                
                if (streams.Count == 0)
                {
                    _streams.TryRemove(sessionId, out _);
                }
            }
        }
    }
    
    public async Task BroadcastAsync(string sessionId, SseEventMessage message)
    {
        List<ActiveSseStream> streams;
        lock (_lock)
        {
            if (!_streams.TryGetValue(sessionId, out var s))
                return;
            
            streams = s.ToList(); // Copy to avoid lock during I/O
        }
        
        // Broadcast to all active streams
        foreach (var stream in streams)
        {
            if (stream.CancellationToken.IsCancellationRequested)
                continue;
            
            try
            {
                await WriteSseEventAsync(stream.Response, message, stream.CancellationToken);
            }
            catch (Exception)
            {
                // Remove dead stream
                Unregister(sessionId, stream.Response);
            }
        }
    }
    
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
        
        // Write event type
        if (!string.IsNullOrEmpty(message.Event))
        {
            await response.WriteAsync($"event: {message.Event}\n", ct);
        }
        
        // Write data
        var json = JsonSerializer.Serialize(message.Data, JsonOptions.Default);
        await response.WriteAsync($"data: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}

public sealed record ActiveSseStream(
    HttpResponse Response,
    CancellationToken CancellationToken
);
```

---

### Task 2.3: Update NotificationService

**Goal:** Add SSE support while keeping WebSocket backward compat.

**Components:**
```csharp
// Updated NotificationService.cs
public sealed class NotificationService : INotificationSender
{
    private readonly EventIdGenerator _eventIdGenerator;
    private readonly SessionService _sessionService;
    private readonly SseStreamRegistry _sseRegistry;
    private readonly ILogger<NotificationService> _logger;
    
    // Legacy WebSocket support (deprecated)
    private readonly ConcurrentBag<WebSocket> _wsSubscribers = new();
    
    public NotificationService(
        EventIdGenerator eventIdGenerator,
        SessionService sessionService,
        SseStreamRegistry sseRegistry,
        ILogger<NotificationService> logger)
    {
        _eventIdGenerator = eventIdGenerator;
        _sessionService = sessionService;
        _sseRegistry = sseRegistry;
        _logger = logger;
    }
    
    public async Task SendNotificationAsync(string? sessionId, NotificationMessage notification)
    {
        // 1. SSE broadcast (MCP 2025-11-25 compliant)
        if (!string.IsNullOrEmpty(sessionId))
        {
            var session = _sessionService.GetSession(sessionId);
            if (session != null)
            {
                // Generate event ID
                var eventId = _eventIdGenerator.GenerateEventId(sessionId);
                
                // Buffer message for replay
                session.MessageBuffer.Add(eventId, notification);
                
                // Broadcast to SSE streams
                var sseMessage = SseEventMessage.CreateMessage(eventId, notification);
                await _sseRegistry.BroadcastAsync(sessionId, sseMessage);
                
                _logger.LogDebug(
                    "Notification sent via SSE to session {SessionId}: {Method}",
                    sessionId,
                    notification.Method);
            }
        }
        
        // 2. WebSocket broadcast (legacy, deprecated)
        await BroadcastToWebSocketAsync(notification);
    }
    
    private async Task BroadcastToWebSocketAsync(NotificationMessage notification)
    {
        // Keep existing WebSocket logic for backward compat
        foreach (var ws in _wsSubscribers.Where(w => w.State == WebSocketState.Open))
        {
            try
            {
                var json = JsonSerializer.Serialize(notification, JsonOptions.Default);
                var bytes = Encoding.UTF8.GetBytes(json);
                await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send WebSocket notification (deprecated)");
            }
        }
    }
    
    // Legacy WebSocket subscribe (deprecated)
    public void Subscribe(WebSocket webSocket)
    {
        _wsSubscribers.Add(webSocket);
        _logger.LogWarning("WebSocket notifications are deprecated, use SSE instead");
    }
}
```

---

### Task 2.4: Update StreamableHttpEndpoint (GET handler)

**Goal:** Implement `Last-Event-ID` replay and notification streaming.

**Components:**
```csharp
// Update StreamableHttpEndpoint.cs - GET handler
app.MapGet(pattern, async (
    HttpContext context,
    SessionService sessionService,
    EventIdGenerator eventIdGenerator,
    SseStreamRegistry sseRegistry,
    ILogger<ToolInvoker> logger,
    CancellationToken ct) =>
{
    // 1. Validate session
    var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();
    if (string.IsNullOrEmpty(sessionId) || !sessionService.ValidateSession(sessionId))
    {
        // Return 400/404 error
        return;
    }
    
    // 2. Set SSE headers
    context.Response.ContentType = "text/event-stream; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.Headers["MCP-Session-Id"] = sessionId;
    await context.Response.StartAsync(ct);
    
    // 3. Get Last-Event-ID for resumption
    var lastEventId = context.Request.Headers["Last-Event-ID"].ToString();
    
    // 4. Replay buffered messages (if Last-Event-ID provided)
    var session = sessionService.GetSession(sessionId);
    if (session != null)
    {
        var bufferedMessages = session.MessageBuffer.GetMessagesAfter(lastEventId);
        foreach (var buffered in bufferedMessages)
        {
            var sseMessage = SseEventMessage.CreateMessage(buffered.EventId, buffered.Message);
            await WriteSseEventAsync(context.Response, sseMessage, ct);
            
            logger.LogDebug(
                "Replayed buffered message {EventId} to session {SessionId}",
                buffered.EventId,
                sessionId);
        }
    }
    
    // 5. Register SSE stream for future notifications
    sseRegistry.Register(sessionId, context.Response, ct);
    
    try
    {
        // 6. Keep connection alive with periodic pings
        while (!ct.IsCancellationRequested)
        {
            await context.Response.WriteAsync(": keep-alive\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
            await Task.Delay(30_000, ct);
        }
    }
    finally
    {
        // 7. Unregister on disconnect
        sseRegistry.Unregister(sessionId, context.Response);
        logger.LogInformation("SSE stream closed for session {SessionId}", sessionId);
    }
});
```

---

## 🧪 Testing Strategy

### Test 1: Message buffering

```csharp
[Fact]
public void MessageBuffer_AddsMessages_MaintainsMaxSize()
{
    // Arrange
    var buffer = new MessageBuffer(maxSize: 10);
    
    // Act - Add 15 messages
    for (int i = 0; i < 15; i++)
    {
        buffer.Add($"event-{i}", new { message = $"test-{i}" });
    }
    
    // Assert - Only last 10 should remain
    var messages = buffer.GetMessagesAfter(null).ToList();
    Assert.Equal(10, messages.Count);
    Assert.Equal("event-5", messages[0].EventId); // First is event-5
    Assert.Equal("event-14", messages[9].EventId); // Last is event-14
}

[Fact]
public void MessageBuffer_GetMessagesAfter_ReturnsCorrectSubset()
{
    // Arrange
    var buffer = new MessageBuffer();
    buffer.Add("event-1", new { message = "one" });
    buffer.Add("event-2", new { message = "two" });
    buffer.Add("event-3", new { message = "three" });
    
    // Act
    var messages = buffer.GetMessagesAfter("event-1").ToList();
    
    // Assert
    Assert.Equal(2, messages.Count);
    Assert.Equal("event-2", messages[0].EventId);
    Assert.Equal("event-3", messages[1].EventId);
}
```

### Test 2: SSE notification broadcast

```csharp
[Fact]
public async Task NotificationService_SendsToSseStreams()
{
    // Arrange
    var sessionId = await CreateSessionAsync();
    var notification = new NotificationMessage
    {
        Method = "notifications/tools/list_changed",
        Params = new { }
    };
    
    // Open SSE stream
    var sseTask = OpenSseStreamAsync(sessionId);
    await Task.Delay(100); // Wait for stream to be ready
    
    // Act - Send notification
    await _notificationService.SendNotificationAsync(sessionId, notification);
    
    // Assert - SSE stream should receive notification
    var receivedEvents = await ReadSseEventsAsync(sseTask, count: 1, timeout: 1000);
    Assert.Single(receivedEvents);
    Assert.Contains("notifications/tools/list_changed", receivedEvents[0]);
}
```

### Test 3: `Last-Event-ID` replay

```csharp
[Fact]
public async Task SseStream_ReplaysMessagesAfterLastEventId()
{
    // Arrange
    var sessionId = await CreateSessionAsync();
    
    // Send 3 notifications
    await _notificationService.SendNotificationAsync(sessionId, 
        new NotificationMessage { Method = "notifications/tools/list_changed" });
    await _notificationService.SendNotificationAsync(sessionId,
        new NotificationMessage { Method = "notifications/prompts/list_changed" });
    await _notificationService.SendNotificationAsync(sessionId,
        new NotificationMessage { Method = "notifications/resources/updated" });
    
    // Get event IDs
    var session = _sessionService.GetSession(sessionId);
    var firstEventId = session.MessageBuffer.GetMessagesAfter(null).First().EventId;
    
    // Act - Reconnect with Last-Event-ID
    var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
    request.Headers.Add("MCP-Session-Id", sessionId);
    request.Headers.Add("Last-Event-ID", firstEventId);
    
    var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    
    // Assert - Should receive last 2 messages
    var receivedEvents = await ReadSseEventsAsync(response, count: 2, timeout: 1000);
    Assert.Equal(2, receivedEvents.Count);
}
```

### Test 4: WebSocket backward compat

```csharp
[Fact]
public async Task NotificationService_StillSupportsWebSocket()
{
    // Arrange
    var ws = await CreateWebSocketAsync("/ws");
    _notificationService.Subscribe(ws);
    
    // Act - Send notification
    await _notificationService.SendNotificationAsync(null,
        new NotificationMessage { Method = "notifications/tools/list_changed" });
    
    // Assert - WebSocket should receive notification
    var received = await ReceiveWebSocketMessageAsync(ws, timeout: 1000);
    Assert.NotNull(received);
    Assert.Contains("notifications/tools/list_changed", received);
}
```

---

## 📊 Timeline & Milestones

| Task | Deliverable | Est. Time | Status |
|------|-------------|-----------|--------|
| **Task 2.1** | Message buffering | 30-45 min | 📝 Planned |
| **Task 2.2** | SSE subscription registry | 30-45 min | 📝 Planned |
| **Task 2.3** | Update NotificationService | 30-45 min | 📝 Planned |
| **Task 2.4** | Update StreamableHttpEndpoint | 30 min | 📝 Planned |
| **Task 2.5** | Testing | 30-45 min | 📝 Planned |
| **Task 2.6** | Documentation | 15 min | 📝 Planned |

**Total estimated:** ~2.5-3 timer

---

## 🚨 Breaking Changes

### None! 🎉
- ✅ WebSocket notifications still work (deprecated but functional)
- ✅ Existing notification code unchanged
- ✅ SSE is additive feature
- ✅ Backward compatible

---

## 🎯 Success Criteria

- ✅ Notifications sent via SSE to active streams
- ✅ Message buffering per session (max 100 messages)
- ✅ `Last-Event-ID` replay works
- ✅ WebSocket still works (deprecated)
- ✅ All existing tests pass
- ✅ New tests for SSE notifications pass
- ✅ Zero regression

---

## 📚 References

### MCP Specification 2025-11-25
- **Transports:** https://modelcontextprotocol.io/specification/2025-11-25/basic/transports
- **Server-sent Events:** https://modelcontextprotocol.io/specification/2025-11-25/basic/transports#server-sent-events-sse

### Implementation notes:
- `.internal/notes/v1.7.0/phase-1-streamable-http-design.md` - Phase 1 design
- `Mcp.Gateway.Tools/NotificationService.cs` - Current implementation
- `Mcp.Gateway.Tools/StreamableHttpEndpoint.cs` - GET handler

---

**Status:** 📝 Design Complete  
**Next Step:** Start implementation (Task 2.1: Message Buffer)  
**Created:** 18. desember 2025, kl. 23:05  
**Author:** ARKo AS - AHelse Development Team
