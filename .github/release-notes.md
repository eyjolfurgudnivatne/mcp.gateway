## [1.8.6] - 2026-01-10

**🐛 WebSocket Ping Fix & Unified Pipeline**

Fixed an issue where `system/ping` was not handled correctly on WebSocket connections. This release unifies the invocation pipeline, ensuring WebSocket and HTTP transports share the same protocol setup routines.

### Fixed
- **WebSocket System Ping**
  - Resolved an issue where `system/ping` requests failed or were ignored over WebSocket connections.
  - WebSocket transport now correctly handles internal system tools.

### Changed
- **Unified Tool Invocation**
  - Refactored WebSocket handling to use the shared `ToolInvoker` pipeline.
  - Ensures consistent protocol behavior (ping, error handling, lifecycle hooks) across all transports.
