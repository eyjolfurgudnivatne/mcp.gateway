# 📦 Resource Subscriptions (v1.8.0)

**Version:** v1.8.0  
**MCP Protocol:** 2025-11-25 (Optional Feature)  
**Status:** Production Ready

## 📋 Overview

Resource subscriptions allow clients to subscribe to specific resource URIs and receive notifications ONLY when those resources change. This reduces bandwidth and improves performance for high-frequency resource updates.

**Key benefits:**
- ✅ **Targeted notifications** - Only receive updates for subscribed resources
- ✅ **Reduced bandwidth** - No unnecessary notifications
- ✅ **Session-based** - Automatic cleanup on session expiry
- ✅ **Thread-safe** - Concurrent subscriptions handled correctly

---

## 🎯 When to Use Subscriptions

### ✅ Good Use Cases

1. **High-frequency updates**
   - Live metrics (`system://metrics`)
   - Real-time data feeds
   - Log file monitoring (`file://logs/app.log`)

2. **Many resources, few subscribers**
   - 100+ resources, client only cares about 5
   - Different clients interested in different resources
   - Reduces broadcast overhead

3. **Bandwidth-sensitive scenarios**
   - Mobile clients
   - Low-bandwidth connections
   - Large resource payloads

### ❌ When NOT to Use Subscriptions

1. **Low-frequency updates**
   - Static configuration files
   - Rarely changing data
   - Subscriptions add unnecessary complexity

2. **All clients need all updates**
   - Global configuration changes
   - System-wide announcements
   - Just broadcast to all sessions

3. **Simple request-response patterns**
   - One-time resource reads
   - No change notifications needed
   - Use `resources/read` instead

---

## 🚀 Quick Start

### 1. Subscribe to a Resource

```bash
# POST /mcp to subscribe (creates session automatically)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/subscribe",
    "params": {
      "uri": "file://data/users.json"
    },
    "id": 1
  }'

# Response:
{
  "jsonrpc": "2.0",
  "result": {
    "subscribed": true,
    "uri": "file://data/users.json"
  },
  "id": 1
}
# Note: MCP-Session-Id header returned for future requests
```

### 2. Open SSE Stream

```bash
# GET /mcp to receive notifications
curl -N http://localhost:5000/mcp \
  -H "Accept: text/event-stream" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: <your-session-id>"

# You'll receive notifications when subscribed resources change:
# id: 1
# event: message
# data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"uri":"file://data/users.json"}}
```

### 3. Unsubscribe

```bash
# POST /mcp to unsubscribe
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: <your-session-id>" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/unsubscribe",
    "params": {
      "uri": "file://data/users.json"
    },
    "id": 2
  }'
```

---

## 📡 API Reference

### resources/subscribe

Subscribe a session to resource update notifications for a specific URI.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/subscribe",
  "params": {
    "uri": "file://data/users.json"  // Exact URI match only (v1.8.0)
  },
  "id": 1
}
```

**Response (Success):**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "subscribed": true,
    "uri": "file://data/users.json"
  },
  "id": 1
}
```

**Response (Error - Resource Not Found):**
```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32601,
    "message": "Resource not found",
    "data": {
      "detail": "Resource 'file://nonexistent.txt' is not configured"
    }
  },
  "id": 1
}
```

**Response (Error - Session Required):**
```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32000,
    "message": "Session required",
    "data": {
      "detail": "Resource subscriptions require an active session. Use POST /mcp to initialize."
    }
  },
  "id": 1
}
```

---

### resources/unsubscribe

Unsubscribe a session from resource update notifications.

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "resources/unsubscribe",
  "params": {
    "uri": "file://data/users.json"
  },
  "id": 2
}
```

**Response (Success):**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "unsubscribed": true,
    "uri": "file://data/users.json"
  },
  "id": 2
}
```

**Note:** Unsubscribing from a non-subscribed URI is not an error (idempotent).

---

### notifications/resources/updated

Server sends notification when a subscribed resource changes.

**Notification (with URI):**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {
    "uri": "file://data/users.json"
  }
}
```

**Notification (without URI - all resources):**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/resources/updated",
  "params": {}
}
```

**Filtering behavior:**
- With `uri` parameter → Only sessions subscribed to that specific URI receive notification
- Without `uri` parameter → All sessions with active SSE streams receive notification (broadcast)

---

## 🏗️ Implementation Guide

### Server-Side: Sending Notifications

Inject `INotificationSender` and call `SendNotificationAsync`:

```csharp
using Mcp.Gateway.Tools.Notifications;

public class MyResources
{
    private readonly INotificationSender _notificationSender;
    
    public MyResources(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    [McpTool("update_user_data")]
    public async Task<JsonRpcMessage> UpdateUserData(JsonRpcMessage request)
    {
        // Update the resource
        await File.WriteAllTextAsync("data/users.json", updatedContent);
        
        // Notify subscribed sessions
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("file://data/users.json"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}
```

