# Changelog

All notable changes to MCP Gateway will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for v2.0
- MCP Resources support (official MCP spec feature)
- Tool lifecycle hooks for monitoring
- Custom transport provider API
- Enhanced streaming (compression, flow control, multiplexing)

---

## [1.4.0] - 2025-12-16

### Added
- MCP Prompts support:
  - New `[McpPrompt]` attribute in `Mcp.Gateway.Tools` to mark prompt methods, separate from `[McpTool]`.
    - `Name` is optional and auto-generated from the method name when omitted (same snake_case logic as tools).
    - `Title` and `Description` are optional and surfaced to MCP clients.
  - New prompt models in `Mcp.Gateway.Tools`:
    - `PromptResponse` – wraps an MCP prompt result:
      - `name` – prompt name.
      - `description` – prompt description.
      - `messages` – list of prompt messages for the LLM to handle.
      - `arguments` – argument metadata used when filling the prompt.
    - `PromptMessage` – a single message with:
      - `role` (e.g. `system`, `user`, `assistant`)
      - `content` (prompt text).
  - Full MCP prompt protocol support:
    - `prompts/list` implemented and returns all discovered prompts with metadata and arguments.
    - `prompts/get` implemented and returns the expanded prompt `messages` for a given prompt and arguments.
  - `initialize` now includes a `prompts` capability flag when the server has registered prompts, mirroring how tools capabilities are surfaced.
- New example server `Examples/PromptMcpServer`:
  - Demonstrates prompts implemented as regular methods returning `JsonRpcMessage` via `ToolResponse.Success(...)`.
  - Example prompt `SantaReportPrompt` shows how to build `PromptResponse` with `name`, `description`, `messages` and `arguments` (including enum-like argument values).

### Behaviour & Compatibility
- Prompts are a new MCP surface area and do not change existing tool behaviour:
  - Tools (`[McpTool]`, `tools/list`, `tools/call`) and streaming semantics are unchanged.
- Prompt types reuse existing JSON-RPC infrastructure:
  - Prompt methods still return `JsonRpcMessage`; `PromptResponse` / `PromptMessage` are serialized using the existing `JsonOptions`.
  - Prompt roles are represented as strings on the wire (`"system"`, `"user"`, `"assistant"`), keeping compatibility with MCP/LLM clients.
- v1.4.0 is backward compatible with v1.3.0 and v1.2.0; no changes are required for existing tool implementations.

### Testing
- New tests in `Examples/PromptMcpServerTests`:
  - Verify that prompts are correctly discovered and exposed via `prompts/list` (name, description, arguments).
  - Verify that `prompts/get` returns the expected `PromptResponse` structure (name, description, messages, arguments).
- All existing `Mcp.Gateway.Tests` and example tests continue to pass with no regressions across tools, transports (HTTP/WebSocket/SSE/stdio), or streaming.

---

## [1.3.0] - 2025-12-14

### Added
- `TypedJsonRpc<T>` helper for strongly-typed tool implementations
  - Thin wrapper over `JsonRpcMessage` used as a convenience API for tool authors.
  - Exposes `Id`, `IdAsString`, `Method`, `Inner` (`JsonRpcMessage`) and `GetParams()` returning `T`.
- Optional JSON Schema auto-generation for `TypedJsonRpc<T>` tools
  - Activated only when:
    - The first parameter of the tool method is `TypedJsonRpc<TParams>`, and
    - `[McpTool]` has no `InputSchema` defined (null/whitespace).
  - Generates a minimal MCP-compatible schema for `TParams`:
    - Root: `{ "type": "object", "properties": { ... }, "required": [ ... ] }`.
    - `properties` from public instance properties on `TParams`.
    - `required` contains all non-nullable properties; nullable properties are optional.
  - Maps C# types to JSON Schema types:
    - `string` → `"string"`
    - `Guid` → `"string"` + `"format": "uuid"`
    - `DateTime` / `DateTimeOffset` → `"string"` + `"format": "date-time"`
    - `bool` → `"boolean"`
    - `int`, `long`, `short`, `byte` → `"integer"`
    - `float`, `double`, `decimal` → `"number"`
    - arrays / `IEnumerable<T>` (except `string`) → `"array"`
    - other types → `"object"`.
- Enum and description support in generated schemas
  - C# `enum` types are represented as string-based enums:
    - `enum Status { Active, Disabled }` → `{ "type": "string", "enum": ["Active", "Disabled"] }`.
  - `[Description("...")]` on record properties (with `[property: Description]`) is mapped to `"description"`.
  - `JsonPropertyName` attributes are respected for property names.

### Changed
- `ToolService` now records the first parameter type for each tool method in `ToolDetailArgumentType`.
  - Enables `ToolInvoker` to construct `TypedJsonRpc<T>` instances when a tool expects that type.
