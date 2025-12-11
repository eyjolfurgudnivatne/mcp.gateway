# 🚨 Phase 0: Tool Capabilities - CRITICAL FIRST STEP

**Created:** 7. desember 2025  
**Branch:** `feat/ollama`  
**Priority:** 🔴 **BLOCKER** for Ollama integration  
**Estimated time:** 3-4 timer

---

## 🎯 Why Phase 0 is Critical

**Problem discovered:**
> Ollama, GitHub Copilot, and Claude Desktop do NOT support binary streaming tools!

**Current situation:**
- `tools/list` returns ALL tools (including `system_binary_streams_in`, etc.)
- Ollama function calling only supports JSON (not binary streams)
- GitHub Copilot likely only supports standard tools (not streaming)
- Clients see incompatible tools → confusion and errors

**Solution:**
> Filter tools based on transport capabilities BEFORE adding Ollama tools

---

## 📊 Transport Compatibility Matrix

| Client | Transport | Standard Tools | Text Streaming | Binary Streaming |
|--------|-----------|----------------|----------------|------------------|
| **GitHub Copilot** | stdio | ✅ | ❌ | ❌ |
| **Web UI** | HTTP | ✅ | ❌ | ❌ |
| **Ollama Agent** | HTTP | ✅ | ❌ | ❌ |
| **Claude Desktop** | SSE | ✅ | ✅ (maybe) | ❌ |
| **WebSocket Client** | WS | ✅ | ✅ | ✅ |

**Key insight:** Only WebSocket supports everything!

---

## 🏗️ Architecture

### New Enum: `ToolCapabilities`

```csharp
[Flags]
public enum ToolCapabilities
{
    Standard = 1,           // Works on all transports
    TextStreaming = 2,      // WebSocket or SSE
    BinaryStreaming = 4,    // WebSocket only
    RequiresWebSocket = 8   // Must use WebSocket
}
```

### Updated `McpToolAttribute`

```csharp
[McpTool("system_binary_streams_in",
    Title = "Binary Stream In",
    Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket
)]
public async Task StreamIn(ToolConnector connector) { }
```

### Auto-filtering in `ToolInvoker`

```csharp
// When client requests tools/list:
var transport = DetectTransport(context);  // "stdio", "http", "ws", "sse"
var tools = await _toolService.GetToolsForTransport(transport);

// stdio → Only Standard tools
// http → Only Standard tools
// sse → Standard + TextStreaming
// ws → All tools
```

---

## ✅ Implementation Checklist (Quick Reference)

**Full details in `implementation-plan.md`**

- [ ] **Step 0.1:** Add `ToolCapabilities` enum (30 min)
- [ ] **Step 0.2:** Update `McpToolAttribute` (15 min)
- [ ] **Step 0.3:** Update `ToolDefinition` record (15 min)
- [ ] **Step 0.4:** Add `GetToolsForTransport()` to `ToolService` (30 min)
- [ ] **Step 0.5:** Mark existing streaming tools (30 min)
- [ ] **Step 0.6:** Update `ToolInvoker` with auto-filtering (45 min)
- [ ] **Step 0.7:** Unit tests (1 time)
- [ ] **Step 0.8:** Integration tests (30 min)
- [ ] **Step 0.9:** Run all tests (15 min)

**Total:** ~4 timer

---

## 🎯 Success Criteria

### Before Phase 0:
```bash
# GitHub Copilot via stdio
tools/list → Returns ALL tools (including binary_streams_in) ❌

# User tries: @mcp_gcc use system_binary_streams_in
→ Error or confusion ❌
```

### After Phase 0:
```bash
# GitHub Copilot via stdio
tools/list → Returns ONLY standard tools (add_numbers, system_ping, etc.) ✅

# Ollama function calling
tools/list → Returns ONLY JSON-compatible tools ✅

# WebSocket client
tools/list → Returns ALL tools (including streaming) ✅
```

---

## 🧪 Testing Strategy

### Unit Tests:

```csharp
[Fact]
public async Task GetToolsForTransport_Stdio_ExcludesStreaming()
{
    var service = new ToolService();
    var tools = await service.GetToolsForTransport("stdio");
    
    // Should NOT contain binary streaming tools
    Assert.DoesNotContain(tools, t => 
        t.Name.Contains("stream", StringComparison.OrdinalIgnoreCase));
}

[Fact]
public async Task GetToolsForTransport_WebSocket_IncludesAll()
{
    var service = new ToolService();
    var tools = await service.GetToolsForTransport("ws");
    
    // Should contain binary streaming tools
    Assert.Contains(tools, t => t.Name == "system_binary_streams_in");
}
```

### Integration Tests:

```csharp
[Fact]
public async Task ToolsList_ViaHttpEndpoint_ExcludesStreaming()
{
    var request = new { jsonrpc = "2.0", method = "tools/list", id = 1 };
    var response = await _client.PostAsJsonAsync("/rpc", request);
    
    var json = await response.Content.ReadFromJsonAsync<JsonRpcMessage>();
    var tools = json.Result.GetProperty("tools").EnumerateArray();
    
    Assert.DoesNotContain(tools, t =>
        t.GetProperty("name").GetString().Contains("binary_stream"));
}
```

---

## 📝 Files to Modify

### Core Library (Mcp.Gateway.Tools):

| File | Changes | Effort |
|------|---------|--------|
| `ToolModels.cs` | Add `ToolCapabilities` enum, update `McpToolAttribute` | 30 min |
| `ToolService.cs` | Add `GetToolsForTransport()` method | 30 min |
| `ToolInvoker.cs` | Add auto-filtering in `tools/list` handler | 45 min |

### Example Server (Mcp.Gateway.Server):

| File | Changes | Effort |
|------|---------|--------|
| `Tools/Systems/BinaryStreams/*.cs` | Mark with `Capabilities` | 30 min |

### Tests (Mcp.Gateway.Tests):

| File | Changes | Effort |
|------|---------|--------|
| `ToolCapabilitiesTests.cs` (NEW) | Unit tests for filtering | 1 time |
| `Endpoints/Rpc/McpProtocolTests.cs` | Integration test updates | 30 min |

---

## 🔍 Design Decisions

### Why `[Flags]` enum?

Tools can require MULTIPLE capabilities:
```csharp
Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket
```

### Why auto-filter instead of manual?

**Auto-filtering:**
- ✅ Automatic - works for all future clients
- ✅ Transport-aware - adapts to client capabilities
- ✅ User-friendly - clients only see compatible tools

**Manual filtering (rejected):**
- ❌ Clients must know about capabilities
- ❌ More complex client code
- ❌ Error-prone

### Why detect transport in `ToolInvoker`?

**Centralized logic:**
- ✅ Single place for filtering rules
- ✅ Consistent across all transports
- ✅ Easy to maintain

---

## 🚀 After Phase 0: Ready for Ollama!

Once Phase 0 is complete:

✅ **GitHub Copilot** sees only compatible tools  
✅ **Ollama** sees only JSON-compatible tools  
✅ **Web UI** sees appropriate tools per transport  
✅ **Zero breaking changes** to existing functionality  
✅ **All tests passing**  

→ **Safe to add Ollama tools in Phase 1!** 🎉

---

## 🤔 FAQ

**Q: Will this break existing clients?**  
A: No! Filtering is transparent. WebSocket clients still see all tools.

**Q: What about tools without `Capabilities` set?**  
A: Default is `ToolCapabilities.Standard` (works everywhere).

**Q: Can we skip this step?**  
A: No! Without filtering, Ollama will try to call incompatible tools → errors.

**Q: Does this affect `tools/call`?**  
A: No, only `tools/list`. If client calls incompatible tool, it will fail gracefully.

---

## 📚 References

- **Full implementation plan:** `implementation-plan.md`
- **Ollama compatibility research:** `ollama-integration.md`
- **MCP Protocol spec:** `../../docs/MCP-Protocol.md`
- **Streaming protocol:** `../../docs/StreamingProtocol.md`

---

**Status:** 🟡 Ready to implement  
**Next step:** Start with Step 0.1 (Add ToolCapabilities enum)  
**Blocker for:** Phase 1 (Ollama tools)  
**Must complete before:** Adding any new tools

---

**Last Updated:** 7. desember 2025  
**Author:** ARKo AS - AHelse Development Team  
**Branch:** feat/ollama  
**Priority:** 🔴 CRITICAL