**Automatic filtering:**
- NotificationService checks `ResourceSubscriptionRegistry`
- Only sessions subscribed to `"file://data/users.json"` receive the notification
- No need to manually filter - framework handles it!

---

### Server-Side: Resource Definition

Define resources using `[McpResource]` attribute:

```csharp
[McpResource("file://data/users.json",
    Name = "User Data",
    Description = "User records in JSON format",
    MimeType = "application/json")]
public JsonRpcMessage GetUserData(JsonRpcMessage request)
{
    var data = File.ReadAllText("data/users.json");
    return ToolResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "file://data/users.json",
            MimeType: "application/json",
            Text: data
        ));
}
```

**Requirements for subscriptions:**
- Resource MUST be registered via `[McpResource]`
- Subscription validates that resource exists
- Invalid URIs return error `-32601` (Resource not found)

---

### Client-Side: JavaScript Example

```javascript
// 1. Subscribe to resource
const session = { id: null };

async function subscribe(uri) {
  const response = await fetch('http://localhost:5000/mcp', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'MCP-Protocol-Version': '2025-11-25',
      ...(session.id && { 'MCP-Session-Id': session.id })
    },
    body: JSON.stringify({
      jsonrpc: '2.0',
      method: 'resources/subscribe',
      params: { uri },
      id: Date.now()
    })
  });
  
  // Save session ID from first request
  if (!session.id) {
    session.id = response.headers.get('MCP-Session-Id');
  }
  
  return await response.json();
}

// Subscribe to multiple resources
await subscribe('file://data/users.json');
await subscribe('system://metrics');

// 2. Open SSE stream for notifications
const eventSource = new EventSource(
  `http://localhost:5000/mcp`,
  {
    headers: {
      'MCP-Protocol-Version': '2025-11-25',
      'MCP-Session-Id': session.id
    }
  }
);

// 3. Listen for notifications
eventSource.addEventListener('message', (event) => {
  const notification = JSON.parse(event.data);
  
  if (notification.method === 'notifications/resources/updated') {
    const uri = notification.params.uri;
    console.log('Resource updated:', uri);
    
    // Re-fetch the updated resource
    fetchResource(uri);
  }
});

// 4. Cleanup
eventSource.close();
await unsubscribe('file://data/users.json');
```

---

## 🔍 Technical Details

### Subscription Storage

Subscriptions are stored in-memory per session:

```
SessionId: "abc123"
├─ Subscriptions:
   ├─ "file://data/users.json"
   ├─ "file://data/products.json"
   └─ "system://metrics"
```

**Thread-safety:**
- `ConcurrentDictionary<string, HashSet<string>>`
- Lock-based synchronization for subscribe/unsubscribe
- Atomic operations (Add/Remove/Contains)

**Memory footprint:**
- Per subscription: ~100 bytes
- 100 subscriptions: ~10 KB
- Negligible memory impact

---

### Notification Filtering

When `NotificationMessage.ResourcesUpdated(uri)` is sent:

1. Extract `uri` from notification params
2. Query `ResourceSubscriptionRegistry.GetSubscribedSessions(uri)`
3. Send notification ONLY to those sessions
4. Skip non-subscribed sessions (logged at Debug level)

**Performance:**
- O(n) where n = active sessions
- Exact string matching (fast!)
- No regex or wildcard parsing overhead

---

### Automatic Cleanup

Subscriptions are automatically cleared when:

1. **Session deleted via DELETE /mcp:**
   ```csharp
   subscriptionRegistry.ClearSession(sessionId);
   sessionService.DeleteSession(sessionId);
   ```

2. **Session expires (timeout):**
   ```csharp
   // SessionService.ValidateSession() detects timeout
   // Invokes onSessionDeleted callback
   // → subscriptionRegistry.ClearSession(sessionId)
   ```

3. **Session cleanup task:**
   ```csharp
   sessionService.CleanupExpiredSessions();
   // → onSessionDeleted callback invoked for each expired session
   ```

**No manual cleanup required!**

---

## 📊 Comparison: Broadcast vs Subscriptions

### Scenario: 100 resources, 10 sessions, 1 resource updated

| Approach | Notifications Sent | Bandwidth | CPU |
|----------|-------------------|-----------|-----|
| **Broadcast (v1.7.0)** | 10 (all sessions) | 10x | Medium |
| **Subscriptions (v1.8.0)** | 2 (subscribed only) | 2x | Low |

**Savings:** 80% fewer notifications, 80% less bandwidth!

---

## 🎛️ Configuration

### DI Registration

Automatic via `AddToolsService()`:

```csharp
builder.AddToolsService();
// Registers:
// - ResourceSubscriptionRegistry (singleton)
// - SessionService (with cleanup callback)
// - NotificationService (with subscription filtering)
```

### Session Timeout

```csharp
builder.Services.AddSingleton<SessionService>(sp =>
{
    return new SessionService(
        sessionTimeout: TimeSpan.FromMinutes(60));  // Custom timeout
});
```

---

## 🧪 Testing

### Unit Tests

```csharp
[Fact]
public void Subscribe_WithValidUri_AddsSubscription()
{
    var registry = new ResourceSubscriptionRegistry();
    
    var added = registry.Subscribe("session123", "file://data/users.json");
    
    Assert.True(added);
    Assert.True(registry.IsSubscribed("session123", "file://data/users.json"));
}

