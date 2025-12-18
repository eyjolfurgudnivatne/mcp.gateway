# 🚀 Phase 1: Streamable HTTP Transport - Design Document

**Created:** 18. desember 2025, kl. 22:30  
**Branch:** feat/v1.7.0-to-2025-11-25  
**Status:** Design & Planning  
**Target:** v1.7.0

---

## 📋 Executive Summary

**Mål:** Implementere MCP 2025-11-25 Streamable HTTP transport.

**Hovedendring:** Fra separate endpoints (`/rpc` POST, `/sse` GET) til **én unified endpoint** (`/mcp` POST+GET).

**Key features:**
1. ✅ Single endpoint (`/mcp`) handles both POST and GET
2. ✅ SSE event IDs for resumability
3. ✅ `Last-Event-ID` header support
4. ✅ Session management (`MCP-Session-Id`)
5. ✅ Protocol version validation (`MCP-Protocol-Version`)

---

## 🏗️ Current Architecture (v1.6.5)

### Dagens endpoint pattern:

```csharp
app.MapHttpRpcEndpoint("/rpc");  // POST only - JSON-RPC request/response
app.MapSseRpcEndpoint("/sse");   // POST only - JSON-RPC over SSE
app.MapWsRpcEndpoint("/ws");     // WebSocket - Full-duplex streaming
```

### Problem med dagens løsning:

| Issue | Impact | MCP 2025-11-25 |
|-------|--------|----------------|
| **Separate endpoints** | `/rpc` vs `/sse` | MUST be single endpoint |
| **No event IDs** | Cannot resume broken connections | MUST support `Last-Event-ID` |
| **No session management** | Stateless | SHOULD support `MCP-Session-Id` |
| **No version validation** | Implicit protocol version | MUST validate `MCP-Protocol-Version` |
| **POST-only SSE** | Client must POST to get SSE | SHOULD support GET for SSE |

---

## 🎯 Target Architecture (v1.7.0)

### New endpoint pattern:

```csharp
// MCP 2025-11-25 compliant (NEW):
app.MapStreamableHttpEndpoint("/mcp");  // POST + GET (SSE)
// → POST: Send JSON-RPC to server
// → GET: Open SSE stream for server→client messages

// Legacy support (DEPRECATED but still work):
app.MapHttpRpcEndpoint("/rpc");  // Old HTTP POST-only
app.MapSseRpcEndpoint("/sse");   // Old SSE endpoint

// Gateway-specific binary streaming (UNCHANGED):
app.MapWsRpcEndpoint("/ws");  // WebSocket for binary streaming
```

### Unified `/mcp` Endpoint Behavior:

**POST `/mcp`:**
- Accepts JSON-RPC requests
- Returns immediate JSON-RPC response OR
- Returns SSE stream with single response event (for async tools)

**GET `/mcp`:**
- Opens SSE stream
- Sends pending responses and notifications
- Supports `Last-Event-ID` for resumption
- Requires `MCP-Session-Id` header (if session management enabled)

---

## 📊 Wire Format Comparison

### v1.6.5 (Current) - Separate endpoints:

**Client sends request:**
```http
POST /rpc HTTP/1.1
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

**Server responds:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { "tools": [...] }
}
```

**Client opens SSE stream:**
```http
POST /sse HTTP/1.1
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

**Server responds with SSE:**
```http
HTTP/1.1 200 OK
Content-Type: text/event-stream

data: {"jsonrpc":"2.0","id":1,"result":{...}}

event: done
data: {}
```

---

### v1.7.0 (Target) - Unified `/mcp` endpoint:

**Client sends request (fast tools):**
```http
POST /mcp HTTP/1.1
Content-Type: application/json
MCP-Protocol-Version: 2025-11-25
MCP-Session-Id: abc123

{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 1
}
```

**Server responds immediately:**
```http
HTTP/1.1 200 OK
Content-Type: application/json
MCP-Session-Id: abc123

