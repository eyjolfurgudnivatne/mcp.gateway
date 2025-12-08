# 🚀 v1.2.0 Implementation Plan - MCP Gateway as Tool Bridge

**Forfatter:** ARKo AS - AHelse Development Team  
**Dato:** 7. desember 2025  
**Branch:** `feat/ollama`  
**Target Release:** v1.2.0  
**Status:** 🟢 Phase 0 Complete → Phase 1 Ready

---

## 🎯 Vision (Updated!)

**MCP Gateway er en bridge mellom AI og tools**

Ikke en kommunikasjonsløsning TIL AI-systemer (det finnes allerede), men en **tool provider** som gjør det enkelt å:
- ✅ Lage tools som AI kan bruke (GitHub Copilot, Ollama, Claude Desktop)
- ✅ Hente ut tool lists i kompatible formater (via JSON-RPC)
- ✅ Filtrere tools basert på transport-capabilities

**Bruksscenario:**
```csharp
// Din applikasjon med OllamaSharp
var httpClient = new HttpClient();

// 1. Hent tools i Ollama-format (via JSON-RPC)
var response = await httpClient.PostAsJsonAsync(
    "http://localhost:5000/rpc",
    new {
        jsonrpc = "2.0",
        method = "tools/list/ollama",  // ← Ollama-formatert liste
        id = 1
    });

var json = await response.Content.ReadFromJsonAsync<JsonRpcMessage>();
var tools = JsonSerializer.Deserialize<List<Tool>>(
    json.Result.GetProperty("tools").GetRawText());

// 2. Bruk direkte med Ollama
var ollama = new OllamaApiClient("http://localhost:11434");
var chat = new Chat(ollama) { Model = "llama3.2", Tools = tools };

var result = await chat.Send("What's the weather?");

// 3. Ollama kaller tools via function calling
// 4. Du sender tool calls til MCP Gateway via /rpc
```

---

## 📅 Implementation Phases

### ✅ Phase 0: Tool Capabilities & Filtering (COMPLETE!)

**Status:** 🎉 **DONE** - All 70 tests passing!

**Deliverables:**
- ✅ `ToolCapabilities` enum implemented
- ✅ `McpToolAttribute.Capabilities` property added
- ✅ `ToolDefinition.Capabilities` added
- ✅ `ToolService.GetToolsForTransport()` implemented
- ✅ Binary streaming tools marked with capabilities
- ✅ `ToolInvoker` updated with transport detection
- ✅ 70/70 tests passing

**Result:** Tools are now filtered by transport capabilities automatically!

---

### 🎯 Phase 1: Tool List Formatters (v1.2.0) - REVISED!

**Timeline:** 1-2 dager  
**Goal:** Make it easy to get tool lists in AI-platform-specific formats via JSON-RPC

#### Deliverables:

**1. Tool List Formatters** (2-3 timer)
```
Mcp.Gateway.Tools/Formatters/
├── IToolListFormatter.cs           (Interface)
├── McpToolListFormatter.cs         (Standard MCP format)
├── OllamaToolListFormatter.cs      (Ollama format)
└── MicrosoftAIToolListFormatter.cs (Microsoft.Extensions.AI format)
```

**2. New JSON-RPC Methods** (1 time)
```csharp
// In ToolInvoker.cs
"tools/list"              // Standard MCP (existing)
"tools/list/ollama"       // Ollama format (NEW!)
"tools/list/microsoft-ai" // Microsoft.AI format (NEW!)
"tools/list/openai"       // OpenAI format (FUTURE)
```

**3. Example Application** (2-3 timer)
```
Examples/OllamaIntegration/
├── OllamaIntegration.csproj
├── Program.cs                 (Demonstrates usage with OllamaSharp)
└── README.md                  (Usage guide)
```

**4. Documentation** (1 time)
- `.internal/notes/v.1.2.0/formatter-usage-guide.md` - How to use formatters
- Update main `README.md` with formatter section
- Update `CHANGELOG.md`

#### Success Criteria:
- ✅ `tools/list/ollama` returns Ollama-formatted tool list
- ✅ `tools/list/microsoft-ai` returns Microsoft.AI-formatted tool list
- ✅ Transport filtering applies automatically
- ✅ Example app demonstrates full integration
- ✅ Documentation is clear and complete
- ✅ All tests passing

---

### ❌ Removed from v1.2.0:

**NOT needed (JSON-RPC methods replace these):**
- ❌ GET `/tools` endpoint (use `tools/list/ollama` instead)
- ❌ `OllamaToolConverter` client-side utility (server handles formatting)
- ❌ `ollama_chat` tool (use OllamaSharp directly)
- ❌ `ollama_generate` tool (use OllamaSharp directly)

**What we DO provide:**
- ✅ Tools that AI can use (via function calling)
- ✅ Formatted tool lists via JSON-RPC (`tools/list/{format}`)
- ✅ Automatic transport filtering
- ✅ Example integration code

---

## 📁 Updated File Structure

