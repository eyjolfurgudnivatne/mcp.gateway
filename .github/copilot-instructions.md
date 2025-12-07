# GitHub Copilot Instructions

## Development Workflow Preferences

### File Operations
- When generating code output, write directly to files unless explicitly asked to show in chat first
- For large changes (>100 lines), prefer creating new files over editing existing ones
- Split large implementations into multiple logical files when appropriate
- Follow existing project structure: separate files for different concerns

### Communication Style
- Keep explanations concise when working in momentum
- Focus on implementation over explanation unless debugging
- Use Norwegian when preferred (can switch between Norwegian and English)

### Code Style & Conventions
- Follow C# 14.0 / .NET 10 standards
- Match existing naming conventions and file organization patterns
- Maintain consistency with project structure (e.g., separate test files per feature)
- Minimal comments unless matching existing style or explaining complex logic

### Terminal & Tools
- Use PowerShell Core (7+) for all terminal operations
- Batch related commands when possible to reduce round-trips
- Prefer built-in file tools over terminal commands for file operations

### Testing Patterns
- Follow xUnit conventions used in the project
- One test class per feature/component
- Use descriptive test method names: `FeatureName_Scenario_ExpectedResult`
- Match the structure used in existing tests (e.g., `StreamsInTests.cs`, `StreamsDuplexTests.cs`)

### Date and Time Context
- **Current context:** User has a datetime MCP server registered globally
- When needing current date/time, you can use the datetime MCP tools available
- Timezone preference: `Europe/Oslo` (Norwegian time)
- Always use ISO 8601 format for timestamps in code and documentation
- When documenting: Use full date format (e.g., "4. desember 2025" in Norwegian, "December 4, 2025" in English)

## Project Structure

### MCP Gateway
This is a Model Context Protocol (MCP) Gateway implementation that supports:
- WebSocket-based streaming (text and binary)
- JSON-RPC 2.0 over HTTP and WebSocket
- stdio transport for local MCP clients (e.g., GitHub Copilot)
- Tool invocation with streaming support
- Binary streaming with duplex communication
- MCP Protocol version: **2025-06-18**

### Key Patterns
- **Tools**: Located in `Mcp.Gateway.Server/Tools/Systems/`
  - Separate subdirectories for different tool categories (Echo, Ping, Notification, TextStreams, BinaryStreams, etc.)
  - Tool names use underscore format: `system_ping`, `add_numbers` (not dots!)
  - All tools registered via `[McpTool]` attribute
- **Tests**: Located in `Mcp.Gateway.Tests/Endpoints/`
  - Organized by protocol (Rpc, Ws, stdio) and then by tool/feature
  - Use `McpGatewayFixture` for integration testing
  - Test coverage: HTTP, WebSocket, and stdio transports
- **Tool Models**: Core tool infrastructure in `Mcp.Gateway.Tools/`
  - `ToolModels.cs`: Core tool definitions (JSON-RPC messages)
  - `ToolModelsStream.cs`: Streaming-specific models (StreamMessage)
  - `ToolService.cs`: Tool registration and discovery
  - `ToolInvoker.cs`: Tool invocation and MCP protocol methods
  - `ToolConnector.cs`: Streaming support (Phase 2 write, Phase 3 read)

### MCP Protocol Implementation
- **Protocol Version:** 2025-06-18
- **Methods Implemented:**
  - `initialize` - Protocol handshake
  - `tools/list` - Tool discovery (with transport-aware filtering)
  - `tools/call` - Tool invocation
- **Transports:** HTTP (`/rpc`), WebSocket (`/ws`), SSE (`/sse`), stdio
- **Tool Naming:** `^[a-zA-Z0-9_-]{1,128}$` (enforced by validator)
- **Documentation:** See `docs/MCP-Protocol.md` and `docs/MCP-Protocol-Verification.md`

### Tool Capabilities (v1.2.0+)
- **Transport filtering:** Tools are filtered based on transport capabilities
  - `stdio/http`: Standard tools only
  - `sse`: Standard + TextStreaming tools
  - `ws`: All tools (Standard + TextStreaming + BinaryStreaming)
- **ToolCapabilities enum:** Standard, TextStreaming, BinaryStreaming, RequiresWebSocket
- **Implementation:** See `.internal/notes/v.1.2.0/phase-0-tool-capabilities.md`

### Repository Organization

#### `.github/` - GitHub Configuration
- `workflows/` - CI/CD pipelines (test.yml, release.yml)
- `copilot-instructions.md` - This file (Copilot context and conventions)

#### `.internal/` - Development Notes & Guides
**Visible in Git** - Share development philosophy and progress
- `guides/` - Process documentation
  - `GitHub-Actions-Testing.md` - CI/CD testing guide
  - `GitHub-Actions-Trusted-Publishing.md` - Automated NuGet publishing
  - `NuGet-Publishing-Guide.md` - Manual NuGet package publishing
  - `RELEASE_INSTRUCTIONS.md` - Release process checklist
  - `GitHub-Release-Automation.md` - Release workflow guide
- `notes/` - Development notes and decisions
  - `attributes.md` - Tool attribute design notes
  - `Auto-Generated-Tool-Names.md` - Auto-naming feature documentation
  - `ArrayPool-Implementation.md` - ArrayPool optimization (v1.0.1)
  - `HybridToolAPI-Plan.md` - Hybrid API design (deferred to v2.0)
  - `Performance-Optimization-Plan.md` - Performance roadmap
  - `Quick-Wins-Session-Summary.md` - Quick wins session notes
  - `v.1.2.0/` - Version-specific implementation notes
    - `README.md` - Overview of v1.2.0 changes
    - `implementation-plan.md` - Ollama integration plan
    - `phase-0-progress.md` - Tool capabilities implementation progress
    - `phase-0-tool-capabilities.md` - Design document
    - `ollama-integration.md` - Ollama provider design
    - `ollama-reverse-integration.md` - Alternative integration approach
- `releases/` - Release-specific documentation
  - `v1.0.1/` - Release notes and checklists
    - `RELEASE_CHECKLIST.md`
    - `v1.0.1-Release-Summary.md`
  - `v1.1.0/` - Release notes
    - `release-notes.md`
- `README.md` - Overview of internal documentation structure

### Important Notes
- GitHub Copilot integration verified and working
- All 70 tests passing (v1.2.0)
- Production-ready (v1.1.0 released, v1.2.0 in development)
- Breaking change from earlier versions: Tool names changed from dots to underscores
- **Phase 0 complete:** Transport-aware tool filtering implemented and tested