{
  "jsonrpc": "2.0",
  "id": 1,
  "result": { "tools": [...] }
}
```

---

**Client opens SSE stream for notifications:**
```http
GET /mcp HTTP/1.1
Accept: text/event-stream
MCP-Protocol-Version: 2025-11-25
MCP-Session-Id: abc123
Last-Event-ID: 42
```

**Server responds with SSE stream:**
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

: keep-alive
```

---

## 🔧 Implementation Plan

### Task 1.1: SSE Event ID Infrastructure (Day 1-2)

**Goal:** Implement globally unique event IDs for SSE messages.

**Components:**
```csharp
// New class: EventIdGenerator.cs
public class EventIdGenerator
{
    private long _nextId = 0;
    
    public string GenerateEventId(string? sessionId)
    {
        var id = Interlocked.Increment(ref _nextId);
        return sessionId != null 
            ? $"{sessionId}-{id}"  // Session-scoped
            : $"{id}";             // Global
    }
}

// New class: SseEventMessage.cs
public sealed record SseEventMessage(
    string Id,           // Event ID (e.g., "session123-42")
    string? Event,       // Event type (e.g., "message", "done")
    object Data,         // JSON-RPC message or notification
    int? Retry = null    // Retry interval (ms) for polling
);
```

**Changes to ToolInvoker.Sse.cs:**
```csharp
private async Task SendSseEventAsync(
    HttpResponse response,
    SseEventMessage message,
    CancellationToken cancellationToken)
{
    // Write event ID
    if (!string.IsNullOrEmpty(message.Id))
    {
        await response.WriteAsync($"id: {message.Id}\n", cancellationToken);
    }
    
    // Write event type (optional, defaults to "message")
    if (!string.IsNullOrEmpty(message.Event))
    {
        await response.WriteAsync($"event: {message.Event}\n", cancellationToken);
    }
    
    // Write retry interval (for polling)
    if (message.Retry.HasValue)
    {
        await response.WriteAsync($"retry: {message.Retry.Value}\n", cancellationToken);
    }
    
    // Write data
    var json = JsonSerializer.Serialize(message.Data, JsonOptions.Default);
    await response.WriteAsync($"data: {json}\n\n", cancellationToken);
    await response.Body.FlushAsync(cancellationToken);
}
```

---

### Task 1.2: Session Management (Day 3-4)

**Goal:** Track sessions and validate `MCP-Session-Id` header.

**Components:**
```csharp
// New class: SessionService.cs
public class SessionService
{
    private readonly ConcurrentDictionary<string, SessionInfo> _sessions = new();
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
    
    public string CreateSession()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new SessionInfo
        {
            Id = sessionId,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            EventIdCounter = 0
        };
        
        _sessions[sessionId] = session;
        return sessionId;
    }
    
    public bool ValidateSession(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return false;
        
        // Check timeout
        if (DateTime.UtcNow - session.LastActivity > _sessionTimeout)
        {
            _sessions.TryRemove(sessionId, out _);
            return false;
        }
        
        // Update last activity
        session.LastActivity = DateTime.UtcNow;
        return true;
    }
    
    public void DeleteSession(string sessionId)
    {
        _sessions.TryRemove(sessionId, out _);
    }
    
    public long GetNextEventId(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Interlocked.Increment(ref session.EventIdCounter);
        }
        throw new InvalidOperationException($"Session '{sessionId}' not found");
    }
}

public class SessionInfo
{
    public required string Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastActivity { get; set; }
    public long EventIdCounter { get; set; }
}
```

**DI Registration:**
```csharp
// ToolExtensions.cs
public static void AddToolsService(this WebApplicationBuilder builder)
{
    builder.Services.AddSingleton<ToolService>();
    builder.Services.AddScoped<ToolInvoker>();
    builder.Services.AddSingleton<INotificationSender, NotificationService>();
    builder.Services.AddSingleton<SessionService>();  // NEW
}
```

---

### Task 1.3: Protocol Version Validation Middleware (Day 5)

**Goal:** Validate `MCP-Protocol-Version` header on all requests.

