# 🔐 Session Management in MCP Gateway (v1.7.0+)

**Feature:** Automatic session tracking with `MCP-Session-Id` headers  
**Version:** v1.7.0+  
**MCP Protocol:** 2025-11-25

---

## 📋 Overview

MCP Gateway v1.7.0 introduces **automatic session management** for the unified `/mcp` endpoint. This enables:
- ✅ **Stateful connections** over HTTP
- ✅ **Message buffering** (100 messages per session)
- ✅ **Last-Event-ID resumption** for SSE streams
- ✅ **Automatic session expiry** (30 min default)

---

## 🔧 How It Works

### 1. First Request (Session Creation)

**Client sends request WITHOUT session ID:**
```http
POST /mcp HTTP/1.1
MCP-Protocol-Version: 2025-11-25
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "initialize",
  "id": 1
}
```

**Server responds WITH new session ID:**
```http
HTTP/1.1 200 OK
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2025-11-25",
    "serverInfo": { "name": "mcp-gateway", "version": "1.7.0" },
    "capabilities": { "tools": {}, "notifications": { "tools": {} } }
  }
}
```

---

### 2. Subsequent Requests (Session Reuse)

**Client includes session ID:**
```http
POST /mcp HTTP/1.1
MCP-Protocol-Version: 2025-11-25
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "id": 2
}
```

**Server validates session and responds:**
```http
HTTP/1.1 200 OK
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "tools": [ ... ]
  }
}
```

---

### 3. Opening SSE Stream (for notifications)

**Client opens SSE stream WITH session ID:**
```http
GET /mcp HTTP/1.1
MCP-Protocol-Version: 2025-11-25
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
Accept: text/event-stream
```

**Server streams events:**
```http
HTTP/1.1 200 OK
Content-Type: text/event-stream
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000

id: 1
event: message
data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}

id: 2
event: message
data: {"jsonrpc":"2.0","id":2,"result":{"tools":[...]}}

: keep-alive (every 30s)
```

---

### 4. Session Resumption (Last-Event-ID)

If SSE connection drops, client can resume:

```http
GET /mcp HTTP/1.1
MCP-Protocol-Version: 2025-11-25
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
Last-Event-ID: 42
Accept: text/event-stream
```

**Server replays buffered messages:**
```http
HTTP/1.1 200 OK
Content-Type: text/event-stream

id: 43
event: message
data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}

id: 44
event: message
data: {"jsonrpc":"2.0","id":5,"result":{...}}

: (continues with new events)
```

---

### 5. Session Expiry

Sessions expire after **30 minutes** of inactivity:

```http
POST /mcp HTTP/1.1
MCP-Session-Id: expired-session-id
```

**Server responds with 404:**
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "id": null,
  "error": {
    "code": -32001,
    "message": "Session not found or expired. Please re-initialize with POST /mcp.",
    "data": {
      "sessionId": "expired-session-id",
      "timeoutMinutes": 30
    }
  }
}
```

**Client should re-initialize** (POST /mcp without session ID).

---

### 6. Manual Session Termination

Client can explicitly terminate session:

```http
DELETE /mcp HTTP/1.1
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
```

**Server responds:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "result": {
    "message": "Session terminated",
    "sessionId": "550e8400-e29b-41d4-a716-446655440000"
  }
}
```

---

## 🧪 Testing Session Management

### Using curl

**1. Initialize (get session ID):**
```bash
curl -X POST http://localhost:5240/mcp \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}' \
  -i
```

**Output:**
```
HTTP/1.1 200 OK
MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000
...
```

**2. Use session ID in subsequent requests:**
```bash
SESSION_ID="550e8400-e29b-41d4-a716-446655440000"

curl -X POST http://localhost:5240/mcp \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: $SESSION_ID" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
```

**3. Open SSE stream:**
```bash
curl -N http://localhost:5240/mcp \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "MCP-Session-Id: $SESSION_ID" \
  -H "Accept: text/event-stream"
```

**4. Trigger notification (in another terminal):**
```bash
curl -X POST http://localhost:5240/api/notify/tools
```

**SSE stream will receive:**
```
id: 5
event: message
data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}
```

---

## 📚 Session Features

### Message Buffering
- **Capacity:** 100 messages per session
- **Overflow:** Oldest messages removed (FIFO)
- **Use case:** Client reconnects after temporary network failure

### Event IDs
- **Format:** Sequential integers (1, 2, 3, ...)
- **Scope:** Per-session (each session starts at 1)
- **Thread-safe:** Atomic increment (`Interlocked.Increment`)

### Timeout Configuration
- **Default:** 30 minutes
- **Configurable:** Via DI when registering `SessionService`
- **Activity:** Any request extends session lifetime

### Backward Compatibility
- **Legacy endpoints:** `/rpc`, `/sse`, `/ws` still work (no session management)
- **Optional:** Session management only applies to `/mcp` endpoint
- **Migration:** Gradual adoption possible

---

## 🔧 Implementation Details

### Session Storage
```csharp
// SessionInfo per session
public record SessionInfo
{
    public string Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastAccessedAt { get; set; }
    public int EventIdCounter { get; set; }
    public MessageBuffer MessageBuffer { get; init; }
}
```

### Session Service
```csharp
// Singleton, thread-safe
public class SessionService
{
    public SessionInfo GetOrCreateSession(string? sessionId);
    public bool ValidateSession(string sessionId);
    public void TerminateSession(string sessionId);
    public IEnumerable<SessionInfo> GetAllSessions(); // For notifications
}
```

### SSE Stream Registry
```csharp
// Tracks active SSE streams per session
public class SseStreamRegistry
{
    public void RegisterStream(string sessionId, HttpResponse response);
    public void UnregisterStream(string sessionId);
    public Task BroadcastAsync(string sessionId, string eventData);
}
```

---

## ⚠️ Important Notes

### Security Considerations
1. **Session IDs are GUIDs** - hard to guess, but not cryptographically secure
2. **Use HTTPS** in production to protect session IDs
3. **Session timeout** prevents abandoned sessions from consuming memory
4. **No authentication** - session management ≠ user authentication

### Performance
- **Memory usage:** ~1 KB per active session (plus buffered messages)
- **Cleanup:** Automatic expiry every 30 minutes
- **Scalability:** Designed for thousands of concurrent sessions

### Migration Path
```csharp
// v1.6.x (no session management):
app.MapHttpRpcEndpoint("/rpc");
app.MapSseRpcEndpoint("/sse");

// v1.7.0 (with session management):
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");  // NEW with sessions
// Legacy endpoints still work (no sessions)
```

---

## 🚀 Next Steps

- **Try the NotificationMcpServer example** to see SSE + sessions in action
- **Read `.internal/notes/v1.7.0/phase-1-streamable-http-design.md`** for detailed design
- **Check CHANGELOG.md** for v1.7.0 release notes

---

**Version:** v1.7.0+  
**MCP Protocol:** 2025-11-25  
**License:** MIT © 2024–2025 ARKo AS – AHelse Development Team
