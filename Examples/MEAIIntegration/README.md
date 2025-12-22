# MEAIIntegration Example

**Microsoft.Extensions.AI integration with MCP Gateway**

This example demonstrates how to use MCP Gateway tools with Microsoft.Extensions.AI and Ollama for autonomous AI agents.

## 🎯 Overview

MEAIIntegration shows two different patterns for integrating MCP Gateway with AI models:

1. **Local Invoker** - Direct in-process tool invocation (fast, simple)
2. **Remote Invoker** - HTTP-based tool invocation (distributed, scalable)

Both patterns use the same `IMEAIInvoker` interface, making it easy to switch between them via configuration.

## 🏗️ Architecture

```
Client
  ↓
MEAIIntegration Server (port 5237)
  ├─ Tool: tell_ollama
  │    ↓ Uses IMEAIInvoker
  │    ↓ Calls Ollama LLM
  │    ↓ Ollama decides which tools to use
  │    ↓ IMEAIInvoker invokes tools (Local or Remote)
  │
  ├─ MEAILocalInvoker (Mode: "Local")
  │    └─ ToolInvoker.InvokeSingleAsync() → Direct tool invocation
  │
  └─ MEAIRemoteInvoker (Mode: "Remote")
       └─ HttpClient → Remote MCP Gateway (e.g., OllamaIntegration)
```

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- Ollama proxy access (or local Ollama instance)
- Visual Studio 2025 / Rider / VS Code

### 1. Start MEAIIntegration (Local Mode)

```bash
cd Examples/MEAIIntegration
dotnet run
```

Server starts at: `http://localhost:5237`

### 2. Test with HTTP

```bash
curl -X POST http://localhost:5237/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "tell_ollama",
      "arguments": {
        "format": "hex"
      }
    },
    "id": 1
  }'
```

## 🔧 Configuration

### Local Mode (Default)

**appsettings.json:**
```json
{
  "MEAIIntegration": {
    "Mode": "Local",
    "RemoteUrl": "http://localhost:62080"
  }
}
```

**Behavior:**
- Tools are invoked directly via `ToolInvoker.InvokeSingleAsync()`
- No HTTP overhead
- Fastest performance
- Best for development and testing

### Remote Mode

**appsettings.json:**
```json
{
  "MEAIIntegration": {
    "Mode": "Remote",
    "RemoteUrl": "http://localhost:62080"
  }
}
```

**Behavior:**
- Tools are invoked via HTTP to remote MCP Gateway
- Supports distributed scenarios
- Scalable (multiple instances)
- Best for production with separate tool servers

## 📂 Project Structure

```
MEAIIntegration/
├── Program.cs                    # ASP.NET Core setup, endpoint mapping
├── appsettings.json              # Configuration (Local/Remote mode)
├── MEAIInvoker.cs                # Interface, ToolDetails, McpGatewayTool
├── MEAILocalInvoker.cs           # Local tool invocation
├── MEAIRemoteInvoker.cs          # Remote tool invocation (HTTP)
├── Tools/
│   └── TellMEAIOllama.cs         # Example tool using Ollama
└── remoteTest.http               # HTTP test cases
```

## 🔍 How It Works

### Local Invoker

```csharp
public class MEAILocalInvoker(ToolService toolService, ToolInvoker gatewayInvoker) 
    : IMEAIInvoker
{
    public async ValueTask<AIFunction[]> BuildToolListAsync()
    {
        // 1. Get tools from local ToolService
        var tools = toolService.GetFunctionsForTransport(...);
        
        // 2. Wrap each tool as AIFunction
        return tools.Select(t => new McpGatewayTool(
            toolDetails,
            async (args, ct) =>
            {
                // 3. Call tool directly (no HTTP!)
                var result = await gatewayInvoker.InvokeSingleAsync(...);
                return ExtractToolResult(result, ct);
            }
        )).ToArray();
    }
}
```

**Advantages:**
- ✅ Fast (direct invocation)
- ✅ Simple (no HTTP configuration)
- ✅ Best for development

### Remote Invoker