**Components:**
```csharp
// New class: McpProtocolVersionMiddleware.cs
public class McpProtocolVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpProtocolVersionMiddleware> _logger;
    private static readonly string[] SupportedVersions = ["2025-11-25", "2025-06-18"];
    
    public McpProtocolVersionMiddleware(
        RequestDelegate next,
        ILogger<McpProtocolVersionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate MCP endpoints
        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            await _next(context);
            return;
        }
        
        // Extract protocol version header
        var protocolVersion = context.Request.Headers["MCP-Protocol-Version"].ToString();
        
        // Backward compatibility: If missing, assume 2025-03-26
        if (string.IsNullOrEmpty(protocolVersion))
        {
            protocolVersion = "2025-03-26";
            _logger.LogWarning("Missing MCP-Protocol-Version header, assuming {Version}", protocolVersion);
        }
        
        // Validate version
        if (!SupportedVersions.Contains(protocolVersion))
        {
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";
            
            var error = new
            {
                error = new
                {
                    code = -32600,
                    message = "Unsupported protocol version",
                    data = new
                    {
                        provided = protocolVersion,
                        supported = SupportedVersions
                    }
                }
            };
            
            await context.Response.WriteAsJsonAsync(error);
            return;
        }
        
        // Store version in HttpContext.Items for later use
        context.Items["MCP-Protocol-Version"] = protocolVersion;
        
        await _next(context);
    }
}

// Extension method:
public static class McpMiddlewareExtensions
{
    public static IApplicationBuilder UseProtocolVersionValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<McpProtocolVersionMiddleware>();
    }
}
```

**Usage:**
```csharp
// Program.cs
app.UseProtocolVersionValidation();  // Before MapStreamableHttpEndpoint
app.MapStreamableHttpEndpoint("/mcp");
```

---

### Task 1.4: Unified `/mcp` Endpoint (Day 6-8)

**Goal:** Implement single endpoint that handles both POST and GET.

**Components:**
```csharp
// New class: StreamableHttpEndpoint.cs
public static class StreamableHttpEndpoint
{
    public static WebApplication MapStreamableHttpEndpoint(
        this WebApplication app,
        string pattern)
    {
        // Handle POST (send request)
        app.MapPost(pattern, async (
            HttpContext context,
            ToolInvoker invoker,
            SessionService? sessionService,
            CancellationToken ct) =>
        {
            // 1. Validate/create session
            var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();
            
            if (sessionService != null)
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    // Create new session on initialize
                    sessionId = sessionService.CreateSession();
                    context.Response.Headers["MCP-Session-Id"] = sessionId;
                }
                else if (!sessionService.ValidateSession(sessionId))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = -32001,
                            message = "Session not found or expired",
                            data = new { sessionId }
                        }
                    });
                    return;
                }
            }
            
            // 2. Parse JSON-RPC request
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, ct);
            var element = doc.RootElement;
            
            // 3. Check if this is async tool (requires SSE stream)
            var requiresStreaming = await invoker.RequiresStreamingAsync(element);
            
            if (requiresStreaming)
            {
                // Return SSE stream
                await invoker.InvokeStreamableAsync(context, element, sessionId, ct);
            }
            else
            {
                // Return immediate JSON response
                var response = await invoker.InvokeSingleAsync(element, "http", ct);
                
                if (sessionId != null)
                {
                    context.Response.Headers["MCP-Session-Id"] = sessionId;
                }
                
                await context.Response.WriteAsJsonAsync(response, ct);
            }
        });
        
        // Handle GET (open SSE stream for notifications)
        app.MapGet(pattern, async (
            HttpContext context,
            SessionService? sessionService,
            INotificationSender? notificationService,
            CancellationToken ct) =>
        {
            // 1. Validate session
            var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();
            
            if (sessionService != null)
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = -32602,
                            message = "Missing MCP-Session-Id header"
                        }
                    });
                    return;
                }
                
                if (!sessionService.ValidateSession(sessionId))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = -32001,
                            message = "Session not found or expired"
                        }
                    });
                    return;
                }
            }
            
            // 2. Set SSE headers
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            
            if (sessionId != null)
            {
                context.Response.Headers["MCP-Session-Id"] = sessionId;
            }
            
            await context.Response.StartAsync(ct);
            
            // 3. Get Last-Event-ID for resumption
            var lastEventId = context.Request.Headers["Last-Event-ID"].ToString();
            
            // 4. Open long-lived SSE stream
            await OpenSseStreamAsync(
                context.Response,
                sessionId,
                lastEventId,
                sessionService,
                notificationService,
                ct);
        });
        
        // Handle DELETE (terminate session)
        app.MapDelete(pattern, async (
            HttpContext context,
            SessionService? sessionService) =>
        {
            if (sessionService == null)
            {
                context.Response.StatusCode = 501;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32601,
                        message = "Session management not enabled"
                    }
                });
                return;
            }
            
            var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();
            
            if (string.IsNullOrEmpty(sessionId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32602,
                        message = "Missing MCP-Session-Id header"
                    }
                });
                return;
            }
            
            sessionService.DeleteSession(sessionId);
            
            context.Response.StatusCode = 204; // No Content
        });
        
        return app;
    }
    
    private static async Task OpenSseStreamAsync(
        HttpResponse response,
        string? sessionId,
        string? lastEventId,
        SessionService? sessionService,
        INotificationSender? notificationService,
        CancellationToken ct)
    {
        // TODO: Implement long-lived SSE stream
        // - Replay messages after lastEventId (if provided)
        // - Subscribe to notifications
        // - Send keep-alive pings
        // - Handle client disconnect
        
        // Placeholder: Send keep-alive ping every 30s
        while (!ct.IsCancellationRequested)
        {
            await response.WriteAsync(": keep-alive\n\n", ct);
            await response.Body.FlushAsync(ct);
            await Task.Delay(30_000, ct);
        }
    }
}
```

