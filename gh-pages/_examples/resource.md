---
layout: mcp-default
title: Resource Server Example
description: Complete MCP server with file and system resources
breadcrumbs:
  - title: Home
    url: /
  - title: Examples
    url: /examples/resource/    
  - title: Resource Server
    url: /examples/resource/
prev: false
next: false
toc: true
---

# Resource Server Example

**Version:** v1.5.0+  
**Features:** Resources, Subscriptions, Notifications  
**Complexity:** Intermediate

## Overview

A complete MCP server demonstrating resource handling with:
- ✅ **File resources** - Application logs and configuration
- ✅ **System resources** - Live metrics and status
- ✅ **Data resources** - User data with subscriptions
- ✅ **Real-time notifications** - Push updates to subscribers

Perfect for learning:
- How to define resources with `[McpResource]`
- Resource content types (text, JSON, blob)
- Resource subscriptions for targeted notifications
- Integration with file system, databases, APIs

## Quick Start

### Run the Server

```bash
cd Examples/ResourceMcpServer
dotnet run
```

Server starts at: `http://localhost:5000`

### Test Resources

```bash
# List all resources
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/list",
    "id": 1
  }'

# Read application logs
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/read",
    "params": {
      "uri": "file://logs/app.log"
    },
    "id": 2
  }'
```

## Available Resources

### File Resources

#### 1. Application Logs (`file://logs/app.log`)

Returns recent log entries:

```json
{
  "uri": "file://logs/app.log",
  "mimeType": "text/plain",
  "text": "[2025-12-20 18:00:00] INFO: Application started\n[2025-12-20 18:01:00] DEBUG: Initializing..."
}
```

**Use case:** Monitor application activity, debug issues

#### 2. Application Settings (`file://config/settings.json`)

Returns current configuration:

```json
{
  "uri": "file://config/settings.json",
  "mimeType": "application/json",
  "text": "{\"environment\":\"Development\",\"logging\":{...}}"
}
```

**Use case:** Inspect configuration, verify settings

### System Resources

#### 3. System Metrics (`system://metrics`)

Returns live system metrics:

```json
{
  "uri": "system://metrics",
  "mimeType": "application/json",
  "text": "{\"cpu\":45.2,\"memory\":2048,\"uptime\":\"02:30:15\"}"
}
```

**Use case:** Monitor performance, health checks

#### 4. System Status (`system://status`)

Returns application health:

```json
{
  "uri": "system://status",
  "mimeType": "application/json",
  "text": "{\"status\":\"healthy\",\"version\":\"1.8.0\",\"ready\":true}"
}
```

**Use case:** Health monitoring, readiness checks

### Data Resources

#### 5. User Data (`data://users`)

Returns user records (with subscriptions support):

```json
{
  "uri": "data://users",
  "mimeType": "application/json",
  "text": "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]"
}
```

**Use case:** CRUD operations, real-time updates

## Resource Subscriptions

Subscribe to resources for real-time notifications when they change:

### 1. Subscribe to a Resource

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "resources/subscribe",
    "params": {
      "uri": "data://users"
    },
    "id": 3
  }'
```

**Response:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "subscribed": true,
    "uri": "data://users"
  },
  "id": 3
}
```

### 2. Open SSE Stream

```bash
curl -N http://localhost:5000/mcp \
  -H "Accept: text/event-stream" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: <session-id>"
```

You'll receive notifications when `data://users` is updated:

```
id: 1
event: message
data: {"jsonrpc":"2.0","method":"notifications/resources/updated","params":{"uri":"data://users"}}
```

### 3. Update Resource (Triggers Notification)

```bash
curl -X POST http://localhost:5000/mcp \
  -H "Content-Type: application/json" \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "update_users",
      "arguments": {
        "newUser": {"id": 3, "name": "Charlie"}
      }
    },
    "id": 4
  }'
```

**All subscribed sessions receive notification!**

## Code Examples

### Defining a File Resource

