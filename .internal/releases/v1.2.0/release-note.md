# Mcp.Gateway.Tools v1.2.0 – Transport-aware tools & improved streaming

## Summary

- Transport-aware tool discovery via `ToolCapabilities`
- New example servers and matching test projects
- WebSocket streaming performance optimizations
- Documentation overhaul for product and library usage

---

## Highlights

### 🔧 New

- **Transport-aware tool capabilities**
  - Introduced `ToolCapabilities` enum: `Standard`, `TextStreaming`, `BinaryStreaming`, `RequiresWebSocket`.
  - Tools are now filtered per transport:
    - HTTP / stdio: `Standard` tools only
    - SSE: `Standard` + `TextStreaming` tools
    - WebSocket: all tools (including `BinaryStreaming` and `RequiresWebSocket`).
- **New example servers and tests**
  - `Examples/CalculatorMcpServer` + `CalculatorMcpServerTests`.
  - `Examples/DateTimeMcpServer` + `DateTimeMcpServerTests`.
  - `Examples/OllamaIntegration` + `OllamaIntegrationTests` for Ollama MCP integration scenarios.
  - `DevTestServer` used as internal host by `Mcp.Gateway.Tests` for end-to-end testing.

---

## Documentation

- Rewrote root `README.md` to focus on:
  - What MCP Gateway is and when to use it
  - Quick start for .NET 10
  - How to connect from GitHub Copilot (stdio) and Claude Desktop (HTTP/WebSocket/SSE)
- Rewrote `Mcp.Gateway.Tools/README.md` to cover:
  - `[McpTool]` usage and tool naming rules
  - `JsonRpcMessage` / `JsonRpcError` models
  - Streaming via `ToolConnector` and `ToolCapabilities`
  - Dependency injection patterns and example tools
- Updated `.internal/README.md` and internal notes so design decisions, performance work and release process are visible to contributors.
- Updated `CONTRIBUTING.md`:
  - Clarified that only `Mcp.Gateway.Tools` is the published NuGet package
  - Described project layout (`DevTestServer`, examples, tests)
  - Mentioned .NET 10 / C# 14.0 and test organisation.

---

## Performance

- **WebSocket streaming optimizations**
  - `ToolConnector` now uses `JsonSerializer.SerializeToUtf8Bytes` instead of `Serialize` + `Encoding.UTF8.GetBytes`, removing an intermediate string allocation per message.
  - WebSocket receive buffers are now rented from `ArrayPool<byte>.Shared` instead of `new byte[...]`:
    - Eliminates ~64 KB allocation per WebSocket connection.
    - Benchmarks show ~159× faster buffer allocation and ~99–100% reduction in GC pressure for streaming scenarios.
- **Performance planning and internal docs**
  - Added/updated:
    - `.internal/notes/Quick-Wins-Session-Summary.md`
    - `.internal/notes/Performance-Optimization-Plan.md`
    - `.internal/notes/ArrayPool-Implementation.md`
  - Parameter parsing cache and Hybrid Tool API have been analysed and explicitly deferred to a later version to avoid premature complexity.

---

## Fixes & Clarifications

- Clarified separation between product and development infrastructure:
  - Only `Mcp.Gateway.Tools` is intended as the public NuGet package.
  - `DevTestServer` and `Examples/*` are development / verification artifacts and not part of the NuGet surface.
- Health endpoint in `DevTestServer`:
  - Negotiates `text/plain` vs `application/json` based on the `Accept` header.
  - Always sends no-cache headers to avoid stale health information.

---

## Testing

- All existing tests passing across transports:
  - HTTP: `Mcp.Gateway.Tests/Endpoints/Http/*`
  - WebSocket: `Mcp.Gateway.Tests/Endpoints/Ws/*`
  - SSE: `Mcp.Gateway.Tests/Endpoints/Sse/*`
  - stdio: `Mcp.Gateway.Tests/Endpoints/Stdio/*`
- New example projects each have dedicated test projects:
  - `CalculatorMcpServerTests`
  - `DateTimeMcpServerTests`
  - `OllamaIntegrationTests`

---

## Upgrade Notes

- No breaking API changes compared to v1.1.0.
- Existing tools continue to work as before.
- New `ToolCapabilities` metadata is optional but recommended for tools that rely on streaming or WebSocket-only behaviour.
