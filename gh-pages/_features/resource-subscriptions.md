---
layout: mcp-default
title: Resource Subscriptions
description: Subscribe to specific resources for targeted notifications
breadcrumbs:
  - title: Home
    url: /
  - title: Resource Subscriptions
    url: /features/resource-subscriptions/
toc: true
---

# Resource Subscriptions

**Version:** v1.8.0  
**MCP Protocol:** 2025-11-25 (Optional Feature)  
**Status:** Production Ready

## Overview

Resource subscriptions allow clients to subscribe to specific resource URIs and receive notifications ONLY when those resources change. This reduces bandwidth and improves performance for high-frequency resource updates.

**Key benefits:**
- ✅ **Targeted notifications** - Only receive updates for subscribed resources
- ✅ **Reduced bandwidth** - No unnecessary notifications
- ✅ **Session-based** - Automatic cleanup on session expiry
- ✅ **Thread-safe** - Concurrent subscriptions handled correctly

## When to Use Subscriptions

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

1. **Low-frequency updates** - Static configuration files
2. **All clients need all updates** - Just broadcast to all sessions
3. **Simple request-response patterns** - Use `resources/read` instead

## Quick Start

### 1. Subscribe to a Resource

```bash
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
```

**Response:**
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

### 2. Open SSE Stream

```bash
curl -N http://localhost:5000/mcp \
  -H "Accept: text/event-stream" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: <your-session-id>"
```

You'll receive notifications when subscribed resources change:
```
id: 1
event: message
data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"uri":"file://data/users.json"}}
```

### 3. Unsubscribe

```bash
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

## Server-Side Implementation

### Sending Notifications

Inject `INotificationSender` to send notifications to subscribed sessions:

```csharp
using Mcp.Gateway.Tools.Notifications;

// Option 1: Constructor injection (class must be registered in DI)
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
        
        // Notify subscribed sessions (automatic filtering!)
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("file://data/users.json"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}

// Register in DI:
builder.Services.AddScoped<MyResources>();

// Option 2: Method parameter injection (no registration needed)
public class MyResources
{
    [McpTool("update_user_data")]
    public async Task<JsonRpcMessage> UpdateUserData(
        JsonRpcMessage request,
        INotificationSender notificationSender)  // ← Automatically injected!
    {
        // Update the resource
        await File.WriteAllTextAsync("data/users.json", updatedContent);
        
        // Notify subscribed sessions (automatic filtering!)
        await notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("file://data/users.json"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}
```

**Parameter resolution order:**
1. `JsonRpcMessage` - The request (always first parameter)
2. Additional parameters - Resolved from DI container

### Defining Resources

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

## Client-Side Example (JavaScript)

```javascript
// 1. Subscribe to resources
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
  
  if (!session.id) {
    session.id = response.headers.get('MCP-Session-Id');
  }
  
  return await response.json();
}

await subscribe('file://data/users.json');
await subscribe('system://metrics');

// 2. Open SSE stream
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
    fetchResource(uri);
  }
});

// 4. Cleanup
eventSource.close();
await unsubscribe('file://data/users.json');
```

## Performance Comparison

### Scenario: 100 resources, 10 sessions, 1 resource updated

| Approach | Notifications Sent | Bandwidth | CPU |
|----------|-------------------|-----------|-----|
| **Broadcast (v1.7.0)** | 10 (all sessions) | 10x | Medium |
| **Subscriptions (v1.8.0)** | 2 (subscribed only) | 2x | Low |

**Savings:** 80% fewer notifications, 80% less bandwidth!

## Automatic Cleanup

Subscriptions are automatically cleared when:

1. **Session deleted via DELETE /mcp**
2. **Session expires (timeout)**
3. **Session cleanup task runs**

**No manual cleanup required!**

## Error Handling

### Common Errors

| Error Code | Meaning | Solution |
|------------|---------|----------|
| `-32000` | Session required | Use `/mcp` endpoint with session management |
| `-32601` | Resource not found | Check resource URI, ensure resource is registered |
| `-32602` | Invalid params | Missing `uri` parameter |

## Best Practices

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
await Subscribe("system://metrics");
```

### 3. Handle Reconnection

```javascript
eventSource.addEventListener('error', async (error) => {
  console.error('SSE connection lost, reconnecting...');
  
  // Re-open SSE stream
  const newEventSource = new EventSource(...);
  
  // Re-subscribe (idempotent)
  await subscribe('file://data/users.json');
});
```

## Future Enhancements (v1.9.0+)

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

## See Also

- [Notifications](/features/notifications/) - Real-time server notifications
- [Resources API](/api/resources/) - Complete Resources API reference
- [Examples: Resource Server](/examples/resource/) - Complete example with subscriptions