```csharp
using Mcp.Gateway.Tools;

public class FileResource
{
    [McpResource("file://logs/app.log",
        Name = "Application Logs",
        Description = "Recent application log entries",
        MimeType = "text/plain")]
    public JsonRpcMessage AppLogs(JsonRpcMessage request)
    {
        // Read log file (simplified example)
        var logs = File.ReadAllText("logs/app.log");
        
        return ToolResponse.Success(
            request.Id,
            new ReadResourceResult
            {
                Meta = new Dictionary<string, object> {
                    { "tools.gateway.mcp/status", "Hello World" }
                },
                Contents = [new ResourceContent(
                    Uri: "file://logs/app.log",
                    MimeType: "text/plain",
                    Text: logs)]
            });
    }
}
```

### System Resource with Live Data

```csharp
[McpResource("system://metrics",
    Name = "System Metrics",
    Description = "Live system performance metrics",
    MimeType = "application/json")]
public JsonRpcMessage SystemMetrics(JsonRpcMessage request)
{
    var metrics = new
    {
        cpu = GetCpuUsage(),
        memory = GetMemoryUsage(),
        uptime = GetUptime()
    };
    
    var json = JsonSerializer.Serialize(metrics);
    
    return ToolResponse.Success(
        request.Id,
        new ReadResourceResult
        {
            Contents = [new ResourceContent(
                Uri: "system://metrics",
                MimeType: "application/json",
                Text: json)]
        });
}
```

### Data Resource with Notifications

```csharp
using Mcp.Gateway.Tools.Notifications;

public class DataResource
{
    private readonly INotificationSender _notificationSender;
    
    // Option 1: Constructor injection (must register in DI)
    public DataResource(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    [McpTool("update_users")]
    public async Task<JsonRpcMessage> UpdateUsers(JsonRpcMessage request)
    {
        // Update user data (simplified)
        await UpdateUserDatabase();
        
        // Notify subscribers
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("data://users"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}

// Option 2: Method parameter injection (no registration needed)
public class DataResource
{
    [McpTool("update_users")]
    public async Task<JsonRpcMessage> UpdateUsers(
        JsonRpcMessage request,
        INotificationSender notificationSender)  // ← Auto-injected!
    {
        await UpdateUserDatabase();
        
        await notificationSender.SendNotificationAsync(
            NotificationMessage.ResourcesUpdated("data://users"));
        
        return ToolResponse.Success(request.Id, new { updated = true });
    }
}
```

## Testing with JavaScript Client

```javascript
// 1. List resources
const listResponse = await fetch('http://localhost:5000/mcp', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'MCP-Protocol-Version': '2025-11-25'
  },
  body: JSON.stringify({
    jsonrpc: '2.0',
    method: 'resources/list',
    id: 1
  })
});

const resources = await listResponse.json();
console.log('Available resources:', resources.result.resources);

// 2. Subscribe to resource
const session = { id: null };

const subscribeResponse = await fetch('http://localhost:5000/mcp', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'MCP-Protocol-Version': '2025-11-25'
  },
  body: JSON.stringify({
    jsonrpc: '2.0',
    method: 'resources/subscribe',
    params: { uri: 'data://users' },
    id: 2
  })
});

// Save session ID
session.id = subscribeResponse.headers.get('MCP-Session-Id');

// 3. Open SSE stream
const eventSource = new EventSource(
  'http://localhost:5000/mcp',
  {
    headers: {
      'MCP-Protocol-Version': '2025-11-25',
      'MCP-Session-Id': session.id
    }
  }
);

// 4. Listen for updates
eventSource.addEventListener('message', (event) => {
  const notification = JSON.parse(event.data);
  
  if (notification.method === 'notifications/resources/updated') {
    console.log('Resource updated:', notification.params.uri);
    
    // Re-read the resource
    readResource(notification.params.uri);
  }
});

// 5. Read resource
async function readResource(uri) {
  const response = await fetch('http://localhost:5000/mcp', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'MCP-Protocol-Version': '2025-11-25'
    },
    body: JSON.stringify({
      jsonrpc: '2.0',
      method: 'resources/read',
      params: { uri },
      id: Date.now()
    })
  });
  
  const data = await response.json();
  console.log('Resource content:', data.result);
}
```