```csharp
public class MEAIRemoteInvoker(IHttpClientFactory httpClientFactory) 
    : IMEAIInvoker
{
    public async ValueTask<AIFunction[]> BuildToolListAsync()
    {
        // 1. Get tools from remote MCP Gateway via HTTP
        var response = await _httpClient.PostAsJsonAsync("/rpc", 
            new { jsonrpc = "2.0", method = "tools/list", id = 1 });
        
        // 2. Parse tool list
        var tools = ParseToolList(response);
        
        // 3. Wrap each tool as AIFunction
        return tools.Select(t => new McpGatewayTool(
            toolDetails,
            async (args, ct) =>
            {
                // 4. Call remote tool via HTTP
                var toolResponse = await _httpClient.PostAsJsonAsync("/rpc",
                    new { jsonrpc = "2.0", method = "tools/call", ... });
                return ExtractResultFromMessage(toolResponse);
            }
        )).ToArray();
    }
}
```

**Advantages:**
- ✅ Distributed (separate servers)
- ✅ Scalable (multiple instances)
- ✅ Best for production

## 🧪 Testing

### Test Local Mode

**1. Start MEAIIntegration (Local):**
```bash
# appsettings.json: Mode = "Local"
dotnet run
```

**2. Call tool:**
```bash
curl -X POST http://localhost:5237/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "tell_ollama",
      "arguments": { "format": "hex" }
    },
    "id": 1
  }'
```

**Expected:** Tool executes locally (no HTTP to other servers).

---

### Test Remote Mode

**1. Start OllamaIntegration (remote MCP Gateway):**
```bash
cd Examples/OllamaIntegration
dotnet run
# Port: 62080
```

**2. Verify OllamaIntegration is running:**
```bash
curl -X POST http://localhost:62080/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 1
  }'
```

**3. Start MEAIIntegration (Remote mode):**
```bash
cd Examples/MEAIIntegration
# appsettings.json: Mode = "Remote", RemoteUrl = "http://localhost:62080"
dotnet run
# Port: 5237
```

**4. Call tool:**
```bash
curl -X POST http://localhost:5237/rpc \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "tell_ollama",
      "arguments": { "format": "hex" }
    },
    "id": 1
  }'
```

**Expected:** 
- MEAIIntegration calls OllamaIntegration via HTTP
- Tool executes on remote server
- Result returned to Ollama
- Response returned to client

---

### Using remoteTest.http

Open `remoteTest.http` in Visual Studio / VS Code and run:

**Test 1: Call tell_ollama tool**
```
POST http://localhost:5237/rpc
```

**Test 3: Verify OllamaIntegration is running**
```
POST http://localhost:62080/rpc
```

## 🔑 Key Components

### IMEAIInvoker Interface

```csharp
public interface IMEAIInvoker
{
    ValueTask<AIFunction[]> BuildToolListAsync();
}
```

**Purpose:** Abstraction for tool discovery and invocation.

**Implementations:**
- `MEAILocalInvoker` - Local tools
- `MEAIRemoteInvoker` - Remote tools (HTTP)

---

### McpGatewayTool (Custom AIFunction)

```csharp
public sealed class McpGatewayTool(
    ToolDetails toolDetails,
    Func<AIFunctionArguments, CancellationToken, ValueTask<object?>> invokeFunc) 
    : AIFunction
{
    public override string Name => toolDetails.Name;
    public override string Description => toolDetails.Description;
    public override JsonElement JsonSchema => toolDetails.JsonSchema;
    
    protected override ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        return invokeFunc(arguments, cancellationToken);
    }
}
```

**Purpose:** Wraps MCP Gateway tools as `AIFunction` for use with Microsoft.Extensions.AI.

**Benefits:**
- ✅ Works with Ollama, Azure OpenAI, etc.
- ✅ Type-safe (JSON Schema validation)
- ✅ Supports both Local and Remote invocation

---

### tell_ollama Tool

```csharp
[McpTool("tell_ollama",
    Title = "Tell Ollama To Get a secret",
    Description = "Tell Ollama to execute a function to get a secret...")]
public async Task<JsonRpcMessage> TellMEAIOllamaTool(
    TypedJsonRpc<TellRequest> message,
    IChatClient chat,
    IMEAIInvoker meaiInvoker)
{
    // 1. Build tool list (Local or Remote based on config)
    var tools = await meaiInvoker.BuildToolListAsync();
    
    // 2. Chat with Ollama using the tools
    var result = await chat.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System, "You are a helpful assistant..."),
            new ChatMessage(ChatRole.User, "Can you give me a secret?")
        ],
        new ChatOptions { Tools = tools.ToList() }
    );
    
    return ToolResponse.Success(message.Id, new { response = result.Text });
}
```

