## [1.8.3] - 2026-01-03

**✨ Non-Generic CallToolAsync**

Added a non-generic `CallToolAsync` overload to `IMcpClient` for scenarios where the tool's return value is not needed.

### Added
- **Non-Generic CallToolAsync**
  - `Task CallToolAsync(string toolName, object? arguments, CancellationToken ct = default)`
  - Useful for fire-and-forget scenarios or tools that don't return data.
