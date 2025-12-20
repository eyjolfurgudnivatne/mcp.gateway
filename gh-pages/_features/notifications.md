---
layout: mcp-default
title: Notifications
description: Real-time server-to-client notifications using SSE
breadcrumbs:
  - title: Home
    url: /
  - title: Features
    url: /features/
  - title: Notifications
    url: /features/notifications/
toc: true
---

# Notifications

Send real-time notifications from server to clients using Server-Sent Events (SSE).

## Overview

**Added in:** v1.7.0  
**Protocol:** MCP 2025-11-25  
**Transport:** SSE (Server-Sent Events)

Notifications allow servers to push updates to clients in real-time:
- ✅ **Tool invocation progress** - Long-running operations
- ✅ **Resource updates** - File changes, database updates
- ✅ **Custom events** - Application-specific notifications
- ✅ **Session management** - Connection state

## Quick Start

### 1. Server-Side: Send Notification

```csharp
using Mcp.Gateway.Tools.Notifications;

public class MyTools
{
    private readonly INotificationSender _notificationSender;
    
    public MyTools(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    [McpTool("long_operation")]
    public async Task<JsonRpcMessage> LongOperation(JsonRpcMessage request)
    {
        // Send progress notification
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.Progress("Starting operation..."));
        
        await Task.Delay(1000);
        
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.Progress("50% complete"));
        
        await Task.Delay(1000);
        
        return ToolResponse.Success(request.Id, new { done = true });
    }
}
```

### 2. Client-Side: Receive Notifications

```javascript
// Open SSE connection
const eventSource = new EventSource('http://localhost:5000/mcp', {
  headers: {
    'MCP-Protocol-Version': '2025-11-25',
    'MCP-Session-Id': sessionId
  }
});

// Listen for notifications
eventSource.addEventListener('message', (event) => {
  const notification = JSON.parse(event.data);
  console.log('Notification:', notification);
});

eventSource.addEventListener('error', (error) => {
  console.error('SSE error:', error);
});
```

## Notification Types

### Progress Notifications

```csharp
await _notificationSender.SendNotificationAsync(
    NotificationMessage.Progress("Processing..."));
```

### Resource Updated

```csharp
await _notificationSender.SendNotificationAsync(
    NotificationMessage.ResourcesUpdated("file://data/users.json"));
```

### Tool Result

```csharp
await _notificationSender.SendNotificationAsync(
    NotificationMessage.ToolResult(
        toolCallId: "call-123",
        result: new { status = "completed" }));
```

### Custom Notifications

```csharp
await _notificationSender.SendNotificationAsync(
    new NotificationMessage(
        Method: "notifications/custom",
        Params: new { message = "Custom event" }));
```

## Session Management

Notifications require session management:

```csharp
// In Program.cs
builder.AddToolsService();  // Automatically includes session management

// Sessions are created on first POST request
// Session ID returned in MCP-Session-Id header
```

## SSE Format

Notifications are sent as Server-Sent Events:

```
id: 1
event: message
data: {"jsonrpc":"2.0","method":"notifications/progress","params":{"message":"Processing..."}}

id: 2
event: message
data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"uri":"file://data/users.json"}}
```

## Best Practices

### 1. Keep Messages Small

```csharp
// ✅ GOOD - Small message
await _notificationSender.SendNotificationAsync(
    NotificationMessage.Progress("Step 1 complete"));

// ❌ BAD - Large payload
await _notificationSender.SendNotificationAsync(
    NotificationMessage.Progress(largeDataString));
```

### 2. Use Resource Subscriptions

```csharp
// ✅ GOOD - Targeted notifications
await _notificationSender.SendNotificationAsync(
    NotificationMessage.ResourcesUpdated("file://specific.json"));
// Only subscribers receive this

// ❌ BAD - Broadcast to all
await _notificationSender.SendNotificationAsync(
    NotificationMessage.Progress("Update"));
// All sessions receive this
```

### 3. Handle Connection Loss

```javascript
eventSource.addEventListener('error', (error) => {
  // Reconnect automatically
  setTimeout(() => {
    eventSource = new EventSource(url);
  }, 5000);
});
```

## See Also

- [Resource Subscriptions](/features/resource-subscriptions/) - Subscribe to specific resources
- [Session Management](/features/sessions/) - Session lifecycle
- [MCP Protocol](/docs/mcp-protocol/) - Protocol specification
