# Mcp.Gateway.Tools v1.6.0 – Pagination & Notifications

## Summary

- Cursor-based pagination for `tools/list`, `prompts/list`, and `resources/list`
- Notification infrastructure for dynamic updates (WebSocket-only)
- 130 tests passing (100% success rate)
- Full backward compatibility with v1.5.0

---

## Highlights

### 🔧 New

#### Cursor-based Pagination
- **Universal pagination support** across all MCP list operations:
  - `tools/list` - Paginate large tool sets
  - `prompts/list` - Paginate prompt collections
  - `resources/list` - Paginate resource catalogs
- **Simple cursor format**: Base64-encoded JSON (`{"offset": 100}`)
- **Optional parameters**:
  - `cursor` - Resume from specific position (optional, defaults to start)
  - `pageSize` - Items per page (optional, defaults to 100)
- **Response includes**:
  - `nextCursor` - Present when more results available
  - Alphabetically sorted results for consistent pagination
- **New utility class**: `CursorHelper` in `Mcp.Gateway.Tools/Pagination/`
  - `Paginate<T>()` - Apply pagination to any enumerable
  - `PaginatedResult<T>` - Consistent pagination response format

**Example request with pagination:**
```json
{
  "jsonrpc": "2.0",
  "method": "tools/list",
  "params": {
    "cursor": "eyJvZmZzZXQiOjEwMH0=",
    "pageSize": 50
  }
}
```

**Example response with nextCursor:**
```json
{
  "jsonrpc": "2.0",
  "result": {
    "tools": [ /* 50 tools */ ],
    "nextCursor": "eyJvZmZzZXQiOjE1MH0="
  }
}
```

#### Notification Infrastructure
- **WebSocket-based notifications** (v1.6.0):
  - Server can push updates to connected WebSocket clients
  - Clients receive real-time notifications when tools/prompts/resources change
- **Three notification types**:
  - `notifications/tools/changed` - Tools added, removed, or modified
  - `notifications/prompts/changed` - Prompts updated
  - `notifications/resources/updated` - Resources changed (optional URI parameter)
- **New service**: `NotificationService` in `Mcp.Gateway.Tools/Notifications/`
  - Thread-safe subscriber management
  - Automatic cleanup of closed connections
  - Broadcasts to all active WebSocket subscribers
- **New interface**: `INotificationSender`
  - Registered automatically via `AddToolsService()`
  - Available for dependency injection in custom tools
- **Updated `initialize` response**:
  - Includes `notifications` capability when NotificationService is configured
  - Capability filtering based on registered functions (only shows capabilities for types that exist)

**Example notification (server → client):**
```json
{
  "jsonrpc": "2.0",
  "method": "notifications/tools/changed",
  "params": {}
}
```

**Capability in `initialize` response:**
```json
{
  "capabilities": {
    "tools": {},
    "prompts": {},
    "resources": {},
    "notifications": {
      "tools": {},
      "prompts": {},
      "resources": {}
    }
  }
}
```

#### New Example Servers
- **PaginationMcpServer**:
  - 120 mock tools (`mock_tool_001` to `mock_tool_120`)
  - 20 mock prompts
  - 20 mock resources
  - Demonstrates pagination with various page sizes
- **NotificationMcpServer**:
  - 3 static tools (ping, echo, get_time)
  - API endpoints for manually triggering notifications:
    - `POST /api/notify/tools` - Send tools/changed notification
    - `POST /api/notify/prompts` - Send prompts/changed notification
    - `POST /api/notify/resources` - Send resources/updated notification
  - Demonstrates notification infrastructure

---

## Documentation

- Updated root `README.md`:
  - Added pagination section with examples
  - Added notification section with WebSocket-only notice
  - Updated test count to 130 tests
- Updated `Mcp.Gateway.Tools/README.md`:
  - Pagination usage examples
  - Notification infrastructure guide
  - `INotificationSender` DI pattern
