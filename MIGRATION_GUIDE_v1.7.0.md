# 📖 Migration Guide: v1.6.x → v1.7.0

**From:** v1.6.x (MCP 2025-06-18 / 2025-11-25 partial)  
**To:** v1.7.0 (MCP 2025-11-25 - 100% compliant!)  
**Breaking Changes:** ❌ **NONE** (fully backward compatible!)

---

## 🎯 TL;DR

**Good news:** v1.7.0 is **100% backward compatible**! Your v1.6.x code continues to work unchanged.

**What's new:**
- ✅ Unified `/mcp` endpoint (POST + GET + DELETE)
- ✅ SSE-based notifications (replaces WebSocket-only approach)
- ✅ Session management (`MCP-Session-Id` headers)
- ✅ Protocol version validation
- ✅ Message buffering and `Last-Event-ID` resumption

**Migration strategy:** Adopt new features gradually—no rush!

---

## 🚀 Quick Migration Path

### Option 1: Keep Existing Code (Works!)

```csharp
// v1.6.x code - STILL WORKS in v1.7.0!
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");
```

**✅ Zero changes needed!**

---

### Option 2: Adopt New Endpoint (Recommended)

```csharp
// v1.7.0 - Add protocol validation + unified endpoint
app.UseProtocolVersionValidation();  // NEW
app.MapStreamableHttpEndpoint("/mcp");  // NEW

// Keep legacy endpoints (optional, deprecated)
app.MapHttpRpcEndpoint("/rpc");  // DEPRECATED but functional
app.MapWsRpcEndpoint("/ws");     // Keep for binary streaming
app.MapSseRpcEndpoint("/sse");   // DEPRECATED, use GET /mcp
```

**Benefits:**
- ✅ MCP 2025-11-25 compliant
- ✅ Automatic session management
- ✅ Better error messages
- ✅ Future-proof

---

## 📋 Feature-by-Feature Migration

### 1. Unified `/mcp` Endpoint

#### Before (v1.6.x):
```csharp
// Separate endpoints for different operations
app.MapHttpRpcEndpoint("/rpc");  // POST only
app.MapSseRpcEndpoint("/sse");   // POST only (SSE response)
app.MapWsRpcEndpoint("/ws");     // WebSocket
```

#### After (v1.7.0):
```csharp
// Single unified endpoint
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");  // POST + GET + DELETE

// Legacy endpoints still work (optional)
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");
```

**Client changes:**
```bash
# v1.6.x
curl -X POST http://localhost:5000/rpc ...

# v1.7.0 (recommended)
curl -X POST http://localhost:5000/mcp ...

# v1.7.0 (SSE stream)
curl -N http://localhost:5000/mcp -H "Accept: text/event-stream" ...
```

---

### 2. Protocol Version Validation

#### Before (v1.6.x):
- No explicit version header validation
- Protocol version returned in `initialize` but not checked on requests

#### After (v1.7.0):
```csharp
// Add middleware to validate MCP-Protocol-Version header
app.UseProtocolVersionValidation();
```

**Supported versions:**
- `2025-11-25` (latest)
- `2025-06-18` (previous)
- `2025-03-26` (oldest)

**Client behavior:**
```bash
# v1.7.0 - Send protocol version header (recommended)
curl -H "MCP-Protocol-Version: 2025-11-25" ...

# Missing header → defaults to 2025-03-26 (backward compat)
```

**Error handling:**
```http
HTTP/1.1 400 Bad Request
{
  "error": {
    "code": -32600,
    "message": "Unsupported protocol version",
    "data": {
      "provided": "2024-01-01",
      "supported": ["2025-11-25", "2025-06-18", "2025-03-26"]
    }
  }
}
```

---

### 3. Session Management

#### Before (v1.6.x):
- Stateless HTTP requests
- No session tracking
- No message buffering

#### After (v1.7.0):
```csharp
// Automatic session management for /mcp endpoint
app.MapStreamableHttpEndpoint("/mcp");
```

**Client changes:**
```bash
# 1. Initialize (server returns session ID)
curl -X POST http://localhost:5000/mcp \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}' \
  -i

# Response includes MCP-Session-Id header:
# MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000

# 2. Use session ID in subsequent requests
curl -X POST http://localhost:5000/mcp \
  -H "MCP-Session-Id: 550e8400-e29b-41d4-a716-446655440000" \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
```

**Features:**
- ✅ **Automatic:** No code changes needed
- ✅ **Message buffering:** 100 messages per session
- ✅ **Session expiry:** 30 min default
- ✅ **Optional:** Legacy endpoints work without sessions

---

### 4. SSE-Based Notifications

#### Before (v1.6.x):
```csharp
// WebSocket-only notifications
public class MyTools
{
    private readonly INotificationSender _notificationSender;
    
    public MyTools(INotificationSender notificationSender)
    {
        _notificationSender = notificationSender;
    }
    
    [McpTool("reload_tools")]
    public async Task<JsonRpcMessage> ReloadTools(JsonRpcMessage request)
    {
        // Reload tools...
        
        // Notify WebSocket subscribers only
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ToolsChanged());
        
        return ToolResponse.Success(request.Id, new { reloaded = true });
    }
}
```

