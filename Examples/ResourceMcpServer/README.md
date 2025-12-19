# 📦 ResourceMcpServer Example

**Version:** v1.8.0  
**MCP Protocol:** 2025-11-25  
**Features:** Resources, Resource Subscriptions (v1.8.0)

## 📋 Overview

This example demonstrates MCP Resources support with optional resource subscriptions (v1.8.0 Phase 4).

**What are Resources?**
- Resources represent data or content that the server can provide to clients
- Unlike tools (which perform actions), resources are read-only data sources
- Examples: files, database records, system metrics, API data

**What are Resource Subscriptions?** (v1.8.0)
- Clients can subscribe to specific resource URIs
- Server sends notifications ONLY to subscribed sessions when resources update
- Reduces unnecessary notifications for high-frequency updates

## 🎯 Available Resources

| URI | Name | Type | Description |
|-----|------|------|-------------|
| `file://data/users.json` | User Data | JSON | Sample user records |
| `file://data/products.json` | Product Catalog | JSON | Sample product data |
| `file://logs/app.log` | Application Logs | Text | Server logs (last 1000 lines) |
| `db://users/example` | Example User | JSON | Single user record |
| `system://status` | System Status | JSON | Server health metrics |
| `system://metrics` | System Metrics | JSON | Performance statistics |

## 🚀 Quick Start

### 1. Start the Server

```bash
# HTTP/WebSocket mode
dotnet run

# stdio mode (for GitHub Copilot, Claude Desktop, etc.)
dotnet run -- --stdio
```

Server endpoints:
- **MCP (Recommended):** `POST /mcp`, `GET /mcp`, `DELETE /mcp`
- **Legacy HTTP:** `POST /rpc`
- **Legacy WebSocket:** `ws://localhost:5000/ws`

### 2. List Available Resources

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/list",
    "id": 1
  }'
```

### 3. Read a Resource

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/read",
    "params": {
      "uri": "system://status"
    },
    "id": 2
  }'
```

## 🔔 Resource Subscriptions (v1.8.0)

### Subscribe to Resource Updates

Clients can subscribe to specific resources to receive notifications when they change.

**Important:** Subscriptions require session management (use `/mcp` endpoint, not `/rpc`).

```bash
# 1. Subscribe to a resource (creates session automatically)
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/subscribe",
    "params": {
      "uri": "file://data/users.json"
    },
    "id": 3
  }'

# Response includes MCP-Session-Id header
# Example: MCP-Session-Id: a1b2c3d4e5f6...
```

### Open SSE Stream for Notifications

```bash
# 2. Open SSE stream to receive notifications (use session ID from step 1)
curl -N http://localhost:5000/mcp \
  -H "Accept: text/event-stream" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: <your-session-id>"

# You'll receive notifications when subscribed resources change:
# id: <session-id>-1
# event: message
# data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"uri":"file://data/users.json"}}
```

### Unsubscribe from Resource

```bash
# 3. Unsubscribe when done
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
    "id": 4
  }'
```

## 📝 Subscription Workflow

```
Client                              Server
  |                                   |
  |-- POST /mcp (subscribe) -------->|
  |   { method: "resources/subscribe",|
  |     params: { uri: "..." } }      |
  |                                   |
  |<---- 200 OK ----------------------|
  |   MCP-Session-Id: abc123          |
  |   { result: { subscribed: true } }|
  |                                   |
  |-- GET /mcp (SSE stream) --------->|
  |   MCP-Session-Id: abc123          |
  |   Accept: text/event-stream       |
  |                                   |
  |<==== SSE: keep-alive =============|
  |                                   |
  |                   [Resource changes]
  |                                   |
  |<==== SSE: notification ===========|
  |   notifications/resources/updated |
  |   { uri: "..." }                  |
  |                                   |
  |-- POST /mcp (unsubscribe) ------->|
  |   { method: "resources/unsubscribe"}
  |                                   |
  |<---- 200 OK ----------------------|
  |   { result: { unsubscribed: true }}|
```

## 🎨 Example: Subscribe to Multiple Resources

