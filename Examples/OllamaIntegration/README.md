# 🚀 MCP Gateway + Ollama Integration Example

This example demonstrates **direct integration** between **MCP Gateway tools** and **Ollama** using OllamaSharp's native tool invocation.

**Last Updated:** 11. desember 2025  
**MCP Gateway Version:** 1.2.0  
**OllamaSharp Version:** 5.4.12

---

## 📋 What This Example Does

This is a **working production example** showing:

1. ✅ **Tool Discovery** - Fetches MCP Gateway tools in Ollama-native format
2. ✅ **Direct Tool Invocation** - Uses `DirectToolInvoker` for zero-HTTP-overhead tool calls
3. ✅ **Full Chat Integration** - Ollama can call any MCP Gateway tool during conversations
4. ✅ **Streaming Support** - Real-time streaming responses with tool execution

**Example Tools:**
- `generate_secret` - Generates random secrets (GUID, hex, base64)
- `tell_ollama` - Tell Ollama To Get a secret

---

## ✅ Prerequisites

### 1. MCP Gateway Running

```bash
# Terminal 1: Start MCP Gateway
cd Examples/OllamaIntegration
dotnet run
```

MCP Gateway will run on `http://localhost:62080`

### 2. Ollama Installed & Running

```bash
# Install Ollama (if needed)
# Download from: https://ollama.com/

# Terminal 2: Start Ollama
ollama serve
```

Ollama should run on `http://localhost:11434` (or configure remote server)

### 3. Model Downloaded

```bash
# Pull the model (only needed once)
ollama pull llama3.2
```

**Recommended models:**
- `llama3.2` (fast, small)
- `llama3.1:8b` (better function calling)
- `qwen2.5:7b` (excellent function calling)

---

## 🎯 How to Run

### HTTP Mode (Testing)

```bash
cd Examples/OllamaIntegration
dotnet run
```

Then test via HTTP:

```powershell
# Test the tell_ollama tool
$body = @{
    jsonrpc = "2.0"
    method = "tell_ollama"
    params = @{ format = "hex" }
    id = 1
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/rpc -Body $body -ContentType "application/json"
```

### stdio Mode (GitHub Copilot Integration)

```bash
cd Examples/OllamaIntegration
echo '{"jsonrpc":"2.0","method":"tell_ollama","params":{"format":"guid"},"id":1}' | dotnet run -- --stdio
```

---

## 🏗️ Architecture

### Component Overview

```
GitHub Copilot / HTTP Client
          ↓ (JSON-RPC)
    tell_ollama tool
          ↓
    OllamaSharp Chat
          ↓ (function calling)
    DirectToolInvoker
          ↓ (direct method call, no HTTP!)
    MCP Gateway ToolInvoker
          ↓
    generate_secret / add_numbers / etc.
```

### Key Components

#### 1. TellOllama Tool (`Tools/TellOllama.cs`)

**Main integration point** - Handles:
- Tool discovery from ToolService
- Ollama chat initialization
- DirectToolInvoker setup
- Streaming response handling


#### 2. DirectToolInvoker (`DirectToolInvoker.cs`)


**Benefits:**
- ✅ No HTTP overhead
- ✅ Direct method invocation
- ✅ Full type safety
- ✅ Same DI container

#### 3. OllamaToolListFormatter (`Formatters/OllamaToolListFormatter.cs`)

Converts MCP tool definitions → Ollama function format:

```json
{
  "type": "function",
  "function": {
    "name": "generate_secret",
    "description": "Generates a random secret token",
    "parameters": {
      "type": "object",
      "properties": {
        "format": {
          "type": "string",
          "enum": ["guid", "hex", "base64"]
        }
      }
    }
  }
}
```

---

## 🔧 Configuration

### Remote Ollama Server

Edit `Tools/TellOllama.cs` (line 37):

```csharp
// Use remote Ollama server
const string ollamaUrl = "http://your-server:11434";
```

### Different Model

Change model (line 38):

```csharp
const string model = "llama3.1:8b";  // Better function calling
```

### Tool Filtering

Tools are filtered by:
1. **Transport capabilities** - `GetToolsForTransport("http")` excludes streaming tools
2. **Recursion prevention** - `tell_ollama` skips itself to avoid loops

---

## 🐛 Troubleshooting

### "Model 'llama3.2' not found"

```bash
# Pull the model
ollama pull llama3.2

# Verify it's installed
ollama list
```

### "Failed to connect to Ollama"

```bash
# Check Ollama is running
ollama serve

# Verify endpoint
curl http://localhost:11434/api/tags
```

### "Tool not found" / Function calling issues

**Symptom:** Ollama says "function not available"

**Common causes:**
1. **Model too small** - Use `llama3.1:8b` or larger
2. **Tool filtered out** - Check `GetToolsForTransport("http")` includes it
3. **Recursion filter** - Tool name is `tell_ollama` (intentionally skipped)

---

## 📊 Performance

**Direct Tool Invocation** (via `DirectToolInvoker`):
- ⚡ **<1ms overhead** (no HTTP serialization/deserialization)
- ✅ **Same process** (direct method call)
- ✅ **Shared DI container** (efficient service resolution)

**vs. HTTP-based invocation:**
- ❌ ~10-50ms overhead (HTTP roundtrip)
- ❌ Serialization/deserialization overhead
- ❌ Network latency

---

**Created:** 8. desember 2025  
**Updated:** 11. desember 2025  
**Status:** ✅ Production Ready
