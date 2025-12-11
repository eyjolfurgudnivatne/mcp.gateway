# 🤖 Ollama Integration with MCP Gateway

**Forfatter:** ARKo AS - AHelse Development Team  
**Dato:** 7. desember 2025  
**Status:** Research & Design  
**Versjon:** 1.0 (Draft)

---

## 📋 Innholdsfortegnelse

1. [Executive Summary](#executive-summary)
2. [Ollama API Overview](#ollama-api-overview)
3. [Compatibility Analysis](#compatibility-analysis)
4. [Integration Architecture](#integration-architecture)
5. [Implementation Patterns](#implementation-patterns)
6. [Performance Considerations](#performance-considerations)
7. [Security Considerations](#security-considerations)
8. [Roadmap](#roadmap)

---

## 🎯 Executive Summary

**Mål:** Integrere Ollama (lokalt kjørende LLM) med MCP Gateway for å gi AI-assistenter som GitHub Copilot tilgang til lokale språkmodeller.

**Hovedfunn:**
- ✅ Ollama bruker **HTTP REST API** (kompatibelt med MCP Gateway)
- ✅ Støtter **streaming** via Server-Sent Events (SSE)
- ✅ OpenAI-kompatibel API for enkel integrasjon
- ✅ Kan kjøres lokalt (privacy-first)
- ⚠️ Krever Ollama installert og kjørende (dependency)

**Konklusjon:** Ollama er **meget godt egnet** for integrasjon med MCP Gateway via HTTP RPC eller WebSocket transports.

---

## 🌐 Ollama API Overview

### Hva er Ollama?

**Ollama** er en plattform for å kjøre store språkmodeller (LLM) lokalt på egen maskin:
- **Lokalt kjørende**: Ingen skydependency
- **Privacy-first**: Data forlater aldri din maskin
- **OpenAI-kompatibel**: Drop-in replacement for OpenAI API
- **Modeller**: Llama 3.2, Gemma 2, Mistral, Code Llama, Phi-3, osv.

### API Endpoints

**Base URL:** `http://localhost:11434` (default)

| Endpoint | Metode | Beskrivelse |
|----------|--------|-------------|
| `/api/generate` | POST | Genererer tekst fra prompt |
| `/api/chat` | POST | Chat completion (OpenAI-style) |
| `/api/embeddings` | POST | Genererer embeddings |
| `/api/tags` | GET | Lister tilgjengelige modeller |
| `/api/show` | POST | Viser modell-info |
| `/api/create` | POST | Oppretter ny modell |
| `/api/pull` | POST | Laster ned modell |
| `/api/push` | POST | Uploader modell |

**Relevante for MCP Gateway:**
- ✅ `/api/generate` - Tekst-generering
- ✅ `/api/chat` - Chat completions
- ✅ `/api/embeddings` - Embeddings for semantisk søk
- ✅ `/api/tags` - Modell-discovery

---

## 🔌 API Spesifikasjon

### 1. Generate API

**Endpoint:** `POST /api/generate`

**Request:**
```json
{
  "model": "llama3.2:1b",
  "prompt": "Why is the sky blue?",
  "stream": false,
  "options": {
    "temperature": 0.7,
    "top_p": 0.9
  }
}
```

**Response (Non-streaming):**
```json
{
  "model": "llama3.2:1b",
  "created_at": "2024-12-05T10:00:00Z",
  "response": "The sky appears blue because...",
  "done": true,
  "context": [1, 2, 3, ...],
  "total_duration": 1234567890,
  "load_duration": 123456,
  "prompt_eval_count": 10,
  "eval_count": 50,
  "eval_duration": 987654321
}
```

**Response (Streaming):**
```json
{"model":"llama3.2:1b","created_at":"...","response":"The","done":false}
{"model":"llama3.2:1b","created_at":"...","response":" sky","done":false}
{"model":"llama3.2:1b","created_at":"...","response":" appears","done":false}
...
{"model":"llama3.2:1b","created_at":"...","response":"","done":true,"context":[...]}
```

### 2. Chat API

**Endpoint:** `POST /api/chat`

**Request:**
```json
{
  "model": "llama3.2:1b",
  "messages": [
    {"role": "system", "content": "You are a helpful assistant."},
    {"role": "user", "content": "What is MCP?"}
  ],
  "stream": false
}
```

**Response:**
```json
{
  "model": "llama3.2:1b",
  "created_at": "2024-12-05T10:00:00Z",
  "message": {
    "role": "assistant",
    "content": "MCP stands for Model Context Protocol..."
  },
  "done": true
}
```

### 3. Embeddings API

**Endpoint:** `POST /api/embeddings`

**Request:**
```json
{
  "model": "nomic-embed-text",
  "prompt": "The sky is blue"
}
```

**Response:**
```json
{
  "embedding": [0.123, -0.456, 0.789, ...]
}
```

### 4. Tags API (List Models)

**Endpoint:** `GET /api/tags`

**Response:**
```json
{
  "models": [
    {
      "name": "llama3.2:1b",
      "modified_at": "2024-12-05T10:00:00Z",
      "size": 1073741824,
      "digest": "sha256:abc123..."
    },
    {
      "name": "phi3:latest",
      "modified_at": "2024-12-04T12:00:00Z",
      "size": 2147483648,
      "digest": "sha256:def456..."
    }
  ]
}
```

---

## ✅ Compatibility Analysis

### MCP Gateway Capabilities vs. Ollama

| Feature | MCP Gateway | Ollama | Compatible? |
|---------|-------------|--------|-------------|
| **Transport** | HTTP, WebSocket, SSE, stdio | HTTP REST | ✅ Yes |
| **Streaming** | Binary + Text streaming | SSE (text) | ✅ Yes |
| **JSON-RPC** | Full support | N/A | ⚠️ Adapter needed |
| **Authentication** | Pluggable | None (local) | ✅ Yes |
| **Request/Response** | JSON-RPC format | Plain JSON | ⚠️ Wrapper needed |

### Integration Patterns

**Pattern 1: MCP Tools as Ollama Proxy** ✅ RECOMMENDED
- MCP Tool wraps Ollama API calls
- Client: GitHub Copilot → MCP Gateway → Ollama
- Benefits:
  - ✅ Simple to implement
  - ✅ No changes to MCP Gateway core
  - ✅ Full control over prompts
  - ✅ Can combine with other tools

**Pattern 2: Ollama as MCP Client** ❌ NOT RECOMMENDED
- Ollama calls MCP Gateway tools
- Client: Ollama → MCP Gateway → Tools
- Issues:
  - ❌ Ollama doesn't support MCP protocol
  - ❌ Requires Ollama modifications
  - ❌ Not beneficial

**Pattern 3: Hybrid (MCP + Ollama together)** ✅ ADVANCED
- MCP Gateway tools enhance Ollama responses
- Client: GitHub Copilot → MCP Gateway → Ollama + Tools → Combined Response
- Benefits:
  - ✅ RAG (Retrieval-Augmented Generation)
  - ✅ Tool-augmented LLM responses
  - ✅ Context from multiple sources

---

## 🏗️ Integration Architecture

### Recommended Architecture: Pattern 1 (Ollama Proxy)

```
┌──────────────────────────────────────────────────────────┐
│                      MCP Clients                         │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐           │
│  │  GitHub   │  │   Claude  │  │  Custom   │           │
│  │  Copilot  │  │  Desktop  │  │  Client   │           │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘           │
└────────┼──────────────┼──────────────┼──────────────────┘
         │              │              │
         │ stdio        │ SSE          │ HTTP/WS
         │              │              │
┌────────▼──────────────▼──────────────▼──────────────────┐
│         MCP Gateway Server (ASP.NET Core)                │
│  ┌──────────────────────────────────────────────────┐   │
│  │             Transport Layer                      │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐   │   │
│  │  │  HTTP  │ │   WS   │ │  SSE   │ │ stdio  │   │   │
│  │  └───┬────┘ └───┬────┘ └───┬────┘ └───┬────┘   │   │
│  └──────┼──────────┼──────────┼──────────┼─────────┘   │
│         │          │          │          │              │
│  ┌──────▼──────────▼──────────▼──────────▼─────────┐   │
│  │     Mcp.Gateway.Tools (Core Library)            │   │
│  │  ┌───────────────────────────────────────────┐  │   │
│  │  │ ToolInvoker - Routes to Ollama Tools     │  │   │
│  │  └──────────────────┬────────────────────────┘  │   │
│  │                     │                            │   │
│  │  ┌──────────────────▼────────────────────────┐  │   │
│  │  │ Ollama Tools (NEW!)                       │  │   │
│  │  │  ┌──────────────┐  ┌──────────────┐      │  │   │
│  │  │  │ ollama_chat  │  │ollama_generate│     │  │   │
│  │  │  └──────┬───────┘  └──────┬───────┘      │  │   │
│  │  │         │                  │              │  │   │
│  │  │         └────────┬─────────┘              │  │   │
│  │  └──────────────────┼────────────────────────┘  │   │
│  └─────────────────────┼───────────────────────────┘   │
└────────────────────────┼───────────────────────────────┘
                         │
                         │ HTTP
                         │
                    ┌────▼────┐
                    │ Ollama  │
                    │ Server  │
                    │ :11434  │
                    └─────────┘
                         │
                    ┌────▼────┐
                    │  Local  │
                    │  Models │
                    └─────────┘
```

### Data Flow

```
1. User: "@mcp_gateway what is 2+2?"
   ↓
2. GitHub Copilot → MCP Gateway (stdio)
   {"jsonrpc":"2.0","method":"ollama_chat","params":{...}}
   ↓
3. MCP Gateway → ollama_chat tool
   ↓
4. ollama_chat tool → Ollama API
   POST http://localhost:11434/api/chat
   {"model":"llama3.2:1b","messages":[...]}
   ↓
5. Ollama → LLM inference → Response
   {"message":{"content":"2+2 equals 4."}}
   ↓
6. ollama_chat tool → MCP Gateway
   {"jsonrpc":"2.0","result":{"response":"2+2 equals 4."}}
   ↓
7. MCP Gateway → GitHub Copilot
   ↓
8. User sees: "2+2 equals 4."
```

---

## 💻 Implementation Patterns

### Pattern 1: Basic Ollama Tool

```csharp
using Mcp.Gateway.Tools;
using System.Net.Http.Json;

namespace Mcp.Gateway.Server.Tools.Ollama;

public class OllamaClient
{
    private readonly HttpClient _httpClient;
    
    public OllamaClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
    }

    [McpTool("ollama_chat",
        Title = "Ollama Chat",
        Description = "Chat with a local LLM via Ollama",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""model"":{""type"":""string"",""description"":""Model name (e.g., llama3.2:1b)""},
                ""prompt"":{""type"":""string"",""description"":""User prompt""}
            },
            ""required"":[""model"",""prompt""]
        }")]
    public async Task<JsonRpcMessage> OllamaChatTool(JsonRpcMessage request)
    {
        var args = request.GetParams<OllamaChatRequest>();
        
        var ollamaRequest = new
        {
            model = args.model,
            messages = new[]
            {
                new { role = "user", content = args.prompt }
            },
            stream = false
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "http://localhost:11434/api/chat", 
            ollamaRequest);
        
        response.EnsureSuccessStatusCode();
        
        var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
        
        return ToolResponse.Success(request.Id, new { 
            response = ollamaResponse?.message?.content,
            model = ollamaResponse?.model
        });
    }
    
    record OllamaChatRequest(string model, string prompt);
    record OllamaChatResponse(string model, Message message, bool done);
    record Message(string role, string content);
}
```

**Program.cs:**
```csharp
// Add HttpClient for Ollama
builder.Services.AddHttpClient("Ollama", client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
    client.Timeout = TimeSpan.FromSeconds(60); // LLM inference kan ta tid
});
```

### Pattern 2: Streaming Ollama Tool

```csharp
[McpTool("ollama_generate_stream",
    Title = "Ollama Generate (Streaming)",
    Description = "Generates text via Ollama with streaming",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""model"":{""type"":""string""},
            ""prompt"":{""type"":""string""}
        },
        ""required"":[""model"",""prompt""]
    }")]
public async Task OllamaGenerateStreamTool(ToolConnector connector)
{
    // Read client request
    string? model = null;
    string? prompt = null;
    
    connector.OnStart = async ctx =>
    {
        var meta = ctx.Meta;
        model = meta?.GetProperty("model").GetString();
        prompt = meta?.GetProperty("prompt").GetString();
    };
    
    await connector.StartReceiveLoopAsync();
    
    if (string.IsNullOrEmpty(model) || string.IsNullOrEmpty(prompt))
    {
        throw new ArgumentException("Model and prompt required");
    }
    
    // Call Ollama API with streaming
    var ollamaRequest = new
    {
        model,
        prompt,
        stream = true
    };
    
    var request = new HttpRequestMessage(HttpMethod.Post, 
        "http://localhost:11434/api/generate")
    {
        Content = JsonContent.Create(ollamaRequest)
    };
    
    using var response = await _httpClient.SendAsync(request, 
        HttpCompletionOption.ResponseHeadersRead);
    
    response.EnsureSuccessStatusCode();
    
    // Stream response back to client
    var meta = new StreamMessageMeta(
        Method: "ollama_generate_stream",
        Binary: false);
    
    await using var outStream = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);
    
    await using var stream = await response.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);
    
    int index = 0;
    while (!reader.EndOfStream)
    {
        var line = await reader.ReadLineAsync();
        if (string.IsNullOrEmpty(line)) continue;
        
        var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
        
        await outStream.WriteChunkAsync(new { 
            index = index++,
            text = chunk?.response,
            done = chunk?.done ?? false
        });
        
        if (chunk?.done == true) break;
    }
    
    await outStream.CompleteAsync(new { totalChunks = index });
}

record OllamaStreamChunk(string? response, bool done);
```

### Pattern 3: Hybrid (RAG - Retrieval-Augmented Generation)

```csharp
[McpTool("ollama_rag_query",
    Title = "Ollama RAG Query",
    Description = "Queries local LLM with context from MCP tools",
    InputSchema = @"{
        ""type"":""object"",
        ""properties"":{
            ""query"":{""type"":""string"",""description"":""User query""},
            ""context_tools"":{""type"":""array"",""items"":{""type"":""string""},""description"":""Tools to gather context from""}
        },
        ""required"":[""query""]
    }")]
public async Task<JsonRpcMessage> OllamaRagQueryTool(
    JsonRpcMessage request,
    ToolService toolService)
{
    var args = request.GetParams<RagQueryRequest>();
    
    // Step 1: Gather context from other MCP tools
    var context = new List<string>();
    
    if (args.context_tools != null)
    {
        foreach (var toolName in args.context_tools)
        {
            // Invoke tool to get context
            var toolRequest = new JsonRpcMessage
            {
                Method = toolName,
                Id = Guid.NewGuid().ToString(),
                Params = new { query = args.query }
            };
            
            var toolResponse = await toolService.InvokeToolAsync(toolRequest);
            if (toolResponse?.Result != null)
            {
                context.Add(toolResponse.Result.ToString() ?? "");
            }
        }
    }
    
    // Step 2: Build enhanced prompt with context
    var enhancedPrompt = $@"
Context from tools:
{string.Join("\n", context)}

User query: {args.query}

Please answer the query using the provided context.
";
    
    // Step 3: Call Ollama with enhanced prompt
    var ollamaRequest = new
    {
        model = "llama3.2:1b",
        messages = new[]
        {
            new { role = "system", content = "You are a helpful assistant that uses provided context to answer questions accurately." },
            new { role = "user", content = enhancedPrompt }
        },
        stream = false
    };
    
    var response = await _httpClient.PostAsJsonAsync(
        "http://localhost:11434/api/chat", 
        ollamaRequest);
    
    var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
    
    return ToolResponse.Success(request.Id, new { 
        response = ollamaResponse?.message?.content,
        context_sources = args.context_tools,
        context_count = context.Count
    });
}

record RagQueryRequest(string query, string[]? context_tools);
```

---

## ⚡ Performance Considerations

### 1. Latency

**Ollama inference time:**
- Small models (1B params): ~100-500ms
- Medium models (7B params): ~500-2000ms
- Large models (13B+ params): ~2000-10000ms

**MCP Gateway overhead:**
- JSON-RPC parsing: <1ms
- HTTP proxy: ~5-10ms
- Total added latency: ~10-15ms (negligible)

**Optimization:**
- ✅ Use HttpClient pooling (already implemented)
- ✅ Reuse Ollama connections
- ✅ Consider model warm-up (preload on startup)

### 2. Memory

**Ollama memory usage:**
- 1B model: ~2 GB RAM
- 7B model: ~8 GB RAM
- 13B model: ~16 GB RAM

**MCP Gateway memory:**
- ~50-100 MB (minimal overhead)

**Recommendation:** Ensure server has sufficient RAM for chosen model + MCP Gateway.

### 3. Concurrent Requests

**Ollama limitations:**
- Single model instance per process
- Sequential request processing (no parallelism by default)
- GPU sharing possible (with proper config)

**MCP Gateway:**
- Async/await throughout (non-blocking)
- Can queue requests for Ollama
- Consider request timeout (60s default)

---

## 🔒 Security Considerations

### 1. Local-only Access

**Ollama default:** Listens on `localhost:11434` (not exposed to network)

**MCP Gateway:**
- ✅ Keep Ollama binding to localhost
- ✅ MCP Gateway can be exposed (acts as controlled proxy)
- ✅ Add authentication to MCP Gateway endpoints if needed

### 2. Prompt Injection

**Risk:** User could craft malicious prompts to:
- Extract sensitive information
- Bypass system instructions
- Cause unexpected behavior

**Mitigation:**
```csharp
// Sanitize user input
public string SanitizePrompt(string userPrompt)
{
    // Remove potential injection patterns
    userPrompt = userPrompt
        .Replace("Ignore previous instructions", "")
        .Replace("System:", "")
        .Replace("Assistant:", "");
    
    // Limit length
    if (userPrompt.Length > 2000)
    {
        userPrompt = userPrompt[..2000];
    }
    
    return userPrompt;
}
```

### 3. Resource Exhaustion

**Risk:** Malicious user sends expensive queries to DoS Ollama

**Mitigation:**
```csharp
// Rate limiting
[McpTool("ollama_chat", ...)]
[RateLimit(RequestsPerMinute = 10)] // Custom attribute
public async Task<JsonRpcMessage> OllamaChatTool(JsonRpcMessage request)
{
    // ...
}

// Timeout enforcement
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var response = await _httpClient.PostAsJsonAsync(url, request, cts.Token);
```

---

## 🚀 Roadmap

### Phase 1: Basic Integration (v1.2) ✅ FEASIBLE NOW

**Goal:** Proof-of-concept Ollama tools

**Deliverables:**
- [ ] `ollama_chat` tool (basic chat completion)
- [ ] `ollama_generate` tool (text generation)
- [ ] `ollama_list_models` tool (model discovery)
- [ ] Example usage documentation
- [ ] Unit tests

**Effort:** ~1 dag
**Dependencies:** Ollama installed lokalt

---

### Phase 2: Streaming Support (v1.3)

**Goal:** Real-time streaming responses

**Deliverables:**
- [ ] `ollama_chat_stream` tool (streaming chat)
- [ ] `ollama_generate_stream` tool (streaming generation)
- [ ] ToolConnector integration for Ollama SSE
- [ ] Performance benchmarks

**Effort:** ~2 dager
**Dependencies:** Phase 1 complete

---

### Phase 3: Advanced Features (v2.0)

**Goal:** Production-ready Ollama integration

**Deliverables:**
- [ ] RAG (Retrieval-Augmented Generation) tools
- [ ] Embedding generation (`ollama_embeddings`)
- [ ] Context management (conversation history)
- [ ] Model management (pull, create, delete)
- [ ] Configuration (model selection, temperature, osv.)
- [ ] Rate limiting and security
- [ ] Comprehensive documentation

**Effort:** ~1 uke
**Dependencies:** Phase 2 complete

---

## 📊 Feasibility Matrix

| Feature | Complexity | Benefit | Priority |
|---------|-----------|---------|----------|
| Basic chat/generate | Low | High | P0 |
| Streaming | Medium | High | P1 |
| Model discovery | Low | Medium | P1 |
| Embeddings | Low | Medium | P2 |
| RAG integration | High | High | P2 |
| Context management | Medium | Medium | P3 |
| Model management | Medium | Low | P3 |

**P0 = Must-have** (v1.2)  
**P1 = Should-have** (v1.3)  
**P2 = Nice-to-have** (v2.0)  
**P3 = Future** (v2.1+)

---

## 🎓 Learning Resources

### Official Ollama Documentation
- **GitHub:** https://github.com/ollama/ollama
- **API Docs:** https://github.com/ollama/ollama/blob/main/docs/api.md
- **Model Library:** https://ollama.com/library

### Microsoft Resources
- **Aspire Ollama Integration:** https://learn.microsoft.com/en-us/dotnet/aspire/compatibility/9.0/ollama-integration-updates
- **AIShell Ollama Plugin:** https://learn.microsoft.com/en-us/powershell/utility-modules/aishell/developer/ollama-agent-readme

### Community
- **Discord:** https://discord.gg/ollama
- **Reddit:** r/ollama

---

## 💡 Konklusjon

**Anbefaling:** ✅ **IMPLEMENTER OLLAMA INTEGRASJON**

**Begrunnelse:**
1. **Høy kompatibilitet** - Ollama HTTP API passer perfekt med MCP Gateway
2. **Lav kompleksitet** - Kan implementeres som standard MCP tools (ingen core changes)
3. **Stor verdi** - Gir lokale LLM-capabilities til GitHub Copilot og andre MCP clients
4. **Privacy-friendly** - Alt kjører lokalt (ingen cloud dependency)
5. **Fremtidssikret** - Kan utvides til RAG, embeddings, osv.

**Neste steg:**
1. Install Ollama og test API manuelt
2. Implementer `ollama_chat` tool (POC)
3. Test med GitHub Copilot
4. Utvid til streaming og flere features
5. Dokumenter best practices

---

**Forfatter:** ARKo AS - AHelse Development Team  
**Dato:** 7. desember 2025  
**Versjon:** 1.0 (Draft)  
**Status:** Ready for implementation 🚀