#### After (v1.7.0):
```csharp
// SSE-first notifications (WebSocket still works)
public class MyTools
{
    private readonly INotificationSender _notificationSender;
    
    public MyTools(INotificationSender notificationSender)
    {
        _notificationSender = _notificationSender;
    }
    
    [McpTool("reload_tools")]
    public async Task<JsonRpcMessage> ReloadTools(JsonRpcMessage request)
    {
        // Reload tools...
        
        // Notify ALL active SSE streams (automatic broadcast!)
        await _notificationSender.SendNotificationAsync(
            NotificationMessage.ToolsChanged());
        
        return ToolResponse.Success(request.Id, new { reloaded = true });
    }
}
```

**Client changes:**
```bash
# v1.6.x - WebSocket only
ws://localhost:5000/ws

# v1.7.0 - SSE (recommended)
curl -N http://localhost:5000/mcp \
  -H "MCP-Session-Id: <session-id>" \
  -H "Accept: text/event-stream"

# Receive notifications:
# id: 1
# event: message
# data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}
```

**Migration notes:**
- ✅ **No code changes** needed in tool implementations
- ✅ **WebSocket notifications** still work (deprecated)
- ✅ **SSE automatic** for clients using `GET /mcp`

---

### 5. Message Buffering & Last-Event-ID

#### Before (v1.6.x):
- No message buffering
- SSE reconnect = missed messages

#### After (v1.7.0):
```bash
# Client reconnects with Last-Event-ID
curl -N http://localhost:5000/mcp \
  -H "MCP-Session-Id: <session-id>" \
  -H "Last-Event-ID: 42" \
  -H "Accept: text/event-stream"

# Server replays buffered messages (43, 44, 45, ...)
# id: 43
# data: {"jsonrpc":"2.0","method":"notifications/tools/list_changed","params":{}}
#
# id: 44
# data: {"jsonrpc":"2.0","id":5,"result":{...}}
```

**Features:**
- ✅ **Automatic:** No code changes needed
- ✅ **Buffer size:** 100 messages per session
- ✅ **Overflow:** FIFO (oldest removed)

---

## 🧪 Testing Your Migration

### 1. Verify Backward Compatibility

**Your v1.6.x code should work unchanged:**

```bash
# Test legacy /rpc endpoint
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'

# Should return tools list (no session ID)
```

---

### 2. Test New /mcp Endpoint

```bash
# Initialize with /mcp
curl -X POST http://localhost:5000/mcp \
  -H "MCP-Protocol-Version: 2025-11-25" \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize"}' \
  -i

# Check for MCP-Session-Id in response headers
```

---

### 3. Test SSE Notifications

```bash
# Terminal 1: Open SSE stream
SESSION_ID="<your-session-id>"
curl -N http://localhost:5000/mcp \
  -H "MCP-Session-Id: $SESSION_ID" \
  -H "Accept: text/event-stream"

# Terminal 2: Trigger notification
curl -X POST http://localhost:5000/api/notify/tools

# Terminal 1 should receive notification
```

---

### 4. Run All Tests

```bash
dotnet test

# All 253 tests should pass
```

---

## 📦 NuGet Package Update

### Update package reference:

```xml
<!-- Before (v1.6.x) -->
<PackageReference Include="Mcp.Gateway.Tools" Version="1.6.0" />

<!-- After (v1.7.0) -->
<PackageReference Include="Mcp.Gateway.Tools" Version="1.7.0" />
```

### Or via CLI:

```bash
dotnet add package Mcp.Gateway.Tools --version 1.7.0
```

---

## ⚠️ Deprecation Warnings

### Deprecated (still functional):

```csharp
app.MapHttpRpcEndpoint("/rpc");  // Use /mcp POST instead
app.MapSseRpcEndpoint("/sse");   // Use /mcp GET instead

// WebSocket notifications
notificationService.AddSubscriber(webSocket);  // Use SSE instead
```

**Timeline:**
- v1.7.x: Deprecated (warnings in logs)
- v1.8.x: Deprecated (still functional)
- v2.0.0: Potentially removed (with migration period)

---

## 🐛 Known Issues

### None! 🎉

v1.7.0 is fully backward compatible with zero known breaking changes.

---

## 📚 Additional Resources

- **CHANGELOG.md** - Full v1.7.0 release notes
- **Examples/NotificationMcpServer/SESSION_MANAGEMENT.md** - Session management deep dive
- **`.internal/notes/v1.7.0/`** - Design documents
- **MCP 2025-11-25 Spec:** https://modelcontextprotocol.io/specification/2025-11-25/

---

## 💬 Need Help?

- **GitHub Issues:** https://github.com/eyjolfurgudnivatne/mcp.gateway/issues
- **GitHub Discussions:** https://github.com/eyjolfurgudnivatne/mcp.gateway/discussions
- **NuGet Package:** https://www.nuget.org/packages/Mcp.Gateway.Tools/

---

## 🎉 Summary

**Migration effort:** ⏱️ **5-15 minutes** (mostly testing!)

**Steps:**
1. ✅ Update NuGet package to v1.7.0
2. ✅ Add `app.UseProtocolVersionValidation()`
3. ✅ Add `app.MapStreamableHttpEndpoint("/mcp")`
4. ✅ (Optional) Update clients to use `/mcp` endpoint
5. ✅ Test and enjoy MCP 2025-11-25 compliance!

**No breaking changes. No forced migrations. Just better features when you're ready!** 💪

---

**Version:** v1.7.0  
**MCP Protocol:** 2025-11-25 (100% compliant)  
**License:** MIT © 2024–2025 ARKo AS – AHelse Development Team
