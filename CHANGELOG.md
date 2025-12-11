# Changelog

All notable changes to MCP Gateway will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for v2.0
- MCP Resources support (official MCP spec feature)
- MCP Prompts support (official MCP spec feature)
- Hybrid Tool API - Simplified tool authoring with auto-generated schemas
- Tool lifecycle hooks for monitoring
- Custom transport provider API
- Enhanced streaming (compression, flow control, multiplexing)

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