```javascript
// JavaScript example using fetch
const session = { id: null };

// 1. Subscribe to multiple resources
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
await subscribe('file://data/products.json');
await subscribe('system://metrics');

// 2. Open SSE stream
const eventSource = new EventSource(
  `http://localhost:5000/mcp?sessionId=${session.id}`,
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
    console.log('Resource updated:', notification.params.uri);
    // Re-fetch the updated resource here
  }
});

// 4. Cleanup
eventSource.close();
```

## 📚 Implementation Details

### Resource Definition

Resources are defined using the `[McpResource]` attribute:

```csharp
[McpResource("file://data/users.json",
    Name = "User Data",
    Description = "Sample user records",
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

### Sending Notifications

When a resource changes, notify subscribed clients:

```csharp
// Inject INotificationSender in your resource class
private readonly INotificationSender _notificationSender;

// Trigger notification when resource changes
await _notificationSender.SendNotificationAsync(
    NotificationMessage.ResourcesUpdated("file://data/users.json"),
    cancellationToken);

// Only sessions subscribed to "file://data/users.json" will receive this notification
```

## 🔍 Key Features

### Exact URI Matching (v1.8.0)
- Subscriptions use exact URI matching
- No wildcard support in v1.8.0 (planned for v1.9.0)
- Example: Subscribe to `file://data/users.json` → Only that URI triggers notifications

### Session-Based (v1.7.0+)
- Subscriptions are per-session
- Sessions auto-expire after 30 minutes of inactivity
- Use `/mcp` endpoint for session support

### Notification Filtering (v1.8.0)
- Server filters notifications by subscription
- Only subscribed sessions receive updates
- Reduces bandwidth for high-frequency notifications

### Automatic Cleanup (v1.8.0)
- Subscriptions cleared on session deletion
- Subscriptions cleared on session timeout
- No manual cleanup required

## 🧪 Testing

```bash
# Run all tests
dotnet test Examples/ResourceMcpServerTests/

# Run only subscription tests
dotnet test Examples/ResourceMcpServerTests/ \
  --filter "FullyQualifiedName~ResourceSubscriptionTests"
```

## 📖 Related Documentation

- **MCP Protocol:** [docs/MCP-Protocol.md](../../docs/MCP-Protocol.md)
- **Lifecycle Hooks:** [docs/LifecycleHooks.md](../../docs/LifecycleHooks.md)
- **Session Management:** [Examples/NotificationMcpServer/SESSION_MANAGEMENT.md](../NotificationMcpServer/SESSION_MANAGEMENT.md)
- **Resource Subscriptions:** [docs/ResourceSubscriptions.md](../../docs/ResourceSubscriptions.md)

## 🎯 Next Steps

1. **Explore Other Examples:**
   - [CalculatorMcpServer](../CalculatorMcpServer/) - Basic tools
   - [NotificationMcpServer](../NotificationMcpServer/) - Notifications
   - [PromptMcpServer](../PromptMcpServer/) - Prompt templates

2. **Read MCP Specification:**
   - https://spec.modelcontextprotocol.io/

3. **Try GitHub Copilot Integration:**
   ```bash
   dotnet run -- --stdio
   # Configure in GitHub Copilot settings
   ```

## 💡 Tips & Best Practices

### When to Use Subscriptions
- ✅ High-frequency resource updates (e.g., live metrics)
- ✅ Many clients, few interested in specific resources
- ✅ Reducing unnecessary notification traffic

### When NOT to Use Subscriptions
- ❌ Low-frequency updates (subscriptions add complexity)
- ❌ All clients need all notifications (just broadcast)
- ❌ Simple request-response patterns (use tools instead)

### Performance Considerations
- Subscriptions are stored in-memory (thread-safe)
- Each subscription adds minimal overhead (~100 bytes)
- Notification filtering is O(n) where n = active sessions
- Use exact URI matching for best performance

## 🚀 Version History

- **v1.8.0** - Added resource subscriptions (optional MCP feature)
- **v1.7.0** - Session management for `/mcp` endpoint
- **v1.5.0** - Initial resources support

---

**Need Help?**
- Open an issue: https://github.com/eyjolfurgudnivatne/mcp.gateway/issues
- Read docs: [README.md](../../README.md)