- `ToolInvoker` argument binding updated to support `TypedJsonRpc<T>`
  - When `ToolArgumentType.IsTypedJsonRpc` is true, the first argument is constructed via `Activator.CreateInstance(paramType, JsonRpcMessage)`.
  - Behaviour for existing tools taking `JsonRpcMessage` as first parameter is unchanged.

### Behaviour & Compatibility
- `InputSchema` remains the source of truth when provided on `[McpTool]`:
  - If `InputSchema` is set, it is used as-is; schema generation is skipped.
- For tools using `TypedJsonRpc<TParams>` and no `InputSchema`:
  - `tools/list` now returns an auto-generated `inputSchema` derived from `TParams`.
- No changes to MCP protocol behaviour or wire format:
  - `JsonRpcMessage` structure is unchanged.
  - `initialize`, `tools/list`, and `tools/call` semantics are unchanged.

### Testing
- `Examples/CalculatorMcpServer` and `CalculatorMcpServerTests` updated with TypedJsonRpc examples:
  - `add_numbers_typed` – uses `TypedJsonRpc<T>` with explicit `InputSchema` (behaves as before).
  - `add_numbers_typed_ii` – uses `TypedJsonRpc<T>` without `InputSchema`, exercising schema generation.
- New tests in `Examples/CalculatorMcpServerTests/Tools/ToolListTests.cs`:
  - `ToolsList_ReturnsAllTools` – validates explicit schema for `add_numbers_typed`.
  - `ToolsListII_ReturnsAllTools` – validates generated schema for `add_numbers_typed_ii`, including `description` and `required`.

---

## [1.2.0] - 2025-12-12

### Added
- **Transport-aware tool capabilities (v1.2.0)**
  - Introduced `ToolCapabilities` enum (`Standard`, `TextStreaming`, `BinaryStreaming`, `RequiresWebSocket`).
  - Tools are now filtered per transport:
    - HTTP / stdio: `Standard` tools only
    - SSE: `Standard` + `TextStreaming` tools
    - WebSocket: all tools (including `BinaryStreaming` and `RequiresWebSocket`).
- **New example servers and tests**
  - `Examples/CalculatorMcpServer` and `Examples/CalculatorMcpServerTests`.
  - `Examples/DateTimeMcpServer` and `Examples/DateTimeMcpServerTests`.
  - `Examples/OllamaIntegration` and `Examples/OllamaIntegrationTests` for Ollama MCP integration scenarios.
  - `DevTestServer` as an internal host used by `Mcp.Gateway.Tests` for end-to-end testing.

### Changed
- **Documentation overhaul**
  - Rewrote root `README.md` to focus on the MCP Gateway product, quick start, transports, and integration with GitHub Copilot / Claude.
  - Rewrote `Mcp.Gateway.Tools/README.md` to focus on the library API (tool attributes, `JsonRpcMessage`, streaming, DI, and examples).
  - Updated `.internal/README.md` and internal notes to be committed to Git, sharing design decisions, performance work, and release process with contributors.
  - Updated `CONTRIBUTING.md` to clarify project structure (core library vs `DevTestServer` and examples), .NET 10 / C# 14.0 usage, and test layout.
- **Health/diagnostics**
  - Improved `DevTestServer` health endpoint to negotiate `text/plain` vs `application/json` based on `Accept` header and to always send no-cache headers.

### Performance
- **WebSocket streaming optimizations**
  - `ToolConnector` now uses `JsonSerializer.SerializeToUtf8Bytes` instead of `Serialize` + `Encoding.UTF8.GetBytes`, removing an intermediate string allocation per message.
  - WebSocket receive buffers are now rented from `ArrayPool<byte>.Shared` instead of allocating new arrays:
    - Eliminates ~64 KB allocation per WebSocket connection.
    - Benchmarks show ~159x faster buffer allocation and ~99–100% reduction in GC pressure for streaming scenarios.
- **Performance planning and notes**
  - Added/updated internal performance docs: `Quick-Wins-Session-Summary.md`, `Performance-Optimization-Plan.md`, `ArrayPool-Implementation.md`.
  - Analysed parameter parsing cache and Hybrid Tool API and explicitly deferred them to later versions to avoid premature complexity.

### Fixed
- Clarified separation between product and development infrastructure:
  - Documented that only `Mcp.Gateway.Tools` is intended as a published NuGet package.
  - Marked `DevTestServer` and the example projects as development / verification artifacts, not part of the NuGet surface.

---

## [1.1.0] - 2025-12-05

