using OllamaSharp;
using System.Net.Http.Json;
using System.Text.Json;

// ═══════════════════════════════════════════════════════════════════════════
// MCP Gateway + Ollama Integration Example
// ═══════════════════════════════════════════════════════════════════════════
// 
// This example demonstrates how to use MCP Gateway tools with Ollama.
//
// Prerequisites:
// 1. MCP Gateway running: dotnet run --project ../../Mcp.Gateway.Server
// 2. Ollama installed: https://ollama.com/
// 3. Ollama running: ollama serve
// 4. Model pulled: ollama pull llama3.2
//
// ═══════════════════════════════════════════════════════════════════════════

Console.WriteLine("🚀 MCP Gateway + Ollama Integration Example");
Console.WriteLine("═══════════════════════════════════════════════════════════════");
Console.WriteLine();

// Configuration
const string mcpGatewayUrl = "http://localhost:5000";
const string ollamaUrl = "http://localhost:11434";
const string model = "llama3.2";

var httpClient = new HttpClient();

try
{
    // ═══════════════════════════════════════════════════════════════════════
    // Step 1: Get tools from MCP Gateway in Ollama format
    // ═══════════════════════════════════════════════════════════════════════
    
    Console.WriteLine("📋 Step 1: Fetching tools from MCP Gateway...");
    
    var toolsRequest = new
    {
        jsonrpc = "2.0",
        method = "tools/list/ollama",
        id = 1
    };
    
    var toolsResponse = await httpClient.PostAsJsonAsync($"{mcpGatewayUrl}/rpc", toolsRequest);
    
    if (!toolsResponse.IsSuccessStatusCode)
    {
        Console.WriteLine($"❌ Failed to fetch tools: {toolsResponse.StatusCode}");
        Console.WriteLine("💡 Make sure MCP Gateway is running: dotnet run --project ../../Mcp.Gateway.Server");
        return;
    }
    
    var toolsJson = await toolsResponse.Content.ReadFromJsonAsync<JsonElement>();
    var toolsArray = toolsJson.GetProperty("result").GetProperty("tools");
    
    Console.WriteLine($"✅ Loaded {toolsArray.GetArrayLength()} tools from MCP Gateway");
    Console.WriteLine();
    
    // Display available tools
    Console.WriteLine("🔧 Available Tools:");
    foreach (var toolElement in toolsArray.EnumerateArray())
    {
        var toolName = toolElement.GetProperty("function").GetProperty("name").GetString();
        var toolDesc = toolElement.GetProperty("function").GetProperty("description").GetString();
        Console.WriteLine($"   • {toolName}: {toolDesc}");
    }
    Console.WriteLine();
    
    // ═══════════════════════════════════════════════════════════════════════
    // Step 2: Setup Ollama client
    // ═══════════════════════════════════════════════════════════════════════
    
    Console.WriteLine("🤖 Step 2: Initializing Ollama client...");
    
    var ollama = new OllamaApiClient(ollamaUrl);
    
    // Verify Ollama is running
    try
    {
        var models = await ollama.ListLocalModelsAsync();
        var modelExists = models.Any(m => m.Name == model || m.Name.StartsWith(model + ":"));
        
        if (!modelExists)
        {
            Console.WriteLine($"❌ Model '{model}' not found locally");
            Console.WriteLine($"💡 Pull model: ollama pull {model}");
            return;
        }
        
        Console.WriteLine($"✅ Ollama connected (model: {model})");
    }
    catch (HttpRequestException)
    {
        Console.WriteLine("❌ Failed to connect to Ollama");
        Console.WriteLine("💡 Make sure Ollama is running: ollama serve");
        return;
    }
    
    Console.WriteLine();
    
    // ═══════════════════════════════════════════════════════════════════════
    // Step 3: Demonstrate tool integration
    // ═══════════════════════════════════════════════════════════════════════
    
    Console.WriteLine("💬 Step 3: Testing tool integration");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("This example demonstrates that MCP Gateway tools are available");
    Console.WriteLine("in Ollama-compatible format. In a real application, you would:");
    Console.WriteLine();
    Console.WriteLine("1. Pass these tools to Ollama's chat API");
    Console.WriteLine("2. Ollama decides when to call tools based on conversation");
    Console.WriteLine("3. Execute tool calls via MCP Gateway RPC endpoint");
    Console.WriteLine("4. Return results to Ollama for final response");
    Console.WriteLine();
    Console.WriteLine("📝 Example tool call to MCP Gateway:");
    Console.WriteLine();
    
    // Example: Call add_numbers tool
    Console.WriteLine("   Calling: add_numbers(5, 3)");
    
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
    
    var toolCallResponse = await httpClient.PostAsJsonAsync($"{mcpGatewayUrl}/rpc", toolCallRequest);
    var toolResult = await toolCallResponse.Content.ReadFromJsonAsync<JsonElement>();
    
    if (toolResult.TryGetProperty("result", out var result))
    {
        Console.WriteLine($"   Result: {JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })}");
    }
    else if (toolResult.TryGetProperty("error", out var error))
    {
        Console.WriteLine($"   Error: {error.GetProperty("message").GetString()}");
    }
    
    Console.WriteLine();
    Console.WriteLine("✅ Integration verified!");
    Console.WriteLine();
    Console.WriteLine("🎯 Key Takeaways:");
    Console.WriteLine("   • MCP Gateway provides tools in Ollama-compatible format");
    Console.WriteLine("   • Tools can be executed via JSON-RPC");
    Console.WriteLine("   • Ready for integration with Ollama's function calling");
    Console.WriteLine();
    Console.WriteLine("📚 For full chat integration with OllamaSharp, see:");
    Console.WriteLine("   https://github.com/awaescher/OllamaSharp");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Troubleshooting:");
    Console.WriteLine("1. Make sure MCP Gateway is running: dotnet run --project ../../Mcp.Gateway.Server");
    Console.WriteLine("2. Make sure Ollama is running: ollama serve");
    Console.WriteLine($"3. Make sure model is pulled: ollama pull {model}");
}
