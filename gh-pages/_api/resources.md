---
layout: mcp-default
title: Resources API Reference
description: Complete reference for the Resources API
breadcrumbs:
  - title: Home
    url: /
  - title: API Reference
    url: /api/
  - title: Resources API
    url: /api/resources/
toc: true
---

# Resources API Reference

Complete reference for MCP Gateway Resources API.

## Overview

Resources represent data that can be read by AI assistants. Common use cases:
- File contents
- Database records
- API responses
- Live metrics
- Configuration data

## Quick Reference

| Method | Description |
|--------|-------------|
| `resources/list` | List all available resources |
| `resources/read` | Read a specific resource |
| `resources/subscribe` | Subscribe to resource updates (v1.8.0) |
| `resources/unsubscribe` | Unsubscribe from updates (v1.8.0) |

## resources/list

List all resources available on the server.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "resources/list",
  "params": {
    "cursor": "optional-cursor"
  },
  "id": 1
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "resources": [
      {
        "uri": "file://data/users.json",
        "name": "User Data",
        "description": "User records in JSON format",
        "mimeType": "application/json"
      }
    ],
    "nextCursor": "optional-cursor"
  },
  "id": 1
}
```

## resources/read

Read a specific resource by URI.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "resources/read",
  "params": {
    "uri": "file://data/users.json"
  },
  "id": 2
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "contents": [
      {
        "uri": "file://data/users.json",
        "mimeType": "application/json",
        "text": "[{\"id\":1,\"name\":\"Alice\"}]"
      }
    ]
  },
  "id": 2
}
```

## resources/subscribe (v1.8.0)

Subscribe to notifications when a resource changes.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "resources/subscribe",
  "params": {
    "uri": "file://data/users.json"
  },
  "id": 3
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "subscribed": true,
    "uri": "file://data/users.json"
  },
  "id": 3
}
```

**Requirements:**
- Session management must be enabled
- Resource must exist (validated before subscription)
- Exact URI matching only (v1.8.0)

## resources/unsubscribe (v1.8.0)

Unsubscribe from resource update notifications.

### Request

```json
{
  "jsonrpc": "2.0",
  "method": "resources/unsubscribe",
  "params": {
    "uri": "file://data/users.json"
  },
  "id": 4
}
```

### Response

```json
{
  "jsonrpc": "2.0",
  "result": {
    "unsubscribed": true,
    "uri": "file://data/users.json"
  },
  "id": 4
}
```

**Note:** Idempotent - safe to call multiple times.

## Defining Resources

### File Resource

```csharp
using Mcp.Gateway.Tools;

public class FileResources
{
    [McpResource("file://data/users.json",
        Name = "User Data",
        Description = "User records in JSON format",
        MimeType = "application/json")]
    public JsonRpcMessage GetUsers(JsonRpcMessage request)
    {
        var data = File.ReadAllText("data/users.json");
        
        return ResourceResponse.Success(
            request.Id,
            new ResourceContent(
                Uri: "file://data/users.json",
                MimeType: "application/json",
                Text: data));
    }
}
```

### Database Resource

```csharp
[McpResource("db://users",
    Name = "User Database",
    Description = "All users from database",
    MimeType = "application/json")]
public async Task<JsonRpcMessage> GetUsersFromDb(JsonRpcMessage request)
{
    var users = await _dbContext.Users.ToListAsync();
    var json = JsonSerializer.Serialize(users);
    
    return ResourceResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "db://users",
            MimeType: "application/json",
            Text: json));
}
```

### Live Metrics Resource

```csharp
[McpResource("system://metrics",
    Name = "System Metrics",
    Description = "Live system metrics",
    MimeType: "application/json")]
public JsonRpcMessage GetMetrics(JsonRpcMessage request)
{
    var metrics = new
    {
        cpu = GetCpuUsage(),
        memory = GetMemoryUsage(),
        timestamp = DateTime.UtcNow
    };
    
    return ResourceResponse.Success(
        request.Id,
        new ResourceContent(
            Uri: "system://metrics",
            MimeType: "application/json",
            Text: JsonSerializer.Serialize(metrics)));
}
```

## Resource Attributes

### [McpResource]

Marks a method as an MCP resource.

```csharp
[McpResource(
    string uri,                     // Required: Resource URI
    string? Name = null,            // Optional: Display name
    string? Description = null,     // Optional: Description
    string? MimeType = null)]       // Optional: Content type
```

## Sending Notifications

When a resource changes, notify subscribed clients using dependency injection:

### Option 1: Constructor Injection

Requires class registration in DI:

```csharp
using Mcp.Gateway.Tools.Notifications;

public class FileResources
{
    private readonly INotificationSender _notificationSender;
    
