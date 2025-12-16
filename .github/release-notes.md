# Release v1.6.0 - Pagination & Notifications

Cursor-based pagination and WebSocket notification infrastructure.

Features:
- Cursor-based pagination for tools/list, prompts/list, resources/list
- Optional cursor and pageSize parameters (defaults: null, 100)
- nextCursor in response when more results available
- Alphabetic sorting for consistent pagination
- Notification infrastructure (WebSocket-only)
- notifications/tools/changed, notifications/prompts/changed, notifications/resources/updated
- NotificationService with thread-safe subscriber management
- Notification capabilities in initialize response

Testing:
- 130 tests passing (100% success)
- 9 new pagination tests
- 8 new notification tests
- Zero regression from v1.5.0

Compatibility:
- Zero breaking changes
- All parameters optional
- Backward compatible with v1.5.0

Known Limitations:
- Notifications require WebSocket (HTTP/stdio must poll)
- Not full MCP 2025-11-25 compliance (deferred to v1.7.0)
- No session management yet (deferred to v1.7.0)

Next: v1.7.0 - Full MCP 2025-11-25 compliance (SSE, sessions, resumability)