---

## 🧪 Testing Strategy

### Test 1: POST `/mcp` with immediate response

```csharp
[Fact]
public async Task PostMcp_InitializeRequest_ReturnsImmediateResponse()
{
    // Arrange
    var request = new
    {
        jsonrpc = "2.0",
        method = "initialize",
        id = 1
    };

    var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
    httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
    httpRequest.Content = JsonContent.Create(request);

    // Act
    var response = await _client.SendAsync(httpRequest);

    // Assert
    response.EnsureSuccessStatusCode();
    Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    
    var sessionId = response.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
    Assert.NotNull(sessionId);
}
```

### Test 2: GET `/mcp` opens SSE stream

```csharp
[Fact]
public async Task GetMcp_OpensLongLivedSseStream()
{
    // Arrange
    var sessionId = await CreateSessionAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
    request.Headers.Add("MCP-Protocol-Version", "2025-11-25");
    request.Headers.Add("MCP-Session-Id", sessionId);
    request.Headers.Add("Accept", "text/event-stream");

    // Act
    var response = await _client.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead);

    // Assert
    response.EnsureSuccessStatusCode();
    Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    
    // Read first keep-alive ping
    var stream = await response.Content.ReadAsStreamAsync();
    var reader = new StreamReader(stream);
    
    var line = await reader.ReadLineAsync();
    Assert.StartsWith(":", line); // Keep-alive comment
}
```

### Test 3: `Last-Event-ID` resumption

```csharp
[Fact]
public async Task GetMcp_WithLastEventId_ReplaysMessages()
{
    // Arrange
    var sessionId = await CreateSessionAsync();
    
    // Simulate notification sent (event ID = 42)
    await SendNotificationAsync(sessionId, eventId: 42);
    
    // Client reconnects with Last-Event-ID: 42
    var request = new HttpRequestMessage(HttpMethod.Get, "/mcp");
    request.Headers.Add("MCP-Protocol-Version", "2025-11-25");
    request.Headers.Add("MCP-Session-Id", sessionId);
    request.Headers.Add("Last-Event-ID", "42");

    // Act
    var response = await _client.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead);

    // Assert
    response.EnsureSuccessStatusCode();
    
    // Should replay messages after event ID 42
    var stream = await response.Content.ReadAsStreamAsync();
    // ... parse SSE events and verify IDs > 42
}
```

### Test 4: Session expiry returns 404

