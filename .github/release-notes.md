## [1.8.5] - 2026-01-09

**✨ Dynamic Resources**

Added support for programmatically registering and unregistering resources at runtime, enabling dynamic scenarios where resources are not known at compile time.

### Added
- **Dynamic Resource Registration**
  - `ToolService.RegisterResource(...)` allows registering resources programmatically.
  - `ToolService.UnregisterResource(...)` allows removing resources at runtime.
  - Support for metadata (Name, Description, MimeType) without attributes.
  - Seamless integration with existing `resources/list` and `resources/read` endpoints.
