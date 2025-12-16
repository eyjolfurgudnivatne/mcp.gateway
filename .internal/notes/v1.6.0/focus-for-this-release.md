# v1.6.0 Release Focus - "Pagination & Notifications"

**Target Date:** Q4 2025  
**Theme:** Foundation for MCP Protocol 2025-11-25  
**Status:** Planning

---

## 🎯 Primary Goals

### 1. Cursor-based Pagination (HIGH PRIORITY)
**Why first?** Foundation for all list operations in 2025-11-25 spec.

**Scope:**
- Implement pagination infrastructure in `ToolService` and `ToolInvoker`
- Add `cursor` and `nextCursor` support to:
  - `tools/list` ✅ (most used)
  - `prompts/list` ✅ (new in v1.4.0)
  - `resources/list` ✅ (new in v1.5.0)
- Backward compatible: `cursor` parameter is optional
- Default page size: 100 items (configurable)

**Implementation:**
```csharp
// Request with pagination
{
  "method": "tools/list",
  "params": {
    "cursor": "eyJvZmZzZXQiOjEwMH0=" // Optional, base64 encoded
  }
}

// Response with nextCursor
{
  "result": {
    "tools": [...],
    "nextCursor": "eyJvZmZzZXQiOjIwMH0=" // Present if more results available
  }
}
```

**Benefits:**
- Handles large tool/prompt/resource sets (100+ items)
- Reduces initial load time
- Enables progressive loading in clients

---

### 2. Dynamic Updates & Notifications (MEDIUM PRIORITY)
**Why second?** Enables real-time updates for tools/prompts/resources.

**Scope:**
- Implement notification infrastructure
- Add support for:
  - `notifications/tools/changed` 🔔
  - `notifications/prompts/changed` 🔔
  - `notifications/resources/updated` 🔔 (for subscriptions)

**Implementation:**
```csharp
// Server sends notification when tools change
{
  "method": "notifications/tools/changed",
  "params": {}  // Triggers client to re-fetch tools/list
}
```

**Use cases:**
- Hot-reload of tools during development
- Dynamic tool registration/unregistration
- Resource file changes (file watchers)

---

### 3. Logging Infrastructure (LOW PRIORITY - if time permits)
**Why last?** Nice-to-have, not critical for core functionality.

**Scope:**
- `logging/setLevel` - Server configures which log levels to receive
- `notifications/message` - Client sends logs to server

**Benefits:**
- Better debugging of MCP clients
- Centralized logging for distributed systems

---

## 🚫 Explicitly OUT OF SCOPE for v1.6.0

### Defer to v2.0:
- **Completion** (`completion/complete`) - Complex, requires deep integration
- **Resource templates** with URI variables - Needs design work
- **Resource subscriptions** (`resources/subscribe`) - Depends on notification infrastructure (can be v1.7 if v1.6 goes well)
- **Tool lifecycle hooks** - Separate feature, deserves own release
- **Custom transport providers** - Major architectural change
- **Enhanced streaming** (compression, multiplexing) - Performance optimization

---

## 📋 Implementation Plan

### Phase 1: Pagination (Dag 1-2)
1. Design cursor format (base64 encoded JSON: `{"offset": 100}`)
2. Add `CursorHelper` utility class in `Mcp.Gateway.Tools`
3. Update `ToolService.GetFunctionsForTransport()` with pagination
4. Update `ToolInvoker.HandleFunctionsList()` to include `nextCursor`
5. **NEW:** Create `Examples/PaginationMcpServer` with 100+ mock tools/prompts/resources
6. **NEW:** Create `Examples/PaginationMcpServerTests` with full pagination test coverage

### Phase 2: Notifications (Dag 3-4)
1. Add `NotificationService` for managing subscriptions in `Mcp.Gateway.Tools`
2. Implement `IFunctionChangeNotifier` interface
3. Add WebSocket notification support (HTTP/stdio can poll)
4. Update `initialize` to include notification capabilities
5. **NEW:** Create `Examples/NotificationMcpServer` with hot-reload demo
6. **NEW:** Create `Examples/NotificationMcpServerTests` with notification tests

### Phase 3: (Optional) Logging (Dag 5)
1. Add `LoggingService` for receiving client logs in `Mcp.Gateway.Tools`
2. Implement `logging/setLevel` handler
3. Add `notifications/message` handler
4. Integrate with `ILogger<T>` in ASP.NET Core
5. **NEW:** Create `Examples/LoggingMcpServer` with client-to-server logging demo
6. **NEW:** Create `Examples/LoggingMcpServerTests` with logging tests