```csharp
[Fact]
public async Task PostMcp_ExpiredSession_Returns404()
{
    // Arrange
    var expiredSessionId = "nonexistent-session";
    
    var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
    request.Headers.Add("MCP-Protocol-Version", "2025-11-25");
    request.Headers.Add("MCP-Session-Id", expiredSessionId);
    request.Content = JsonContent.Create(new
    {
        jsonrpc = "2.0",
        method = "tools/list",
        id = 1
    });

    // Act
    var response = await _client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

### Test 5: DELETE `/mcp` terminates session

```csharp
[Fact]
public async Task DeleteMcp_TerminatesSession()
{
    // Arrange
    var sessionId = await CreateSessionAsync();
    
    var request = new HttpRequestMessage(HttpMethod.Delete, "/mcp");
    request.Headers.Add("MCP-Protocol-Version", "2025-11-25");
    request.Headers.Add("MCP-Session-Id", sessionId);

    // Act
    var response = await _client.SendAsync(request);

    // Assert
    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    
    // Subsequent requests should fail with 404
    var postRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
    postRequest.Headers.Add("MCP-Session-Id", sessionId);
    postRequest.Content = JsonContent.Create(new { jsonrpc = "2.0", method = "tools/list", id = 1 });
    
    var postResponse = await _client.SendAsync(postRequest);
    Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
}
```

---

## 📊 Timeline & Milestones

| Day | Task | Deliverable | Status |
|-----|------|-------------|--------|
| **1-2** | SSE Event IDs | Event ID generation working | 📝 Planned |
| **3-4** | Session Management | Sessions created/validated | 📝 Planned |
| **5** | Protocol Version Middleware | Version validation working | 📝 Planned |
| **6-8** | Unified `/mcp` Endpoint | POST+GET+DELETE working | 📝 Planned |
| **9** | Testing | All tests passing | 📝 Planned |
| **10** | Documentation | Design doc + API docs | 📝 Planned |

**Total estimated:** 10 dager (2 uker)

---

## 🚨 Breaking Changes

### For server implementations:

**Before (v1.6.x):**
```csharp
app.MapHttpRpcEndpoint("/rpc");
app.MapSseRpcEndpoint("/sse");
```

**After (v1.7.0):**
```csharp
app.MapStreamableHttpEndpoint("/mcp");  // NEW
app.MapHttpRpcEndpoint("/rpc");          // Deprecated (still works)
app.MapSseRpcEndpoint("/sse");           // Deprecated (still works)
```

### For clients:

**Changes required:**
1. Send `MCP-Protocol-Version: 2025-11-25` header
2. Use `/mcp` endpoint instead of `/rpc` or `/sse`
3. Handle `MCP-Session-Id` header (if session management enabled)
4. Support `Last-Event-ID` for SSE resumption

---

## 🎯 Success Criteria

- ✅ Single `/mcp` endpoint handles POST, GET, DELETE
- ✅ SSE events have globally unique IDs
- ✅ `Last-Event-ID` resumption works
- ✅ Session management (create/validate/delete)
- ✅ Protocol version validation (400 Bad Request on invalid)
- ✅ Backward compatibility: `/rpc` and `/sse` still work
- ✅ All existing tests pass
- ✅ New tests for `/mcp` endpoint pass

---

## 📚 References

### MCP Specification 2025-11-25
- **Transports:** https://modelcontextprotocol.io/specification/2025-11-25/basic/transports
- **Lifecycle:** https://modelcontextprotocol.io/specification/2025-11-25/basic/lifecycle
- **SSE Transport:** https://modelcontextprotocol.io/specification/2025-11-25/basic/transports#http-with-sse

### Implementation notes:
- `.internal/notes/v1.7.0/omw-to-2025-11-25.md` - Main roadmap
- `Mcp.Gateway.Tools/ToolInvoker.Sse.cs` - Existing SSE implementation
- `Mcp.Gateway.Tools/ToolExtensions.cs` - Endpoint mapping extensions

---

**Status:** 📝 Design Complete  
**Next Step:** Start implementation (Task 1.1: SSE Event IDs)  
**Created:** 18. desember 2025, kl. 22:30  
**Author:** ARKo AS - AHelse Development Team