### Added
- **Auto-generated tool names** - `[McpTool]` attribute now accepts optional `name` parameter
  - If `name` is null, it's auto-generated from method name (e.g., `AddNumbers` → `add_numbers`)
  - `ToolNameGenerator.ToSnakeCase()` - Converts method names to snake_case
  - `ToolNameGenerator.ToHumanizedTitle()` - Generates human-readable titles
  - Backward compatible - explicit names still work as before
  - Example: `Mcp.Gateway.Server/Tools/Examples/AutoNamedTools.cs`
- **GitHub Actions automation**
  - Automated testing on push/PR to main branch
  - Automated release workflow with version tagging
  - NuGet Trusted Publishing support (keyless authentication)
- **Documentation**
  - Auto-generated tool names guide (`docs/Auto-Generated-Tool-Names.md`)
  - GitHub Actions testing guide (`docs/GitHub-Actions-Testing.md`)
  - GitHub release automation guide (`docs/GitHub-Release-Automation.md`)
  - NuGet Trusted Publishing guide (`docs/NuGet-Trusted-Publishing.md`)
  - Client examples (`Mcp.Gateway.Tools/docs/examples/client-examples.md`)

### Changed
- `McpToolAttribute` constructor now accepts optional `name` parameter (nullable)
- Tool discovery logic updated to auto-generate names if not specified
- Updated all documentation with auto-naming examples

### Fixed
- GitHub Actions workflow: Updated `upload-artifact` from v3 to v4 (v3 deprecated)

### Testing
- 17 new unit tests for `ToolNameGenerator` (total: 62+ tests)
- All tests passing ✅

---

## [1.0.0] - 2025-12-04

### Initial Release 🎉

First production release of MCP Gateway - a library for building Model Context Protocol (MCP) servers in .NET 10.

#### Added

**Core Library (Mcp.Gateway.Tools)**
- Complete MCP Protocol 2025-06-18 implementation
- `ToolInvoker` - JSON-RPC and MCP protocol handler
- `ToolService` - Auto-discovery of tools via `[McpTool]` attribute
- `ToolConnector` - High-level API for streaming tools
- Tool name validation with MCP-compliant regex: `^[a-zA-Z0-9_-]{1,128}$`
- JSON Schema-based input validation
- Full dependency injection support

**Transport Protocols**
- HTTP RPC endpoint (`/rpc`) - Standard JSON-RPC over POST
- WebSocket RPC endpoint (`/ws`) - Full-duplex communication
- SSE endpoint (`/sse`) - Server-Sent Events for remote MCP clients
- stdio transport - Local integration (GitHub Copilot, etc.)

**Streaming Support**
- Binary streaming (write, read, duplex modes)
- Text streaming (JSON-based)
- ToolConnector API with `BinaryStreamHandle` and `TextStreamHandle`
- 24-byte binary header format (GUID + index)
- Stream lifecycle management (start, chunk, done, error)

**MCP Protocol Methods**
- `initialize` - Protocol handshake and capability negotiation
- `tools/list` - Tool discovery with JSON Schema
- `tools/call` - Tool invocation with MCP content format

**Client Integration**
- GitHub Copilot integration (stdio transport)
- Claude Desktop integration (SSE transport)
- Custom client support (HTTP/WebSocket)

**Example Servers**
- `Mcp.Gateway.Server` - Full-featured reference implementation
- `Mcp.Gateway.GCCServer` - Minimal GitHub Copilot example

**Example Tools**
- Calculator tools (`add_numbers`, `multiply_numbers`)
- System tools (`system_ping`, `system_echo`)
- Binary streaming tools (`system_binary_streams_in`, `system_binary_streams_out`, `system_binary_streams_duplex`)
- Secret generator tool

**Testing**
- 45+ comprehensive tests (100% passing)
- Full transport coverage (HTTP, WebSocket, SSE, stdio)
- Binary and text streaming tests
- Error handling and edge case tests
- GitHub Copilot integration tests

**Documentation**
- Comprehensive README.md with library focus
- MCP Protocol specification (docs/MCP-Protocol.md)
- Streaming Protocol v1.0 (docs/StreamingProtocol.md)
- JSON-RPC 2.0 reference (docs/JSON-RPC-2.0-spec.md)
- Tool creation guide (Mcp.Gateway.Tools/README.md)

#### Technical Details

**Architecture**
- Clean separation: Core library vs. Example servers
- Modular transport system
- Event-based streaming architecture
- Type-safe with C# 14.0 records

**Performance**
- Optimized for low-latency (<10ms)
- Efficient binary streaming
- Minimal memory allocations
- WebSocket connection pooling

**Security**
- Input validation via JSON Schema
- Type-safe parameter parsing
- Error sanitization
- Transport-level security (HTTPS/WSS support)

---

**Project Type**: Library (.NET 10)  
**Target Frameworks**: net10.0  
**Language**: C# 14.0  
**License**: MIT  
**Protocol Version**: MCP 2025-06-18