---

## 🏗️ New Example Servers (v1.6.0)

### Why Example Servers > DevTestServer?
✅ **Focused testing** - Each server tests ONE feature  
✅ **Clear examples** - Users see how to use each feature  
✅ **Better isolation** - Bugs don't affect other features  
✅ **Documentation** - Code IS the documentation  
✅ **Reusable patterns** - Copy-paste friendly for users  

### New Servers:

#### `Examples/PaginationMcpServer`
- 100+ mock tools (`tool_001`, `tool_002`, ..., `tool_100`)
- 50+ mock prompts
- 50+ mock resources
- Demonstrates pagination with different page sizes

#### `Examples/NotificationMcpServer`
- Tools that can be added/removed at runtime
- File watcher that triggers `resources/updated`
- WebSocket notification demo
- Hot-reload example

#### `Examples/LoggingMcpServer` (optional)
- Client-to-server log forwarding
- Log level filtering demo
- Integration with ASP.NET Core logging

### DevTestServer stays for:
- Internal testing (xUnit integration)
- Complex scenarios (multi-transport, streaming)
- Performance testing

---

## 🎯 Success Criteria

### Must Have:
- ✅ Cursor-based pagination for `tools/list`, `prompts/list`, `resources/list`
- ✅ Backward compatible (no breaking changes)
- ✅ All existing tests pass
- ✅ 10+ new pagination tests

### Nice to Have:
- ✅ Notification infrastructure for `tools/changed`, `prompts/changed`, `resources/updated`
- ✅ Hot-reload example in `DevTestServer`
- ✅ Logging support (`logging/setLevel`, `notifications/message`)

### Out of Scope:
- ❌ Resource subscriptions (defer to v1.7 or v2.0)
- ❌ Completion support (defer to v2.0)
- ❌ Tool lifecycle hooks (defer to v2.0)

---

## 🚀 Release Timeline

**Estimated Duration:** 5-7 dager (ikke uker! 😊)

| Phase | Timeline | Deliverable |
|-------|----------|-------------|
| Planning | Dag 0 | Dette dokumentet + API design |
| Phase 1 | Dag 1-2 | Pagination infrastructure |
| Phase 2 | Dag 3-4 | Notifications infrastructure |
| Phase 3 | Dag 5 (optional) | Logging support |
| Testing | Dag 6-7 | Full test coverage + docs |
| Release | Dag 7 | v1.6.0 shipped! 🎉 |

**Note:** Med eksisterende arkitektur (`ToolService` partials, `ToolInvoker` partials) og gode patterns på plass blir dette raskt å implementere!

---

## 💡 Why This Approach?

### Incremental Value:
- **v1.6.0:** Pagination + Notifications (foundation)
- **v1.7.0:** Resource subscriptions + Completion (build on foundation)
- **v2.0.0:** Full 2025-11-25 spec + Lifecycle hooks + Custom transports

### Risk Mitigation:
- Smaller releases = easier to test
- Pagination is low-risk, high-value
- Notifications can be rolled back if issues arise

### Community Feedback:
- Get feedback on pagination before doing subscriptions
- Let users tell us if they need completion or lifecycle hooks first

---

## 📝 Notes

- Keep all changes **backward compatible**
- Follow existing patterns (`ToolService` partials, `ToolInvoker` partials)
- Add examples to `DevTestServer` for testing
- Update MCP Protocol doc with 2025-11-25 changes

---

**Created:** 2025-12-16  
**Author:** Development Team  
**Status:** DRAFT - Ready for review

## 🧪 Testing Strategy

### New Tests:
- Pagination tests (empty results, single page, multiple pages, invalid cursor)
- Notification tests (WebSocket, HTTP polling)
- Integration tests (client hot-reload scenarios)
- Logging tests (setLevel, message forwarding)

### Test Count Target: **150+ tests** (currently 121)
- **PaginationMcpServerTests:** ~15 tests
- **NotificationMcpServerTests:** ~10 tests
- **LoggingMcpServerTests:** ~5 tests (optional)
- Existing tests: 121 tests (all passing)

### New Example Servers:
- `Examples/PaginationMcpServer` + `PaginationMcpServerTests`
- `Examples/NotificationMcpServer` + `NotificationMcpServerTests`
- `Examples/LoggingMcpServer` + `LoggingMcpServerTests` (optional)
