## [1.8.4] - 2026-01-09

**✨ System Ping & Client Connectivity Check**

Added a built-in `system/ping` tool to the gateway and a corresponding `PingAsync` method to the client for connectivity verification.

### Added
- **Internal `system/ping` Tool**
  - The gateway now handles `system/ping` requests internally (returning an empty success response).
  - Useful for health checks and connection validation without invoking user tools.

- **`IMcpClient.PingAsync`**
  - Added `Task PingAsync(CancellationToken ct)` to `IMcpClient` and `McpClient`.
  - Sends a `system/ping` request to the server to verify the transport and protocol handling are operational.
