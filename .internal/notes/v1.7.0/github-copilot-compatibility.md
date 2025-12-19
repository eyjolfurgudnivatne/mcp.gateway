# 🔌 GitHub Copilot MCP Compatibility Guide

**Created:** 19. desember 2025  
**Status:** Important for v1.7.0 users  
**Issue:** `/mcp` endpoint requires session management, not compatible with legacy SSE clients

---

## 🎯 Problem Statement

**MCP Gateway v1.7.0** introduced the unified `/mcp` endpoint (MCP 2025-11-25 Streamable HTTP):
- ✅ POST /mcp → Create session, send requests
- ✅ GET /mcp → Open SSE stream (requires session ID)
- ✅ DELETE /mcp → Terminate session

**GitHub Copilot (VS Code, December 2025):**
- ✅ Supports `"type": "sse"` in `.mcp.json`
- ❌ But this uses **LEGACY SSE** (no session management)
- ❌ Cannot connect to `/mcp` endpoint (missing MCP-Session-Id)

---

## 📊 Transport Matrix

| Transport Type | `.mcp.json` Config | MCP Protocol Version | Session Management | Gateway Support |
|----------------|-------------------|----------------------|-------------------|----------------|
| **stdio** | `"type": "stdio"` | All versions | Not applicable | ✅ Supported |
| **Legacy SSE** | `"type": "sse"` | Pre-2025-11-25 | No | ✅ `/sse` endpoint |
| **Streamable HTTP** | `"type": "http"` | 2025-11-25 | Yes (POST → GET) | ✅ `/mcp` endpoint |

---

## 🔧 Solutions

### Option 1: Use Legacy `/sse` Endpoint (RECOMMENDED for now)

**Why:** Works with current GitHub Copilot clients

**Configuration:**
```json
{
  "mcpServers": {
    "my-server": {
      "type": "sse",
      "url": "http://localhost:5239/sse",  // Legacy endpoint
      "headers": {}
    }
  }
}
```

**Pros:**
- ✅ Works immediately with GitHub Copilot
- ✅ No session management needed
- ✅ Backward compatible

**Cons:**
- ⚠️ Uses deprecated endpoint
- ⚠️ No Last-Event-ID resumption
- ⚠️ No message buffering

---

### Option 2: Use Streamable HTTP (when supported)

**Status:** Requires client support for session management

**Configuration:**
```json
{
  "mcpServers": {
    "my-server": {
      "type": "http",
      "url": "http://localhost:5239/mcp",  // MCP 2025-11-25 endpoint
      "headers": {
        "MCP-Protocol-Version": "2025-11-25"
      }
    }
  }
}
```

**Client Requirements:**
1. Send POST /mcp with `initialize` request
2. Receive `MCP-Session-Id` header in response
3. Send GET /mcp with `MCP-Session-Id` header to open SSE stream

**Supported By:**
- ✅ Microsoft Copilot Studio (2025+)
- ✅ Microsoft Learn MCP Server (streamable http)
- ❓ GitHub Copilot VS Code (unknown timeline)

---

## 📝 GitHub Copilot Roadmap

**Known from Microsoft Learn docs (December 2025):**

### Visual Studio 2026:
- ✅ MCP Authentication Management
- ✅ MCP Server Instructions
- ✅ MCP Elicitations and Sampling
- ✅ MCP Server Management UI

### Transport Support:
- ✅ stdio (local servers)
- ✅ sse (legacy, pre-2025-11-25)
- ✅ streamable HTTP (MCP 2025-11-25) **- but implementation details unclear**

**Quote from Microsoft Learn:**
> "The options for MCP server transport are local standard input/output (stdio), 
> server-sent events (sse), and streamable HTTP (http)."

**But:** No clear documentation on when `"type": "http"` with session management will be fully supported.

---

## 🎯 Recommendations

### For MCP Gateway Server Developers:

**Keep ALL endpoints active (v1.8.0+):**
```csharp
// MCP 2025-11-25 Streamable HTTP (recommended for new clients)
app.UseProtocolVersionValidation();
app.MapStreamableHttpEndpoint("/mcp");

// Legacy endpoints (deprecated but functional)
app.MapHttpRpcEndpoint("/rpc");  // HTTP POST only
app.MapWsRpcEndpoint("/ws");     // WebSocket (keep for binary streaming)
app.MapSseRpcEndpoint("/sse");   // SSE only (keep for legacy clients!)
```

**Why:**
- ✅ Backward compatibility with existing clients
- ✅ Gradual migration path
- ✅ Zero breaking changes

---

### For Client Developers (GitHub Copilot users):

**Current (December 2025):**
```json
{
  "mcpServers": {
    "my-server": {
      "type": "sse",
      "url": "http://localhost:5239/sse",  // Use /sse for now
      "headers": {}
    }
  }
}
```

**Future (when session management supported):**
```json
{
  "mcpServers": {
    "my-server": {
      "type": "http",
      "url": "http://localhost:5239/mcp",  // Upgrade to /mcp
      "headers": {
        "MCP-Protocol-Version": "2025-11-25"
      }
    }
  }
}
```

---

## ⚠️ Common Pitfall

**ERROR:** Using `/mcp` endpoint with `"type": "sse"`:

```json
// ❌ THIS WILL NOT WORK!
{
  "mcpServers": {
    "my-server": {
      "type": "sse",
      "url": "http://localhost:5239/mcp",  // Wrong! /mcp requires session
      "headers": {}
    }
  }
}
```

**Symptoms:**
```
warn: Missing MCP-Protocol-Version header on /mcp, assuming 2025-03-26
info: Created new session: 10993b8c365c44e9bb77dcff6a4edc39
info: Registered SSE stream for session: 10993b8c365c44e9bb77dcff6a4edc39
info: SSE stream closed for session: 10993b8c365c44e9bb77dcff6a4edc39
info: Terminated session: 10993b8c365c44e9bb77dcff6a4edc39
```

**Why it fails:**
1. Client sends GET /mcp (no POST initialize first)
2. Server creates session but expects session ID in next request
3. Client doesn't send session ID
4. Connection closes immediately

---

## 📚 References

### Microsoft Documentation:
- [Visual Studio MCP Support](https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers)
- [Copilot Studio MCP Transport](https://learn.microsoft.com/en-us/microsoft-copilot-studio/mcp-add-existing-server-to-agent)
- [Azure MCP Server](https://learn.microsoft.com/en-us/azure/developer/azure-mcp-server/)

### MCP Specification:
- [MCP 2025-11-25 Streamable HTTP](https://modelcontextprotocol.io/specification/2025-11-25/basic/transports#streamable-http)
- [MCP Transports Overview](https://modelcontextprotocol.io/docs/concepts/transports)

---

## 🚀 When to Migrate

**Monitor GitHub Copilot releases for:**
- Full Streamable HTTP (MCP 2025-11-25) support
- Session management implementation
- Explicit `"type": "http"` support in `.mcp.json`

**Until then:**
- ✅ Keep `/sse` endpoint for legacy clients
- ✅ Use `/mcp` for modern clients that support session management
- ✅ Document both approaches in README

---

## 💡 Summary

**Current State (December 2025):**
- GitHub Copilot: Use `/sse` endpoint with `"type": "sse"`
- Modern Clients: Use `/mcp` endpoint with `"type": "http"` (if supported)
- MCP Gateway: Support both endpoints (zero breaking changes!)

**Future State (when GitHub Copilot adds full support):**
- All clients: Use `/mcp` endpoint with session management
- Legacy endpoints: Can be removed in v2.0 (after long deprecation)

---

**Created by:** ARKo AS - AHelse Development Team  
**Last Updated:** 19. desember 2025, kl. 02:15  
**Version:** 1.0 (Initial Compatibility Guide)