```
Mcp.Gateway/
├── .internal/notes/v.1.2.0/
│   ├── implementation-plan.md             (This file - updated!)
│   ├── formatter-usage-guide.md           (NEW - how to use formatters)
│   ├── phase-0-progress.md                (Phase 0 complete)
│   └── README.md                          (Overview)
│
├── Mcp.Gateway.Tools/
│   ├── Formatters/                        (NEW! - Tool list formatters)
│   │   ├── IToolListFormatter.cs
│   │   ├── McpToolListFormatter.cs
│   │   ├── OllamaToolListFormatter.cs
│   │   └── MicrosoftAIToolListFormatter.cs
│   ├── ToolInvoker.cs                     (Add HandleFormattedToolsList)
│   ├── ToolService.cs                     (GetToolsForTransport - done!)
│   └── ToolModels.cs
│
├── Examples/                              (NEW)
│   └── OllamaIntegration/
│       ├── OllamaIntegration.csproj
│       ├── Program.cs                     (Full example with OllamaSharp)
│       └── README.md
│
├── Mcp.Gateway.Tests/
│   ├── ToolCapabilitiesTests.cs           (Phase 0 tests - done!)
│   └── Formatters/
│       ├── OllamaToolListFormatterTests.cs    (NEW)
│       └── MicrosoftAIToolListFormatterTests.cs (NEW)
│
└── README.md                              (Update with formatter section)
```

---

## ✅ Implementation Checklist (Revised)

### Phase 1: Tool List Formatters

#### Step 1.1: Create Formatter Infrastructure (1 time)
- [x] Create `Mcp.Gateway.Tools/Formatters/` directory
- [x] Create `IToolListFormatter.cs` interface
- [x] Create `McpToolListFormatter.cs` (standard MCP format)
- [x] Create `OllamaToolListFormatter.cs` (Ollama format)
- [x] Create `MicrosoftAIToolListFormatter.cs` (Microsoft.AI format)

#### Step 1.2: Update ToolInvoker (30 min)
- [x] Add `HandleFormattedToolsList()` method
- [x] Update `InvokeSingleAsync()` to handle `tools/list/{format}` methods
- [x] Add error handling for unknown formats

#### Step 1.3: Unit Tests (1 time)
- [x] Create `Mcp.Gateway.Tests/Formatters/OllamaToolListFormatterTests.cs`
  - [x] Test basic conversion
  - [x] Test with complex schemas
  - [x] Test edge cases
- [x] Create `Mcp.Gateway.Tests/Formatters/MicrosoftAIToolListFormatterTests.cs`
  - [x] Test basic conversion
  - [x] Test parameter mapping
  - [x] Test edge cases

#### Step 1.4: Integration Tests (30 min)
- [x] Test `tools/list/ollama` via HTTP
- [x] Test `tools/list/microsoft-ai` via HTTP
- [x] Test transport filtering still works
- [x] Test unknown format returns error

#### Step 1.5: Example Application (2-3 timer)
- [ ] Create `Examples/OllamaIntegration/` project
- [ ] Add NuGet reference: `OllamaSharp`
- [ ] Implement `Program.cs`:
  ```csharp
  using OllamaSharp;
  using System.Net.Http.Json;
  using System.Text.Json;
  
  var httpClient = new HttpClient();
  
  // 1. Hent tools i Ollama-format
  var response = await httpClient.PostAsJsonAsync(
      "http://localhost:5000/rpc",
      new {
          jsonrpc = "2.0",
          method = "tools/list/ollama",
          id = 1
      });
  
  var json = await response.Content.ReadFromJsonAsync<JsonRpcMessage>();
  var tools = JsonSerializer.Deserialize<List<Tool>>(
      json.Result.GetProperty("tools").GetRawText());
  
  // 2. Bruk med Ollama
  var ollama = new OllamaApiClient("http://localhost:11434");
  var chat = new Chat(ollama)
  {
      Model = "llama3.2",
      Tools = tools
  };
  
  Console.WriteLine("Ask Ollama (tools available):");
  var userInput = Console.ReadLine();
  
  var result = await chat.Send(userInput);
  Console.WriteLine($"Ollama: {result.Message.Content}");
  
  // 3. Handle tool calls
  if (result.Message.ToolCalls?.Any() == true)
  {
      foreach (var toolCall in result.Message.ToolCalls)
      {
          var toolResult = await httpClient.PostAsJsonAsync(
              "http://localhost:5000/rpc",
              new {
                  jsonrpc = "2.0",
                  method = toolCall.Function.Name,
                  @params = toolCall.Function.Arguments,
                  id = 2
              });
          
          // Continue conversation...
      }
  }
  ```

#### Step 1.6: Documentation (1-2 timer)
- [ ] Create `.internal/notes/v.1.2.0/formatter-usage-guide.md`
  - [ ] Prerequisites (Ollama, OllamaSharp, Microsoft.Extensions.AI)
  - [ ] How to use `tools/list/ollama`
  - [ ] How to use `tools/list/microsoft-ai`
  - [ ] Code examples
  - [ ] Troubleshooting