[Fact]
public void Unsubscribe_RemovesSubscription()
{
    var registry = new ResourceSubscriptionRegistry();
    registry.Subscribe("session123", "file://data/users.json");
    
    var removed = registry.Unsubscribe("session123", "file://data/users.json");
    
    Assert.True(removed);
    Assert.False(registry.IsSubscribed("session123", "file://data/users.json"));
}
```

### Integration Tests

```csharp
[Fact]
public async Task ResourcesSubscribe_WithValidUri_ReturnsSuccess()
{
    var request = new
    {
        jsonrpc = "2.0",
        method = "resources/subscribe",
        @params = new { uri = "file://data/users.json" },
        id = 1
    };
    
    var response = await PostAsync("/mcp", request);
    
    Assert.True(response.Result.GetProperty("subscribed").GetBoolean());
    Assert.Equal("file://data/users.json", 
                 response.Result.GetProperty("uri").GetString());
}
```

---

## 🚨 Error Handling

### Common Errors

| Error Code | Meaning | Solution |
|------------|---------|----------|
| `-32000` | Session required | Use `/mcp` endpoint with session management |
| `-32601` | Resource not found | Check resource URI, ensure resource is registered |
| `-32602` | Invalid params | Missing `uri` parameter |

### Error Response Example

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": -32601,
    "message": "Resource not found",
    "data": {
      "detail": "Resource 'file://unknown.txt' is not configured"
    }
  },
  "id": 1
}
```

---

## 🔮 Future Enhancements (v1.9.0+)

### Wildcard Subscriptions (Planned)

```json
{
  "method": "resources/subscribe",
  "params": {
    "uri": "file://logs/*.log"  // Subscribe to all log files
  }
}
```

**Patterns planned:**
- `file://logs/*.log` - Wildcard matching
- `file://logs/**` - Recursive directory matching
- `file://logs/app-[0-9]+.log` - Regex patterns

**Status:** Deferred to v1.9.0 (keeps v1.8.0 simple and spec-compliant)

---

## 📚 Related Documentation

- **MCP Protocol:** [docs/MCP-Protocol.md](MCP-Protocol.md)
- **Session Management:** [Examples/NotificationMcpServer/SESSION_MANAGEMENT.md](../Examples/NotificationMcpServer/SESSION_MANAGEMENT.md)
- **Notifications:** [README.md](../README.md#-notifications-v170---mcp-2025-11-25-compliant)
- **Resource Example:** [Examples/ResourceMcpServer/README.md](../Examples/ResourceMcpServer/README.md)

---

## 💡 Best Practices

### 1. Use Exact URI Matching

```csharp
// ✅ GOOD: Exact URI
await Subscribe("file://data/users.json");

// ❌ BAD: Wildcard (not supported in v1.8.0)
await Subscribe("file://data/*.json");
```

### 2. Subscribe Before Opening SSE Stream

```csharp
// ✅ GOOD: Subscribe first, then open stream
await Subscribe("system://metrics");
OpenSseStream(sessionId);

// ❌ BAD: Open stream first (may miss notifications)
OpenSseStream(sessionId);
await Subscribe("system://metrics");  // Too late!
```

### 3. Unsubscribe on Cleanup

```javascript
// ✅ GOOD: Explicit cleanup
window.addEventListener('beforeunload', async () => {
  await unsubscribe('file://data/users.json');
  eventSource.close();
});

// ⚠️ OK: Automatic cleanup on session expiry
// (But explicit is better for immediate cleanup)
```

### 4. Handle Reconnection

```javascript
eventSource.addEventListener('error', async (error) => {
  console.error('SSE connection lost, reconnecting...');
  
  // Re-open SSE stream (Last-Event-ID header handles replay)
  const newEventSource = new EventSource(...);
  
  // Re-subscribe (idempotent - safe to call again)
  await subscribe('file://data/users.json');
});
```

---

## 🎖️ Version History

- **v1.8.0** - Initial release
  - Exact URI matching only
  - Session-based subscriptions
  - Automatic cleanup
  - Thread-safe implementation

- **v1.9.0** (Planned)
  - Wildcard subscriptions
  - Regex pattern matching
  - Enhanced filtering

---

**Built with ❤️ using .NET 10 and C# 14.0**

**License:** MIT  
**Repository:** https://github.com/eyjolfurgudnivatne/mcp.gateway
