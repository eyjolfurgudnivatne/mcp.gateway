# MCP Gateway v1.5.0 – MCP Resources Support

## Summary

v1.5.0 introduces first-class **MCP Resources** support alongside existing tools and prompts, including:

- Attribute-based resource registration via `McpResourceAttribute`
- Resource models and response types in `Mcp.Gateway.Tools`
- A new example server `ResourceMcpServer` + tests
- Architecture improvements (ToolService and ToolInvoker split into partial classes)
- Full MCP protocol compliance with Tools, Prompts, and Resources

---

## Highlights

### New: MCP Resources

- Added `[McpResource]` attribute in `Mcp.Gateway.Tools`:
  - `Uri` – required, must follow `scheme://path` format (e.g., `file://logs/app.log`, `db://users/123`).
  - `Name` – optional, human-friendly name (falls back to humanized URI when omitted).
  - `Description` – optional, shown to MCP clients in `resources/list`.
  - `MimeType` – optional, specifies content type (`text/plain`, `application/json`, etc.).
  - Used to mark methods as **resources**, separate from `[McpTool]` and `[McpPrompt]`.
- Introduced dedicated resource response models in `Mcp.Gateway.Tools`:
  - `ResourceDefinition` – describes a resource:
    - `Uri` – resource identifier.
    - `Name` – human-readable name.
    - `Description` – resource description.
    - `MimeType` – content type.
  - `ResourceContent` – wraps resource content:
    - `Uri` – resource identifier.
    - `MimeType` – content type.
    - `Text` – text content (v1.5.0 supports text-based resources).
  - `ResourceListResponse` – response format for `resources/list`.
  - `ResourceReadResponse` – response format for `resources/read`.
  - These models match the MCP resource result shape and can be reused by all resource implementations.
  - MCP resource methods implemented:
    - `resources/list`
    - `resources/read`
    - `initialize` resources capability flag

> Note: Resources are **not** streamed in v1.5.0; they are returned as regular JSON-RPC responses with text content.
> Binary blob support and resource subscriptions are planned for v1.6+.

### New: Resource Example Server

- Added `Examples/ResourceMcpServer` showcasing resource usage:
  - `Resources/FileResource.cs` – file-based resources:
    - `file://logs/app.log` – application logs.
    - `file://config/settings.json` – application configuration.
  - `Resources/SystemResource.cs` – system metrics resources:
    - `system://status` – system health metrics.
    - `system://environment` – environment information.
  - `Resources/DataResource.cs` – database resources:
    - `db://users/example` – example user profile.
    - `db://stats/summary` – application statistics.
  - Demonstrates how resources and `JsonRpcMessage` integrate cleanly with existing infrastructure.

- Added `Examples/ResourceMcpServerTests`:
  - `Resources/ResourceListTests.cs` verifies that resources are correctly discovered and exposed via `resources/list`.
  - `Resources/ResourceReadTests.cs` verifies that `resources/read` returns the expected content structure.
  - `Resources/McpProtocolTests.cs` verifies that `initialize` includes resources capability and tests full workflow.

> The example server is intended as a reference implementation for MCP Resources, similar to how
> `CalculatorMcpServer` demonstrates tools and `PromptMcpServer` demonstrates prompts.

### Architecture Improvements

- **ToolService refactored into 6 partial classes** for better organization:
  - `ToolService.cs` (Core) – 97 lines
  - `ToolService.Scanning.cs` (253 lines) – Function scanning and registration
  - `ToolService.Functions.cs` (168 lines) – Function definitions (Tools & Prompts)
  - `ToolService.Invocation.cs` (46 lines) – Function invocation with DI
  - `ToolService.Resources.cs` (155 lines) – Resource-specific functionality
  - **Average per file**: 143 lines (down from 719!)

- **ToolInvoker refactored into 6 partial classes** for better maintainability:
  - `ToolInvoker.cs` (Core) – 42 lines
  - `ToolInvoker.Http.cs` (75 lines) – HTTP transport
  - `ToolInvoker.WebSocket.cs` (421 lines) – WebSocket transport
  - `ToolInvoker.Sse.cs` (99 lines) – SSE transport
  - `ToolInvoker.Protocol.cs` (531 lines) – MCP protocol handlers
  - `ToolInvoker.Resources.cs` (162 lines) – Resources support
  - **Average per file**: 222 lines (down from 1060!)

---

## Behaviour & Compatibility

- Resources are a **new** MCP surface area and do **not** change existing behaviour:
  - Tools (`[McpTool]`, `tools/list`, `tools/call`) remain unchanged.
  - Prompts (`[McpPrompt]`, `prompts/list`, `prompts/get`) remain unchanged.
  - Wire format for tools, prompts, and streaming is unchanged.
- Resource types live in `Mcp.Gateway.Tools` and reuse existing JSON-RPC infrastructure:
  - Resource methods still return `JsonRpcMessage` via `ToolResponse.Success(...)`.
  - `ResourceContent` and `ResourceDefinition` are regular record types serialized by the existing `JsonOptions`.
- `initialize` now includes a `resources` capability flag when the server has registered resources (mirroring how tools and prompts capabilities are surfaced). MCP clients can use this to detect resource support.

> v1.5.0 delivers a complete first version of MCP Resources: attribute, response models, auto-discovery, resources/list and resources/read wired into ToolInvoker, and initialize-capabilities when resources are present.

---

## Testing

- New example tests in `Examples/ResourceMcpServerTests`:
  - Validate that resource endpoints:
    - Accept `JsonRpcMessage` request models.
    - Produce `ResourceContent` objects with correct structure (`uri`, `mimeType`, `text`).
    - Handle errors correctly (resource not found, invalid URI, missing parameters).
  - Full MCP protocol workflow tests (initialize → resources/list → resources/read).
- All existing `Mcp.Gateway.Tests` and example tests continue to pass:
  - No regressions to tools, prompts, transports (HTTP/WS/SSE/stdio), or streaming behaviour.
- **Test summary**: 121/121 tests passing (100% success rate)
  - Mcp.Gateway.Tests: 70 tests
  - CalculatorMcpServerTests: 16 tests
  - DateTimeMcpServerTests: 4 tests
  - PromptMcpServerTests: 10 tests
  - **ResourceMcpServerTests**: **10 tests** (NEW)
  - OllamaIntegrationTests: 11 tests

---

## Upgrade Notes

- v1.5.0 is backward compatible with v1.4.0 and v1.3.0.
- No changes are required for existing tool or prompt implementations.
- To start using resources:
  1. Add a new class in your server with methods annotated by `[McpResource]`.
  2. Return `ToolResponse.Success(request.Id, ResourceContent)` with a `uri`, `mimeType`, and `text` content.
  3. Resources will be automatically discovered and exposed via `resources/list` and `resources/read`.
- Resource URIs must follow `scheme://path` format:
  - `file://logs/app.log`
  - `db://users/123`
  - `system://status`
  - `http://api.example.com/data`