    public FileResources(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    [McpTool("update_users")]
    public async Task<JsonRpcMessage> UpdateUsers(
        TypedJsonRpc<UpdateUsersArgs> request)
    {
        var args = request.GetParams()!;
        
        // Update the file
        await File.WriteAllTextAsync("data/users.json", args.Data);
        
        // Notify subscribed sessions (v1.8.0)
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("file://data/users.json"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}

// Register in DI:
builder.Services.AddScoped<FileResources>();
```

### Option 2: Method Parameter Injection

No class registration needed - parameters resolved from DI:

```csharp
using Mcp.Gateway.Tools.Notifications;

public class FileResources
{
    [McpTool("update_users")]
    public async Task<JsonRpcMessage> UpdateUsers(
        TypedJsonRpc<UpdateUsersArgs> request,
        INotificationSender notificationSender)  // ← Automatically injected!
    {
        var args = request.GetParams()!;
        
        // Update the file
        await File.WriteAllTextAsync("data/users.json", args.Data);
        
        // Notify subscribed sessions (v1.8.0)
        await notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("file://data/users.json"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}

// No registration needed - class auto-discovered!
```

**Parameter resolution order:**
1. `JsonRpcMessage` or `TypedJsonRpc<T>` - The request (must be first parameter)
2. Additional parameters - Resolved from DI container (in order)

**Benefits of method parameter injection:**
- ✅ No class registration needed
- ✅ Simpler for resources with few dependencies
- ✅ Clear what each method needs
- ✅ Easier testing (mock parameters directly)

## URI Schemes

Common URI schemes:

| Scheme | Example | Use Case |
|--------|---------|----------|
| `file://` | `file://data/config.json` | File system |
| `db://` | `db://users` | Database records |
| `http://` | `http://api.example.com/data` | HTTP endpoints |
| `system://` | `system://metrics` | System information |
| `custom://` | `custom://my-resource` | Custom schemes |

## MIME Types

Common MIME types:

| Type | Description |
|------|-------------|
| `application/json` | JSON data |
| `text/plain` | Plain text |
| `text/markdown` | Markdown |
| `text/html` | HTML |
| `application/xml` | XML |

## Best Practices

### 1. Use Descriptive URIs

```csharp
// ✅ GOOD
[McpResource("file://logs/app-2025-12-20.log")]

// ❌ BAD
[McpResource("file://log1")]
```

### 2. Specify MIME Type

```csharp
[McpResource("file://data/users.json",
    MimeType = "application/json")]
```

### 3. Handle Missing Resources

```csharp
[McpResource("file://data/users.json")]
public JsonRpcMessage GetUsers(JsonRpcMessage request)
{
    if (!File.Exists("data/users.json"))
    {
        throw new ToolInvalidParamsException(
            "Resource not found: users.json");
    }
    
    var data = File.ReadAllText("data/users.json");
    return ResourceResponse.Success(...);
}
```

### 4. Cache When Appropriate

```csharp
private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

[McpResource("db://users")]
public async Task<JsonRpcMessage> GetUsers(JsonRpcMessage request)
{
    var cacheKey = "users";
    
    if (!_cache.TryGetValue(cacheKey, out string? data))
    {
        var users = await _dbContext.Users.ToListAsync();
        data = JsonSerializer.Serialize(users);
        
        _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
    }
    
    return ResourceResponse.Success(...);
}
```

## Resource Subscriptions (v1.8.0)

### Server-Side

Resources support subscriptions automatically when:
1. Session management is enabled
2. Resource is registered via `[McpResource]`
3. Server sends notifications on changes

```csharp
// Resource definition
[McpResource("file://data/users.json")]
public JsonRpcMessage GetUsers(JsonRpcMessage request) { ... }

// When resource changes
await _notificationSender.SendNotificationAsync(
    NotificationMessage.ResourcesUpdated("file://data/users.json"));
// Only subscribed sessions receive notification!
```

### Client-Side

```javascript
// 1. Subscribe
await fetch('/mcp', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'MCP-Session-Id': sessionId
  },
  body: JSON.stringify({
    jsonrpc: '2.0',
    method: 'resources/subscribe',
    params: { uri: 'file://data/users.json' },
    id: 1
  })
});

// 2. Open SSE stream
const eventSource = new EventSource('/mcp', {
  headers: { 'MCP-Session-Id': sessionId }
});

eventSource.addEventListener('message', (event) => {
  const notification = JSON.parse(event.data);
  if (notification.method === 'notifications/resources/updated') {
    // Re-fetch the resource
    fetchResource(notification.params.uri);
  }
});

// 3. Unsubscribe
await fetch('/mcp', {
  method: 'POST',
  body: JSON.stringify({
    jsonrpc: '2.0',
    method: 'resources/unsubscribe',
    params: { uri: 'file://data/users.json' },
    id: 2
  })
});
```

## Testing

### Unit Test

```csharp
[Fact]
public void GetUsers_FileExists_ReturnsContent()
{
    // Arrange
    var resources = new FileResources();
    var request = JsonRpcMessage.CreateRequest("resources/read", "1");
    
    // Act
    var response = resources.GetUsers(request);
    
    // Assert
    Assert.NotNull(response.Result);
}
```

### Integration Test

```csharp
[Fact]
public async Task ResourcesRead_ValidUri_ReturnsContent()
{
    // Arrange
    using var server = new McpGatewayFixture();
    var client = server.CreateClient();
    
    var request = new
    {
        jsonrpc = "2.0",
        method = "resources/read",
        @params = new { uri = "file://data/users.json" },
        id = 1
    };
    
    // Act
    var response = await client.PostAsJsonAsync("/mcp", request);
    var result = await response.Content.ReadFromJsonAsync<JsonDocument>();
    
    // Assert
    Assert.True(result.RootElement.TryGetProperty("result", out var resultProp));
    Assert.True(resultProp.TryGetProperty("contents", out _));
}
```

## See Also

- [Resource Subscriptions](/mcp.gateway/features/resource-subscriptions/) - Complete subscription guide
- [Tools API](/mcp.gateway/api/tools/) - Tool invocation reference
- [Resource Example](/mcp.gateway/examples/resource/) - Complete resource server example
