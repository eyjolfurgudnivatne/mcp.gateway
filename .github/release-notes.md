## [1.8.7] - 2026-01-11

**✨ Resource API Refinements & Ping Compliance**

This release aligns the `ping` implementation with MCP specifications and expands client capabilities for resource management.

### ⚠️ Breaking Changes
- **Client Resource Read Return Type**
  - `IMcpClient.ReadResourceAsync` now returns `ReadResourceResult` instead of `ResourceContent` (wrapped in Task). This change is required to support specification-compliant metadata and structure like `_meta`.

### Changed
- **Ping Renamed**
  - Moved `system/ping` to `ping` in both `Mcp.Gateway.Client` and `Mcp.Gateway.Tools` to strictly follow the MCP specification.

### Added
- **Client Unsubscribe**
  - Added `UnSubscribeResourceAsync` to `IMcpClient`, completing the client-side API for resource subscription management.
- **Resource Metadata**
  - Added `ReadResourceRequestParamsMeta` and `_meta` property support to align with MCP specification.

### Fixed
- **Resource Read Handler**
  - Updated `resources/read` to use strict `ReadResourceRequestParams` and `ReadResourceResult` types.
  - Fixed `ResourceContent` serialization to include specification-compliant metadata.