**Purpose:** Demonstrates how to use `IMEAIInvoker` to call tools via Ollama.

## 🛠️ Troubleshooting

### Port Already in Use

**Error:** `Address already in use: 5237`

**Solution:** Change port in `Properties/launchSettings.json`:
```json
{
  "applicationUrl": "http://localhost:5238"
}
```

---

### Remote Gateway Not Responding

**Error:** `HttpRequestException: Connection refused`

**Solution:**
1. Verify remote gateway is running:
   ```bash
   curl http://localhost:62080/rpc \
     -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'
   ```
2. Check `appsettings.json` RemoteUrl is correct
3. Check firewall settings

---

### Tools Not Found

**Error:** `Method not found: tell_ollama`

**Solution:**
1. Ensure tool class is public
2. Ensure method has `[McpTool]` attribute
3. Restart server after code changes

---

### Ollama Connection Failed

**Error:** `HttpRequestException: Unable to connect to Ollama proxy`

**Solution:**
1. Check Ollama proxy URL in `Program.cs`:
   ```csharp
   const string ollamaUrl = "https://your-ollama-proxy.com";
   ```
2. Verify Ollama is running:
   ```bash
   curl https://your-ollama-proxy.com/api/tags
   ```

## 🎓 Learn More

### Related Examples

- **[OllamaIntegration](../OllamaIntegration/)** - Ollama-specific tools (used as remote gateway)
- **[CalculatorMcpServer](../CalculatorMcpServer/)** - Basic MCP server example
- **[DateTimeMcpServer](../DateTimeMcpServer/)** - Typed parameters example

### Documentation

- [MCP Gateway Documentation](../../docs/)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions)
- [OllamaSharp Documentation](https://github.com/awaescher/OllamaSharp)

## 📊 Comparison: Local vs Remote

| Aspect | Local Invoker | Remote Invoker |
|--------|---------------|----------------|
| **Performance** | ✅ Fast (~1ms) | ⚠️ Slower (~10-50ms) |
| **Setup** | ✅ Simple (DI only) | ⚠️ Requires HttpClient config |
| **Security** | ✅ Implicit (same process) | ⚠️ Needs authentication |
| **Scalability** | ⚠️ Single process | ✅ Multiple instances |
| **Use Case** | Development, testing | Production, distributed |
| **Memory** | ✅ Lower | ⚠️ Higher (HTTP buffers) |
| **Latency** | ✅ ~1ms | ⚠️ ~10-50ms |
| **Tool Updates** | ⚠️ Requires restart | ✅ Dynamic (from remote) |

## 🏆 Best Practices

### 1. Use Configuration-Based Selection

```csharp
var mode = builder.Configuration["MEAIIntegration:Mode"];

if (mode == "Local")
{
    builder.Services.AddScoped<IMEAIInvoker, MEAILocalInvoker>();
}
else if (mode == "Remote")
{
    builder.Services.AddScoped<IMEAIInvoker, MEAIRemoteInvoker>();
}
```

**Benefit:** Easy to switch without code changes.

---

### 2. Add Logging for Debugging

```csharp
public async ValueTask<AIFunction[]> BuildToolListAsync()
{
    Console.WriteLine("🔧 Building tool list (Remote mode)...");
    var tools = await FetchToolsFromRemote();
    Console.WriteLine($"✅ Found {tools.Length} tools");
    return tools;
}
```

**Benefit:** Easy to troubleshoot issues.

---

### 3. Handle Errors Gracefully

```csharp
try
{
    var response = await _httpClient.PostAsJsonAsync(...);
    response.EnsureSuccessStatusCode();
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"❌ Remote gateway error: {ex.Message}");
    return []; // Return empty list on error
}
```

**Benefit:** Prevents crashes on network issues.

---

### 4. Use Timeout Configuration

```csharp
builder.Services.AddHttpClient("MCP", client =>
{
    client.BaseAddress = new Uri("http://localhost:62080");
    client.Timeout = TimeSpan.FromSeconds(30); // ← Important!
});
```

**Benefit:** Prevents hanging on slow networks.

## 📝 License

MIT License - See [LICENSE](../../LICENSE)

---

**Built with ❤️ by ARKo AS - AHelse Development Team**
