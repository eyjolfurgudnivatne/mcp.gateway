# 🚀 Ollama Integration - Implementation Plan (v1.2.0)

**Forfatter:** ARKo AS - AHelse Development Team  
**Dato:** 7. desember 2025  
**Branch:** `feat/ollama`  
**Target Release:** v1.2.0  
**Status:** 🟡 Planning → Implementation

---

## 📋 Innholdsfortegnelse

1. [Overview](#overview)
2. [Architecture Decision](#architecture-decision)
3. [Implementation Phases](#implementation-phases)
4. [File Structure](#file-structure)
5. [Implementation Checklist](#implementation-checklist)
6. [Testing Strategy](#testing-strategy)
7. [Documentation Updates](#documentation-updates)
8. [Release Plan](#release-plan)

---

## 🎯 Overview

**Goal:** Integrere Ollama med MCP Gateway i to retninger:
1. **Pattern 1:** User → MCP Gateway → Ollama (Chat interface)
2. **Pattern 2:** User → Ollama → MCP Gateway → Tools (Autonomous agent)

**Philosophy:**
- **Transport layer** (HTTP/WS/SSE/stdio) → Uendret i `ToolExtensions.cs`
- **Integration layer** (Ollama) → Egen namespace `Mcp.Gateway.Ollama`
- **Multi-client support** → Web UI, GitHub Copilot, Ollama agent kan bruke samme tools

---

## 🏗️ Architecture Decision

### Approved Design: Alternativ 2+3 (Hybrid)

**Server-side (Pattern 1: MCP → Ollama):**
```
Mcp.Gateway.Server/Tools/Ollama/
├── OllamaTools.cs              (Tools som kaller Ollama API)
│   ├── ollama_chat              (Basic chat completion)
│   ├── ollama_generate          (Text generation)
│   ├── ollama_list_models       (Model discovery)
│   └── ollama_embeddings        (Embeddings generation)
```

**Client-side (Pattern 2: Ollama → MCP):**
```
Mcp.Gateway.Ollama/             (Nytt prosjekt - client library)
├── OllamaMcpAdapter.cs         (Main adapter class)
├── OllamaExtensions.cs         (ASP.NET Core extensions)
├── OllamaFunctionConverter.cs  (MCP → Ollama function format)
└── Models/
    ├── OllamaAdapterOptions.cs
    ├── OllamaFunction.cs
    └── OllamaToolCall.cs
```

### Transport Philosophy

| Layer | Responsibility | Location |
|-------|----------------|----------|
| **Transport** | HTTP/WS/SSE/stdio | `ToolExtensions.cs` (uendret) |
| **Integration** | Ollama-specific logic | `Mcp.Gateway.Ollama` (nytt) |
| **Tools** | Business logic | `Mcp.Gateway.Server/Tools/Ollama` |

**Rationale:** Clean separation of concerns, konsistent abstraction levels.

---

## 📅 Implementation Phases

### Phase 0: Tool Capabilities & Filtering (v1.2.0) 🎯 **CRITICAL - DO FIRST!**

**Timeline:** 3-4 timer  
**Goal:** Implement tool capability filtering før Ollama-integrasjon

**Rationale:** Ollama, GitHub Copilot, og Claude Desktop støtter ikke alle tool-typer (f.eks. binary streaming). Vi må filtrere tools basert på transport-capabilities før vi legger til nye tools.

#### Deliverables:
- [ ] **Add `ToolCapabilities` enum** (`Mcp.Gateway.Tools/ToolModels.cs`)
  ```csharp
  /// <summary>
  /// Defines capabilities required by a tool.
  /// </summary>
  [Flags]
  public enum ToolCapabilities
  {
      /// <summary>Standard JSON-RPC tool (works on all transports)</summary>
      Standard = 1,
      
      /// <summary>Requires text streaming support (WebSocket or SSE)</summary>
      TextStreaming = 2,
      
      /// <summary>Requires binary streaming support (WebSocket only)</summary>
      BinaryStreaming = 4,
      
      /// <summary>Must use WebSocket transport</summary>
      RequiresWebSocket = 8
  }
  ```

- [ ] **Update `McpToolAttribute`** (`Mcp.Gateway.Tools/ToolModels.cs`)
  ```csharp
  /// <summary>
  /// Capabilities required by this tool. Default: Standard (works on all transports)
  /// </summary>
  public ToolCapabilities Capabilities { get; init; } = ToolCapabilities.Standard;
  ```

- [ ] **Update ToolDefinition record** (`Mcp.Gateway.Tools/ToolModels.cs`)
  ```csharp
  public sealed record ToolDefinition(
      string Name,
      string Description,
      string InputSchema,
      ToolCapabilities Capabilities = ToolCapabilities.Standard  // NEW!
  );
  ```

- [ ] **Update `ToolService` discovery** (`Mcp.Gateway.Tools/ToolService.cs`)
  - [ ] Oppdatere `ScanForTools()` for å hente ut `Capabilities` fra attributt
  - [ ] Legge til ny metode `GetToolsForTransport(string transport)`:
    ```csharp
    /// <summary>
    /// Gets tools filtered by transport capabilities.
    /// </summary>
    public Task<List<ToolDefinition>> GetToolsForTransport(string transport)
    {
        var allTools = GetTools(); // Existing method
        
        var allowedCapabilities = transport switch
        {
            "stdio" => ToolCapabilities.Standard,
            "http" => ToolCapabilities.Standard,
            "sse" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming,
            "ws" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming | ToolCapabilities.BinaryStreaming,
            _ => ToolCapabilities.Standard
        };
        
        var filtered = allTools.Where(tool =>
            (tool.Capabilities & allowedCapabilities) != 0 ||
            tool.Capabilities == ToolCapabilities.Standard
        ).ToList();
        
        return Task.FromResult(filtered);
    }
    ```
  
- [ ] **Mark existing streaming tools** (mark with capabilities)
  - [ ] `Mcp.Gateway.Server/Tools/Systems/BinaryStreams/*`
  - [ ] Oppdatere `McpToolAttribute`:
    ```csharp
    [McpTool("system_binary_streams_in",
        Title = "Binary Stream In",
        Description = "...",
        Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket
    )]
    ```
  - [ ] Gjenta for:
    - [ ] `StreamsOut.cs`
    - [ ] `StreamsDuplex.cs`
    - [ ] Eventuelle andre streaming-verktøy

- [ ] **Update `ToolInvoker`** (`Mcp.Gateway.Tools/ToolInvoker.cs`)
  - [ ] Legge til hjelpemetode for transportdeteksjon:
    ```csharp
    private static string DetectTransport(HttpContext? context)
    {
        if (context == null) return "stdio";
        if (context.WebSockets.IsWebSocketRequest) return "ws";
        if (context.Request.Headers["Accept"].Contains("text/event-stream")) return "sse";
        return "http";
    }
    ```
  - [ ] Oppdatere håndtering av `tools/list` i hver transportmetode:
    ```csharp
    // I InvokeHttpRpcAsync, InvokeWsRpcAsync, osv.
    if (request.Method == "tools/list")
    {
        var transport = DetectTransport(context);
        var tools = await _toolService.GetToolsForTransport(transport);
        return ToolResponse.Success(request.Id, new { tools });
    }
    ```

- [ ] **Unit tests**
  - [ ] Create `Mcp.Gateway.Tests/ToolCapabilitiesTests.cs`
  - [ ] Test filtering per transport
    ```csharp
    [Fact]
    public async Task GetToolsForTransport_Stdio_ExcludesStreamingTools()
    {
        // Arrange
        var service = new ToolService();
        
        // Act
        var tools = await service.GetToolsForTransport("stdio");
        
        // Assert
        Assert.DoesNotContain(tools, t => 
            t.Name.Contains("stream", StringComparison.OrdinalIgnoreCase));
    }
    
    [Fact]
    public async Task GetToolsForTransport_WebSocket_IncludesAllTools()
    {
        // Arrange
        var service = new ToolService();
        
        // Act
        var tools = await service.GetToolsForTransport("ws");
        
        // Assert
        Assert.Contains(tools, t => t.Name == "system_binary_streams_in");
    }
    ```

#### Success criteria:
- ✅ `tools/list` via stdio excludes binary streaming tools
- ✅ `tools/list` via WebSocket includes all tools
- ✅ Existing tools work unchanged
- ✅ All existing tests still pass

**Transport Filtering Matrix:**

| Transport | Standard | TextStreaming | BinaryStreaming |
|-----------|----------|---------------|-----------------|
| stdio     | ✅       | ❌            | ❌              |
| http      | ✅       | ❌            | ❌              |
| sse       | ✅       | ✅            | ❌              |
| ws        | ✅       | ✅            | ✅              |

---

### Phase 1: Basic Integration (v1.2.0) 🎯 CURRENT

**Timeline:** 1-2 dager  
**Goal:** Proof-of-concept with basic chat/generate tools

#### Deliverables:
- [ ] **Mcp.Gateway.Server/Tools/Ollama/OllamaTools.cs**
  - [ ] `ollama_chat` tool (basic chat completion)
  - [ ] `ollama_generate` tool (text generation)
  - [ ] `ollama_list_models` tool (model discovery)
  
- [ ] **HttpClient setup in Program.cs**
  ```csharp
  builder.Services.AddHttpClient("Ollama", client =>
  {
      client.BaseAddress = new Uri("http://localhost:11434");
      client.Timeout = TimeSpan.FromSeconds(60);
  });
  ```

- [ ] **Example usage documentation**
  - [ ] `.internal/notes/v.1.2.0/implementation-guide.md`
  - [ ] Update main README.md with Ollama section

- [ ] **Unit tests**
  - [ ] `Mcp.Gateway.Tests/Tools/OllamaToolsTests.cs`
  - [ ] Mock Ollama API responses
  - [ ] Test all three tools

**Success criteria:**
- ✅ Tools discoverable via `tools/list`
- ✅ Can call tools via HTTP `/rpc` endpoint
- ✅ Works with GitHub Copilot via stdio
- ✅ All tests passing

---

### Phase 2: Client Library (v1.2.0) 🔜 NEXT

**Timeline:** 2-3 dager  
**Goal:** Ollama → MCP Gateway adapter (reverse integration)

#### Deliverables:
- [ ] **New project: Mcp.Gateway.Ollama**
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <LangVersion>14.0</LangVersion>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="OllamaSharp" Version="3.0.0" />
      <PackageReference Include="Microsoft.Extensions.AI" Version="9.0.0" />
    </ItemGroup>
    <ItemGroup>
      <ProjectReference Include="..\Mcp.Gateway.Tools\Mcp.Gateway.Tools.csproj" />
    </ItemGroup>
  </Project>
  ```

- [ ] **OllamaMcpAdapter.cs** (core adapter)
  ```csharp
  public class OllamaMcpAdapter
  {
      public async Task<string> ExecuteQuery(string userQuery);
      private async Task<List<ToolDefinition>> DiscoverMcpTools();
      private AIFunction ConvertToOllamaFunction(ToolDefinition tool);
  }
  ```

- [ ] **OllamaExtensions.cs** (ASP.NET integration)
  ```csharp
  public static WebApplication MapOllamaIntegration(
      this WebApplication app,
      string toolsEndpoint = "/ollama/tools");
  ```

- [ ] **Example: System monitoring agent**
  - [ ] `Examples/OllamaAgent/SystemMonitor.cs`
  - [ ] Demonstrates autonomous monitoring

**Success criteria:**
- ✅ Adapter kan kalle MCP tools via Ollama function calling
- ✅ Ollama kan autonomt beslutte hvilke tools som skal kalles
- ✅ System monitoring example fungerer
- ✅ Documentation complete

---

### Phase 3: Advanced Features (v1.3) 🔮 FUTURE

**Timeline:** 3-5 dager  
**Goal:** Production-ready features

#### Deliverables:
- [ ] Streaming support
  - [ ] `ollama_chat_stream` tool
  - [ ] `ollama_generate_stream` tool
  
- [ ] RAG (Retrieval-Augmented Generation)
  - [ ] `ollama_rag_query` tool
  - [ ] Context gathering from multiple tools
  
- [ ] Security features
  - [ ] Rate limiting
  - [ ] Human-in-the-loop approval flow
  - [ ] Audit logging

- [ ] Performance optimization
  - [ ] Connection pooling
  - [ ] Model warm-up
  - [ ] Benchmarks

**Success criteria:**
- ✅ Streaming works with WebSocket transport
- ✅ RAG correctly combines tool outputs
- ✅ Security measures prevent abuse
- ✅ Performance benchmarks meet targets

---

## 📁 File Structure

```
Mcp.Gateway/
├── .internal/notes/v.1.2.0/
│   ├── ollama-integration.md              (Research - Pattern 1)
│   ├── ollama-reverse-integration.md      (Research - Pattern 2)
│   ├── implementation-plan.md             (This file)
│   └── implementation-guide.md            (TODO: Step-by-step guide)
│
├── Mcp.Gateway.Tools/                     (Core - no changes)
│   ├── ToolExtensions.cs                  (Transport layer - unchanged)
│   ├── ToolInvoker.cs                     (Unchanged)
│   └── ToolService.cs                     (Unchanged)
│
├── Mcp.Gateway.Server/
│   ├── Tools/
│   │   └── Ollama/                        (NEW - Pattern 1 tools)
│   │       ├── OllamaTools.cs             (Chat, Generate, ListModels)
│   │       └── README.md                  (Tool usage documentation)
│   └── Program.cs                         (Add HttpClient config)
│
├── Mcp.Gateway.Ollama/                    (NEW PROJECT - Pattern 2)
│   ├── Mcp.Gateway.Ollama.csproj
│   ├── OllamaMcpAdapter.cs                (Main adapter)
│   ├── OllamaExtensions.cs                (ASP.NET extensions)
│   ├── OllamaFunctionConverter.cs         (Format conversion)
│   ├── Models/
│   │   ├── OllamaAdapterOptions.cs
│   │   ├── OllamaFunction.cs
│   │   └── OllamaToolCall.cs
│   └── README.md
│
├── Mcp.Gateway.Tests/
│   ├── Tools/
│   │   └── OllamaToolsTests.cs            (Tests for Pattern 1 tools)
│   └── Ollama/
│       └── OllamaMcpAdapterTests.cs       (Tests for Pattern 2 adapter)
│
├── Examples/                              (NEW - Example usage)
│   └── OllamaAgent/
│       ├── OllamaAgent.csproj
│       ├── SystemMonitor.cs               (Autonomous monitoring example)
│       └── README.md
│
└── README.md                              (Update with Ollama section)
```

---

## ✅ Implementation Checklist

### Phase 0: Tool Capabilities & Filtering (CRITICAL - DO FIRST!)

#### Step 0.1: Add ToolCapabilities enum (30 min)
- [ ] Open `Mcp.Gateway.Tools/ToolModels.cs`
- [ ] Add `ToolCapabilities` enum (after existing records)
  ```csharp
  /// <summary>
  /// Defines capabilities required by a tool.
  /// </summary>
  [Flags]
  public enum ToolCapabilities
  {
      /// <summary>Standard JSON-RPC tool (works on all transports)</summary>
      Standard = 1,
      
      /// <summary>Requires text streaming support (WebSocket or SSE)</summary>
      TextStreaming = 2,
      
      /// <summary>Requires binary streaming support (WebSocket only)</summary>
      BinaryStreaming = 4,
      
      /// <summary>Must use WebSocket transport</summary>
      RequiresWebSocket = 8
  }
  ```

#### Step 0.2: Update McpToolAttribute (15 min)
- [ ] Open `Mcp.Gateway.Tools/ToolModels.cs`
- [ ] Add property to `McpToolAttribute`:
  ```csharp
  /// <summary>
  /// Capabilities required by this tool. Default: Standard (works on all transports)
  /// </summary>
  public ToolCapabilities Capabilities { get; init; } = ToolCapabilities.Standard;
  ```

#### Step 0.3: Update ToolDefinition record (15 min)
- [ ] Open `Mcp.Gateway.Tools/ToolModels.cs`
- [ ] Add `Capabilities` property to `ToolDefinition` record:
  ```csharp
  public sealed record ToolDefinition(
      string Name,
      string Description,
      string InputSchema,
      ToolCapabilities Capabilities = ToolCapabilities.Standard  // NEW!
  );
  ```

#### Step 0.4: Update ToolService discovery (30 min)
- [ ] Open `Mcp.Gateway.Tools/ToolService.cs`
- [ ] Update `ScanForTools()` to extract `Capabilities` from attribute
- [ ] Add new method:
  ```csharp
  /// <summary>
  /// Gets tools filtered by transport capabilities.
  /// </summary>
  public Task<List<ToolDefinition>> GetToolsForTransport(string transport)
  {
      var allTools = GetTools(); // Existing method
      
      var allowedCapabilities = transport switch
      {
          "stdio" => ToolCapabilities.Standard,
          "http" => ToolCapabilities.Standard,
          "sse" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming,
          "ws" => ToolCapabilities.Standard | ToolCapabilities.TextStreaming | ToolCapabilities.BinaryStreaming,
          _ => ToolCapabilities.Standard
      };
      
      var filtered = allTools.Where(tool =>
          (tool.Capabilities & allowedCapabilities) != 0 ||
          tool.Capabilities == ToolCapabilities.Standard
      ).ToList();
      
      return Task.FromResult(filtered);
  }
  ```
  
#### Step 0.5: Mark existing streaming tools (30 min)
- [ ] Open `Mcp.Gateway.Server/Tools/Systems/BinaryStreams/StreamsIn.cs`
- [ ] Update `McpToolAttribute`:
  ```csharp
  [McpTool("system_binary_streams_in",
      Title = "Binary Stream In",
      Description = "...",
      Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket
  )]
  ```
- [ ] Gjenta for:
  - [ ] `StreamsOut.cs`
  - [ ] `StreamsDuplex.cs`
  - [ ] Eventuelle andre streaming-verktøy

#### Step 0.6: Update ToolInvoker (45 min)
- [ ] Open `Mcp.Gateway.Tools/ToolInvoker.cs`
- [ ] Legge til hjelpemetode for transportdeteksjon:
  ```csharp
  private static string DetectTransport(HttpContext? context)
  {
      if (context == null) return "stdio";
      if (context.WebSockets.IsWebSocketRequest) return "ws";
      if (context.Request.Headers["Accept"].Contains("text/event-stream")) return "sse";
      return "http";
  }
  ```
- [ ] Oppdatere håndtering av `tools/list` i hver transportmetode:
  ```csharp
  // I InvokeHttpRpcAsync, InvokeWsRpcAsync, osv.
  if (request.Method == "tools/list")
  {
      var transport = DetectTransport(context);
      var tools = await _toolService.GetToolsForTransport(transport);
      return ToolResponse.Success(request.Id, new { tools });
  }
  ```

#### Step 0.7: Unit Tests (1 time)
- [ ] Create `Mcp.Gateway.Tests/ToolCapabilitiesTests.cs`
- [ ] Test filtering per transport:
  ```csharp
  [Fact]
  public async Task GetToolsForTransport_Stdio_ExcludesStreamingTools()
  {
      // Arrange
      var service = new ToolService();
      
      // Act
      var tools = await service.GetToolsForTransport("stdio");
      
      // Assert
      Assert.DoesNotContain(tools, t => 
          t.Name.Contains("stream", StringComparison.OrdinalIgnoreCase));
  }
  
  [Fact]
  public async Task GetToolsForTransport_WebSocket_IncludesAllTools()
  {
      // Arrange
      var service = new ToolService();
      
      // Act
      var tools = await service.GetToolsForTransport("ws");
      
      // Assert
      Assert.Contains(tools, t => t.Name == "system_binary_streams_in");
  }
  ```

#### Step 0.8: Integration Tests (30 min)
- [ ] Update `Mcp.Gateway.Tests/Endpoints/Rpc/McpProtocolTests.cs`
- [ ] Add test for filtered tool list:
  ```csharp
  [Fact]
  public async Task ToolsList_ViaHttp_ExcludesStreamingTools()
  {
      var request = new { jsonrpc = "2.0", method = "tools/list", id = 1 };
      var response = await _client.PostAsJsonAsync("/rpc", request);
      
      var json = await response.Content.ReadFromJsonAsync<JsonRpcMessage>();
      var tools = json.Result.GetProperty("tools").EnumerateArray();
      
      Assert.DoesNotContain(tools, t =>
          t.GetProperty("name").GetString().Contains("binary_stream"));
  }
  ```

#### Step 0.9: Run all tests (15 min)
- [ ] `dotnet test`
- [ ] Verify all 45+ existing tests still pass
- [ ] Verify new capability tests pass

**Estimated total time for Phase 0:** ~4 timer

---

### Phase 1: Basic Integration

#### Step 1: Setup (15 min)
- [ ] Create branch `feat/ollama` ✅ (Already done!)
- [ ] Create directory `Mcp.Gateway.Server/Tools/Ollama`
- [ ] Update `.internal/notes/v.1.2.0/ollama-integration.md` (update date to 7. desember 2025)

#### Step 2: Implement Tools (2-3 timer)
- [ ] Create `OllamaTools.cs`
  - [ ] Add `ollama_chat` tool
  - [ ] Add `ollama_generate` tool
  - [ ] Add `ollama_list_models` tool
  - [ ] Add proper error handling
  - [ ] Add XML documentation comments

#### Step 3: Configure HttpClient (15 min)
- [ ] Update `Mcp.Gateway.Server/Program.cs`
  - [ ] Add HttpClient factory for Ollama
  - [ ] Configure base URL and timeout
  - [ ] Add to DI container

#### Step 4: Testing (1-2 timer)
- [ ] Create `OllamaToolsTests.cs`
  - [ ] Test `ollama_chat` with mock
  - [ ] Test `ollama_generate` with mock
  - [ ] Test `ollama_list_models` with mock
  - [ ] Test error scenarios
- [ ] Manual testing with Ollama running locally
  - [ ] Test via HTTP `/rpc` endpoint
  - [ ] Test via GitHub Copilot (stdio)

#### Step 5: Documentation (1 time)
- [ ] Create `.internal/notes/v.1.2.0/implementation-guide.md`
  - [ ] Prerequisites (install Ollama)
  - [ ] Setup instructions
  - [ ] Usage examples
  - [ ] Troubleshooting
- [ ] Update main `README.md`
  - [ ] Add "Ollama Integration" section
  - [ ] Add usage examples
  - [ ] Update feature list

---

### Phase 2: Client Library

#### Step 1: Project Setup (30 min)
- [ ] Create new project `Mcp.Gateway.Ollama`
- [ ] Add NuGet packages:
  - [ ] `OllamaSharp`
  - [ ] `Microsoft.Extensions.AI`
- [ ] Add project reference to `Mcp.Gateway.Tools`
- [ ] Update solution file

#### Step 2: Core Adapter (3-4 timer)
- [ ] Implement `OllamaMcpAdapter.cs`
  - [ ] `ExecuteQuery()` method
  - [ ] `DiscoverMcpTools()` method
  - [ ] `ConvertToOllamaFunction()` method
  - [ ] Error handling and retry logic
- [ ] Implement `OllamaFunctionConverter.cs`
  - [ ] MCP JSON Schema → Ollama function parameters
  - [ ] Handle complex types
  - [ ] Validation

#### Step 3: ASP.NET Extensions (1 time)
- [ ] Implement `OllamaExtensions.cs`
  - [ ] `MapOllamaIntegration()` extension method
  - [ ] Configuration options
  - [ ] Endpoint for tool discovery (`/ollama/tools`)

#### Step 4: Testing (2 timer)
- [ ] Create `OllamaMcpAdapterTests.cs`
  - [ ] Test tool discovery
  - [ ] Test function conversion
  - [ ] Test tool execution via adapter
  - [ ] Test error scenarios
- [ ] Manual testing with example agent

#### Step 5: Example Agent (1-2 timer)
- [ ] Create `Examples/OllamaAgent/SystemMonitor.cs`
  - [ ] Autonomous CPU monitoring
  - [ ] Alert sending
  - [ ] Demonstrate multi-step reasoning
- [ ] Create README with usage instructions

---

## 🧪 Testing Strategy

### Unit Tests

**Location:** `Mcp.Gateway.Tests/Tools/OllamaToolsTests.cs`

```csharp
public class OllamaToolsTests
{
    [Fact]
    public async Task OllamaChat_ReturnsSuccessResponse()
    {
        // Arrange: Mock HttpClient with Ollama response
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://localhost:11434/api/chat")
                .Respond("application/json", "{\"model\":\"llama3.2:1b\",\"message\":{\"content\":\"Hello!\"}}");
        
        var httpClient = mockHttp.ToHttpClient();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("Ollama")).Returns(httpClient);
        
        var tools = new OllamaTools(factory.Object);
        
        // Act
        var request = new JsonRpcMessage
        {
            Method = "ollama_chat",
            Id = 1,
            Params = new { model = "llama3.2:1b", prompt = "Hello" }
        };
        
        var response = await tools.OllamaChatTool(request);
        
        // Assert
        Assert.NotNull(response.Result);
        Assert.Equal("Hello!", response.Result.GetProperty("response").GetString());
    }
}
```

### Integration Tests

**Location:** `Mcp.Gateway.Tests/Ollama/OllamaIntegrationTests.cs`

```csharp
public class OllamaIntegrationTests : IClassFixture<McpGatewayFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OllamaChat_ViaHttpEndpoint_ReturnsResponse()
    {
        // Requires Ollama running locally
        var client = _fixture.CreateClient();
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "ollama_chat",
            id = 1,
            @params = new
            {
                model = "llama3.2:1b",
                prompt = "What is 2+2?"
            }
        };
        
        var response = await client.PostAsJsonAsync("/rpc", request);
        
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<JsonRpcMessage>();
        
        Assert.NotNull(result.Result);
        // Ollama should answer "4" or similar
    }
}
```

### Manual Testing Checklist

- [ ] **Prerequisites:** Ollama installed and running (`ollama serve`)
- [ ] **Model downloaded:** `ollama pull llama3.2:1b`
- [ ] **Server running:** `dotnet run --project Mcp.Gateway.Server`

**Test scenarios:**
1. [ ] HTTP endpoint: `curl -X POST http://localhost:5000/rpc -d '{...}'`
2. [ ] GitHub Copilot: `@mcp_gateway chat with Ollama: what is MCP?`
3. [ ] WebSocket: Test via browser console
4. [ ] Tool discovery: `tools/list` should include ollama tools
5. [ ] Error handling: Test with invalid model name
6. [ ] Timeout: Test with very large prompt (should timeout gracefully)

---

## 📚 Documentation Updates

### Files to Create:
- [ ] `.internal/notes/v.1.2.0/implementation-guide.md` - Step-by-step implementation guide
- [ ] `Mcp.Gateway.Server/Tools/Ollama/README.md` - Tool usage documentation
- [ ] `Mcp.Gateway.Ollama/README.md` - Client library documentation
- [ ] `Examples/OllamaAgent/README.md` - Agent example documentation

### Files to Update:
- [ ] `README.md` - Add Ollama integration section
- [ ] `CHANGELOG.md` - Add v1.2.0 changes
- [ ] `docs/MCP-Protocol.md` - Reference Ollama integration (if relevant)

### README.md Updates (Draft):

```markdown
## 🤖 Ollama Integration (v1.2.0)

MCP Gateway now supports integration with Ollama for local LLM capabilities!

### Pattern 1: MCP Tools → Ollama (Chat Interface)

Use Ollama as a chat backend via MCP Gateway tools:

```bash
# Via GitHub Copilot
@mcp_gateway chat with Ollama: explain MCP protocol

# Via HTTP
curl -X POST http://localhost:5000/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc":"2.0",
    "method":"ollama_chat",
    "id":1,
    "params":{
      "model":"llama3.2:1b",
      "prompt":"What is the Model Context Protocol?"
    }
  }'
```

### Pattern 2: Ollama → MCP Tools (Autonomous Agent)

Let Ollama autonomously call MCP tools:

```csharp
var adapter = new OllamaMcpAdapter(
    ollamaUrl: "http://localhost:11434",
    mcpGatewayUrl: "http://localhost:5000"
);

var response = await adapter.ExecuteQuery(
    "Monitor CPU and send alert if over 80%"
);
// Ollama bestemmer hvilke MCP tools som skal kalles, basert på konteksten
```

**Se også:** [Ollama Integrasjonsguide](docs/Ollama-Integration.md)
```

---

## 🚀 Release Plan (v1.2.0)

### Pre-release Checklist:
- [ ] All Phase 1 tasks complete
- [ ] All tests passing (unit + integration)
- [ ] Documentation complete
- [ ] Manual testing done
- [ ] CHANGELOG.md updated
- [ ] README.md updated
- [ ] Example code verified

### Release Process:
1. [ ] Merge `feat/ollama` → `main`
2. [ ] Tag release: `v1.2.0`
3. [ ] GitHub release notes (auto-generated + manual edits)
4. [ ] NuGet package update (if needed)
5. [ ] Announce in README and social media

### Success Metrics:
- ✅ All 45+ existing tests passing
- ✅ 10+ new Ollama-specific tests passing
- ✅ Zero breaking changes to existing API
- ✅ Documentation complete and clear
- ✅ Example agent demonstrates value

---

## 📝 Notes & Decisions

### Design Decisions:
1. **Separate namespace for Ollama integration** - Clean separation from core
2. **No changes to ToolExtensions.cs** - Preserve transport abstraction
3. **Client library as separate project** - Reusable across applications
4. **Multi-client support** - Web UI, Copilot, Ollama agent kan coexist

### Open Questions:
- [ ] Should we include Ollama tools in default `Mcp.Gateway.Server`? (Answer: YES)
- [ ] Should we publish `Mcp.Gateway.Ollama` as separate NuGet package? (Answer: YES, in v1.3)
- [ ] What's the recommended model for demos? (Answer: `phi3` or `llama3.2:1b`)

### Future Considerations (v2.0):
- LangChain integration (similar pattern)
- Semantic Kernel integration
- Azure OpenAI fallback
- Tool marketplace

---

## 🤝 Contributing

**Branch:** `feat/ollama`  
**Review process:** PR to `main` after Phase 1 complete  
**Questions?** Open issue on GitHub or ask in PR comments

---

**Last Updated:** 7. desember 2025  
**Status:** 🟡 Planning → Implementation starting  
**Next Action:** **Phase 0 Step 0.1 - Add ToolCapabilities enum** ⚠️ CRITICAL FIRST!

---

**Forfatter:** ARKo AS - AHelse Development Team  
**Versjon:** 1.1  
**Branch:** feat/ollama  
**Target:** v1.2.0 🚀
