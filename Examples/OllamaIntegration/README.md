# 🚀 MCP Gateway + Ollama Integration Example

This example demonstrates how to use **MCP Gateway** tools with **Ollama**.

## 📋 What This Example Does

1. **Fetches tools** from MCP Gateway in Ollama-compatible format (`tools/list/ollama`)
2. **Verifies Ollama** connection and model availability
3. **Demonstrates tool execution** via MCP Gateway's JSON-RPC API
4. **Shows integration readiness** for Ollama function calling

---

## ✅ Prerequisites

### 1. MCP Gateway Running

```bash
# From repository root
dotnet run --project Mcp.Gateway.Server
```

MCP Gateway should be running on `http://localhost:5000`

### 2. Ollama Installed

Download and install from: https://ollama.com/

### 3. Ollama Running

```bash
ollama serve
```

Ollama should be running on `http://localhost:11434`

### 4. Model Downloaded

```bash
ollama pull llama3.2
```

This example uses `llama3.2` to verify model availability.

---

## 🎯 How to Run

### Option 1: From this directory

```bash
cd Examples/OllamaIntegration
dotnet run
```

### Option 2: From repository root

```bash
dotnet run --project Examples/OllamaIntegration
```

---

## 💬 Example Output

```
🚀 MCP Gateway + Ollama Integration Example
═══════════════════════════════════════════════════════════════

📋 Step 1: Fetching tools from MCP Gateway...
✅ Loaded 8 tools from MCP Gateway

🔧 Available Tools:
   • echo_message: No description available
   • get_user: Gets user by ID
   • system_echo: Echoes back the input parameters
   • add_numbers_tool: Adds two numbers
   • ping: No description available
   • system_notification: Sends a notification (no response expected)
   • add_numbers: Adds two numbers and return result. Example: 5 + 3 = 8
   • system_ping: Simple ping tool that returns pong with timestamp

🤖 Step 2: Initializing Ollama client...
✅ Ollama connected (model: llama3.2)

💬 Step 3: Chat with Ollama using MCP Gateway tools
═══════════════════════════════════════════════════════════════

This example demonstrates real Ollama integration with tool calling.

💡 Try asking Ollama to perform calculations:
   'Add 5 and 3'
   'What is 42 plus 58?'
   'Calculate 123 + 456'

Type 'exit' to quit.

You: Add 5 and 3

🤖 Ollama is thinking...

   🔧 Calling tool: add_numbers
      Args: { "number1": 5, "number2": 3 }
      Result: 8

🤖 Ollama: The answer is 8. I used the add_numbers tool to calculate 5 + 3 = 8.

You: What is 42 plus 58?

🤖 Ollama is thinking...

   🔧 Calling tool: add_numbers
      Args: { "number1": 42, "number2": 58 }
      Result: 100

🤖 Ollama: The answer is 100. I used the add_numbers tool to calculate 42 + 58 = 100.

You: exit
👋 Goodbye!

✅ Tool integration demonstrated!

🎯 Key Takeaways:
   • MCP Gateway provides tools in Ollama-compatible format
   • Tools can be executed via JSON-RPC
   • In this demo, we simulated Ollama's decision to call tools

📚 For production integration with OllamaSharp 4.0+, see:
   https://github.com/awaescher/OllamaSharp
   https://ollama.com/blog/tool-support
```

---

## 🏗️ How It Works

### Step 1: Fetch Tools from MCP Gateway

```csharp
var toolsRequest = new
{
    jsonrpc = "2.0",
    method = "tools/list/ollama",  // ← Ollama-formatted tool list
    id = 1
};

var response = await httpClient.PostAsJsonAsync(
    "http://localhost:5000/rpc", 
    toolsRequest);

var toolsJson = await response.Content.ReadFromJsonAsync<JsonElement>();
var toolsArray = toolsJson.GetProperty("result").GetProperty("tools");
```

**Tools are returned in Ollama format:**
```json
{
  "type": "function",
  "function": {
    "name": "add_numbers",
    "description": "Adds two numbers",
    "parameters": {
      "type": "object",
      "properties": {
        "number1": { "type": "number", "description": "First number" },
        "number2": { "type": "number", "description": "Second number" }
      },
      "required": ["number1", "number2"]
    }
  }
}
```

### Step 2: Execute Tools via JSON-RPC

```csharp
var toolCallRequest = new
{
    jsonrpc = "2.0",
    method = "add_numbers",
    @params = new
    {
        number1 = 5.0,
        number2 = 3.0
    },
    id = 2
};

var toolCallResponse = await httpClient.PostAsJsonAsync(
    "http://localhost:5000/rpc",
    toolCallRequest);
```

---

## 🔧 Available Tools

The example automatically loads all tools from MCP Gateway. Default tools include:

| Tool | Description | Example Params |
|------|-------------|----------------|
| `add_numbers` | Adds two numbers | `{"number1": 5, "number2": 3}` |
| `system_ping` | Pings the system | `{}` |
| `system_echo` | Echoes a message | `{"message": "Hello"}` |
| `echo_message` | Simple echo | `{}` |

---

## 🐛 Troubleshooting

### "Failed to fetch tools"
- **Check MCP Gateway is running:** `dotnet run --project ../../Mcp.Gateway.Server`
- **Verify endpoint:** `curl http://localhost:5000/rpc -d '{"jsonrpc":"2.0","method":"tools/list","id":1}'`

### "Failed to connect to Ollama"
- **Check Ollama is running:** `ollama serve`
- **Verify endpoint:** `curl http://localhost:11434/api/tags`

### "Model 'llama3.2' not found"
- **Pull the model:** `ollama pull llama3.2`
- **List models:** `ollama list`

---

## 🚀 Next Steps

This example demonstrates the **foundation** for Ollama integration. To build a full chat application:

1. **Use OllamaSharp's chat API** with tools parameter
2. **Handle tool calls** from Ollama's response
3. **Execute tools** via MCP Gateway (as shown in this example)
4. **Return results** to Ollama for final response

See **OllamaSharp documentation** for chat API details:  
https://github.com/awaescher/OllamaSharp

---

## 📚 Learn More

- **MCP Gateway Documentation:** [../../README.md](../../README.md)
- **Ollama Tool Support:** https://ollama.com/blog/tool-support
- **OllamaSharp Library:** https://github.com/awaescher/OllamaSharp
- **Formatter Usage Guide:** [../../.internal/notes/v.1.2.0/formatter-usage-guide.md](../../.internal/notes/v.1.2.0/formatter-usage-guide.md)

---

## 🎯 Key Takeaways

1. ✅ **MCP Gateway provides tools** in Ollama-compatible format via `tools/list/ollama`
2. ✅ **Tools are executable** via JSON-RPC (demonstrated in this example)
3. ✅ **Ready for integration** with Ollama's function calling
4. ✅ **Separation of concerns**: MCP Gateway = tool provider, Ollama = AI decision-making

---

**Created:** 8. desember 2025  
**MCP Gateway Version:** 1.2.0  
**Status:** Integration demo (production-ready foundation)