- [ ] Update `README.md`:
  ```markdown
  ## 🤖 Use with AI Platforms
  
  MCP Gateway provides tool lists in multiple formats via JSON-RPC:
  
  ### Ollama (OllamaSharp)
  ```csharp
  var response = await httpClient.PostAsJsonAsync("/rpc", new {
      jsonrpc = "2.0",
      method = "tools/list/ollama",
      id = 1
  });
  ```
  
  ### Microsoft.Extensions.AI
  ```csharp
  var response = await httpClient.PostAsJsonAsync("/rpc", new {
      jsonrpc = "2.0",
      method = "tools/list/microsoft-ai",
      id = 1
  });
  ```
  
  **See:** [Formatter Usage Guide](.internal/notes/v.1.2.0/formatter-usage-guide.md)
  ```
- [ ] Update `CHANGELOG.md` with v1.2.0 changes

---

## 🧪 Testing Strategy (Updated)

### Unit Tests

**New tests needed:**
- `OllamaToolListFormatterTests.cs` - Test Ollama formatter
- `MicrosoftAIToolListFormatterTests.cs` - Test Microsoft.AI formatter
- `FormattedToolsListTests.cs` - Test `tools/list/{format}` integration

**Existing tests:**
- ✅ `ToolCapabilitiesTests.cs` - Already done (Phase 0)

### Manual Testing

1. [ ] Start MCP Gateway: `dotnet run --project Mcp.Gateway.Server`
2. [ ] Test standard: `curl -X POST http://localhost:5000/rpc -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'`
3. [ ] Test Ollama: `curl -X POST http://localhost:5000/rpc -d '{"jsonrpc":"2.0","method":"tools/list/ollama","id":1}'`
4. [ ] Test Microsoft.AI: `curl -X POST http://localhost:5000/rpc -d '{"jsonrpc":"2.0","method":"tools/list/microsoft-ai","id":1}'`
5. [ ] Start Ollama: `ollama serve`
6. [ ] Run example: `dotnet run --project Examples/OllamaIntegration`
7. [ ] Verify Ollama kan call MCP tools

---

## 📚 Documentation Updates (Revised)

### Files to Create:
- [ ] `.internal/notes/v.1.2.0/formatter-usage-guide.md` - How to use formatters
- [ ] `Examples/OllamaIntegration/README.md` - Example app guide

### Files to Update:
- [ ] `README.md` - Add "Use with AI Platforms" section
- [ ] `CHANGELOG.md` - Add v1.2.0 changes
- [ ] `.internal/notes/v.1.2.0/README.md` - Update overview

### Files to Remove/Archive:
- [ ] Move `ollama-integration.md` → archive (Pattern 1 not needed)
- [ ] Move `ollama-reverse-integration.md` → archive (not needed)

---

## 🚀 Release Plan (v1.2.0)

### Pre-release Checklist:
- [x] Phase 0 complete (tool filtering)
- [x] Phase 1 complete (formatters)
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Example app verified
- [ ] CHANGELOG.md updated
- [ ] README.md updated

### Release Process:
1. [ ] Merge `feat/ollama` → `main`
2. [ ] Tag release: `v1.2.0`
3. [ ] GitHub release notes
4. [ ] NuGet package update (if needed)

### Success Metrics:
- ✅ 75+ tests passing (70 existing + 5 new)
- ✅ `tools/list/ollama` works
- ✅ `tools/list/microsoft-ai` works
- ✅ Example app demonstrates integration
- ✅ Documentation clear

---

## 📝 Key Decisions (Updated)

### What Changed:
1. ❌ **Removed:** GET `/tools` endpoint - Use JSON-RPC methods instead
2. ❌ **Removed:** Client-side converters - Server handles formatting
3. ✅ **Added:** `Formatters/` directory - Clean organization
4. ✅ **Added:** JSON-RPC methods (`tools/list/{format}`)
5. ✅ **Focus:** MCP Gateway as **tool provider** with format flexibility

### Rationale:
- **Konsistent med MCP Protocol** - Alt går gjennom JSON-RPC
- **Enklere for klienter** - Får formatert data direkte
- **Automatisk transport filtering** - Samme logikk som `tools/list`
- **Fremtidssikret** - Lett å legge til nye formater
- **Bedre separasjon** - Formattering i egen mappe

---

## 🎯 Next Steps

1. ✅ Phase 0 complete - Tool filtering works!
2. 🔜 Create `Formatters/` directory and interfaces
3. 🔜 Implement `OllamaToolListFormatter`
4. 🔜 Implement `MicrosoftAIToolListFormatter`
5. 🔜 Update `ToolInvoker` with `HandleFormattedToolsList`
6. 🔜 Build example app
7. 🔜 Write documentation
8. 🔜 Release v1.2.0

---

**Last Updated:** 8. desember 2025 (kl. 17:50)  
**Status:** 🟢 Phase 0 Complete → Phase 1: 70% Complete  
**Next Action:** Manual testing and example application

---

**Forfatter:** ARKo AS - AHelse Development Team  
**Versjon:** 3.0 (Revised with Formatters architecture)  
**Branch:** feat/ollama  
**Target:** v1.2.0 🚀