- Updated `CHANGELOG.md`:
  - Full v1.6.0 changelog with breaking changes section
  - Planned v1.7.0 features (full MCP 2025-11-25 compliance)

---

## Performance

- **Pagination overhead**: Minimal (< 1ms for cursor encoding/decoding)
- **Alphabetic sorting**: Applied once during function discovery, cached thereafter
- **Notification sending**: Lock-free, parallel broadcast to all subscribers
- **Memory**: No additional allocations for empty pages or single-page results

---

## Known Limitations

### Notifications (v1.6.0)
- **WebSocket-only**: HTTP and stdio clients cannot receive push notifications
  - HTTP/stdio clients must poll `tools/list` to detect changes
  - SSE-based notifications planned for v1.7.0
- **No session management**: `MCP-Session-Id` header not supported
  - Planned for v1.7.0 (full MCP 2025-11-25 compliance)
- **No resumability**: SSE event IDs not implemented
  - Planned for v1.7.0

### MCP Protocol 2025-11-25 Compliance
- **Streamable HTTP transport**: Not implemented in v1.6.0
  - We use WebSocket for notifications instead of SSE
  - Full compliance planned for v1.7.0
- **Protocol version header**: `MCP-Protocol-Version` not validated
  - Planned for v1.7.0

---

## Testing

- **130 tests (100% passing)**
  - Mcp.Gateway.Tests: 70 tests
  - CalculatorMcpServerTests: 16 tests
  - DateTimeMcpServerTests: 4 tests
  - PromptMcpServerTests: 10 tests
  - ResourceMcpServerTests: 11 tests
  - **PaginationMcpServerTests: 9 tests** (NEW)
  - **NotificationMcpServerTests: 8 tests** (NEW)
  - OllamaIntegrationTests: 2 tests

### New Test Coverage
**Pagination tests (9 tests):**
- Default pagination (100 items per page)
- Custom page sizes (10, 50, 200 items)
- Cursor-based navigation (first page, second page, last page)
- Invalid cursor handling (graceful fallback)
- Edge cases (empty results, single page, exact boundary)

**Notification tests (8 tests):**
- Notification capabilities in `initialize` response
- Capability filtering (only shows capabilities for registered types)
- Notification sending (tools, prompts, resources)
- API endpoints for triggering notifications
- Multiple concurrent notifications

---

## Upgrade Notes

### From v1.5.0
- **No breaking changes**
- **Pagination is optional**:
  - Existing `tools/list` / `prompts/list` / `resources/list` calls work unchanged
  - Add `cursor` / `pageSize` parameters to enable pagination
- **Notifications are opt-in**:
  - Automatically registered via `AddToolsService()`
  - Only included in `initialize` if functions are registered
  - WebSocket clients receive notifications; HTTP/stdio clients must poll

### Backward Compatibility
✅ All v1.5.0 code continues to work  
✅ Pagination parameters are optional  
✅ Notification capabilities are optional  
✅ All existing tests pass (no regression)

---

## Roadmap

### v1.7.0 (Q1 2026) - Full MCP 2025-11-25 Compliance
- **Streamable HTTP transport** (SSE-based notifications)
- **Session Management** (`MCP-Session-Id` header)
- **Protocol Version Header** (`MCP-Protocol-Version` validation)
- **Resumability** (SSE event IDs, `Last-Event-ID`)
- **SSE-based notifications** (migrate from WebSocket)

### v2.0.0 (Q2 2026) - Advanced Features
- **Resource subscriptions** (`resources/subscribe`, `resources/unsubscribe`)
- **Resource templates** with URI variables
- **Completion support** (`completion/complete`)
- **Logging infrastructure** (`logging/setLevel`, `notifications/message`)
- **Tool lifecycle hooks**
- **Custom transport provider API**

---

**Release Date:** 16. desember 2025  
**Protocol Version:** MCP 2025-06-18 (partial 2025-11-25 features)  
**Target Framework:** .NET 10  
**License:** MIT