## Integration Tests

The ResourceMcpServer includes comprehensive tests:

```bash
cd Examples/ResourceMcpServerTests
dotnet test
```

**Test coverage:**
- ✅ List all resources
- ✅ Read each resource type
- ✅ Subscribe to resources
- ✅ Receive notifications on updates
- ✅ Unsubscribe from resources
- ✅ Session management

## Best Practices

### 1. Resource URIs

Use clear, descriptive URIs:

```csharp
// ✅ GOOD
"file://logs/app.log"
"system://metrics"
"data://users"

// ❌ BAD
"file://1"
"sys"
"data"
```

### 2. MIME Types

Use standard MIME types:

```csharp
// Text content
MimeType = "text/plain"

// JSON data
MimeType = "application/json"

// Binary data
MimeType = "application/octet-stream"
```

### 3. Descriptions

Write clear, helpful descriptions:

```csharp
// ✅ GOOD
Description = "Recent application log entries (last 100 lines)"

// ❌ BAD
Description = "Logs"
```

### 4. Error Handling

Handle missing resources gracefully:

```csharp
[McpResource("file://data/report.pdf")]
public JsonRpcMessage Report(JsonRpcMessage request)
{
    var filePath = "data/report.pdf";
    
    if (!File.Exists(filePath))
    {
        throw new ToolNotFoundException(
            $"Resource not found: file://data/report.pdf");
    }
    
    var data = File.ReadAllBytes(filePath);
    var base64 = Convert.ToBase64String(data);
    
    var content = new ResourceContent(
        Uri: "file://data/report.pdf",
        MimeType: "application/pdf",
        Blob: base64);

    return ToolResponse.Success(
        request.Id,
        new ReadResourceResult 
        {
            Contents = [content]
        });
}
```

## Common Use Cases

### 1. Configuration Management

```csharp
[McpResource("file://config/app.json")]
public JsonRpcMessage Config(JsonRpcMessage request)
{
    var config = File.ReadAllText("appsettings.json");

    var content = new ResourceContent(
        Uri: "file://config/app.json",
        MimeType: "application/json",
        Text: config);

    return ToolResponse.Success(
        request.Id,
        new ReadResourceResult 
        {
            Contents = [content]
        });
}
```

### 2. Log Monitoring

```csharp
[McpResource("file://logs/errors.log")]
public JsonRpcMessage ErrorLogs(JsonRpcMessage request)
{
    var lines = File.ReadLines("logs/errors.log")
        .TakeLast(100)
        .ToList();
    
    var content = string.Join("\n", lines);
    
    return ToolResponse.Success(request.Id,
        new ReadResourceResult 
        {
            Contents = [new ResourceContent(
                Uri: "file://logs/errors.log",
                MimeType: "text/plain",
                Text: content)]
        });
}
```

### 3. Database Access

```csharp
[McpResource("data://products")]
public async Task<JsonRpcMessage> Products(JsonRpcMessage request)
{
    var products = await _dbContext.Products.ToListAsync();
    var json = JsonSerializer.Serialize(products);
    
    return ToolResponse.Success(request.Id,
        new ReadResourceResult
        {
            Contents = [new ResourceContent(
                Uri: "data://products",
                MimeType: "application/json",
                Text: json)]
        });
}
```

### 4. API Integration

```csharp
[McpResource("api://weather/current")]
public async Task<JsonRpcMessage> Weather(JsonRpcMessage request)
{
    var response = await _httpClient.GetStringAsync(
        "https://api.weather.com/current");
    
    return ToolResponse.Success(request.Id,
        new ReadResourceResult
        {
            Contents = [new ResourceContent(
                Uri: "api://weather/current",
                MimeType: "application/json",
                Text: response)]
        });
}
```

## See Also

- [Resource Subscriptions](/mcp.gateway/features/resource-subscriptions/) - Complete subscription guide
- [Resources API](/mcp.gateway/api/resources/) - Complete Resources API reference
- [Notifications](/mcp.gateway/features/notifications/) - Real-time notifications
