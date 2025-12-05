# Changelog

All notable changes to MCP Gateway will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for v1.1
- NuGet package release for Mcp.Gateway.Tools
- Additional example tools
- Performance optimizations
- Enhanced documentation

### Planned for v2.0
- MCP Resources support (official MCP spec feature)
- MCP Prompts support (official MCP spec feature)
- Tool lifecycle hooks for monitoring
- Custom transport provider API
- Enhanced streaming (compression, flow control, multiplexing)

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
