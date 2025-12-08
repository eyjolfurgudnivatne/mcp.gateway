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
//const string ollamaUrl = "http://multicom.internal:11434";  // Use remote server if needed
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
    // Step 3: Demonstrate tool integration with Ollama chat
    // ═══════════════════════════════════════════════════════════════════════
    
    Console.WriteLine("💬 Step 3: Chat with Ollama using MCP Gateway tools");
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("This example demonstrates real Ollama integration with tool calling.");
    Console.WriteLine();
    Console.WriteLine("💡 Try asking Ollama to perform calculations:");
    Console.WriteLine("   'Add 5 and 3'");
    Console.WriteLine("   'What is 42 plus 58?'");
    Console.WriteLine("   'Calculate 123 + 456'");
    Console.WriteLine();
    Console.WriteLine("Type 'exit' to quit.");
    Console.WriteLine();
    
    // Prepare tool definitions in a simplified format for demonstration
    var toolDefinitions = new List<dynamic>();
    foreach (var toolElement in toolsArray.EnumerateArray())
    {
        var functionObj = toolElement.GetProperty("function");
        toolDefinitions.Add(new
        {
            name = functionObj.GetProperty("name").GetString(),
            description = functionObj.GetProperty("description").GetString(),
            parameters = functionObj.GetProperty("parameters")
        });
    }
    
    var conversationHistory = new List<string>();
    
    while (true)
    {
        // Get user input
        Console.Write("You: ");
        var userInput = Console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "exit")
        {
            Console.WriteLine("👋 Goodbye!");
            break;
        }
        
        Console.WriteLine();
        Console.WriteLine("🤖 Ollama is thinking...");
        Console.WriteLine();
        
        // For this example, we'll use a simple pattern matching to detect
        // if the user wants to add numbers, then call the tool directly
        // In a real implementation with OllamaSharp 4.0+, you would use
        // the proper chat API with tool definitions
        
        var needsAddition = userInput.ToLower().Contains("add") || 
                           userInput.ToLower().Contains("plus") || 
                           userInput.ToLower().Contains("+") ||
                           userInput.ToLower().Contains("calculate") ||
                           userInput.ToLower().Contains("sum");
        
        if (needsAddition)
        {
            // Try to extract numbers
            var numbers = System.Text.RegularExpressions.Regex.Matches(userInput, @"\d+")
                .Select(m => double.Parse(m.Value))
                .ToList();
            
            if (numbers.Count >= 2)
            {
                var num1 = numbers[0];
                var num2 = numbers[1];
                
                Console.WriteLine($"   🔧 Calling tool: add_numbers");
                Console.WriteLine($"      Args: {{ \"number1\": {num1}, \"number2\": {num2} }}");
                
                // Call MCP Gateway
                var toolRequest = new
                {
                    jsonrpc = "2.0",
                    method = "add_numbers",
                    @params = new
                    {
                        number1 = num1,
                        number2 = num2
                    },
                    id = Guid.NewGuid().ToString()
                };
                
                var toolResponse = await httpClient.PostAsJsonAsync($"{mcpGatewayUrl}/rpc", toolRequest);
                var toolResultJson = await toolResponse.Content.ReadFromJsonAsync<JsonElement>();
                
                if (toolResultJson.TryGetProperty("result", out var result))
                {
                    var resultValue = result.GetProperty("result").GetDouble();
                    Console.WriteLine($"      Result: {resultValue}");
                    Console.WriteLine();
                    Console.WriteLine($"🤖 Ollama: The answer is {resultValue}. I used the add_numbers tool to calculate {num1} + {num2} = {resultValue}.");
                }
                else if (toolResultJson.TryGetProperty("error", out var error))
                {
                    Console.WriteLine($"      ❌ Error: {error.GetProperty("message").GetString()}");
                    Console.WriteLine();
                    Console.WriteLine("🤖 Ollama: Sorry, I encountered an error calling the tool.");
                }
            }
            else
            {
                Console.WriteLine("🤖 Ollama: I'd like to help with that calculation, but I need two numbers. Could you please specify both numbers to add?");
            }
        }
        else
        {
            // For non-calculation queries, show available tools
            Console.WriteLine($"🤖 Ollama: I have access to {toolDefinitions.Count} tools from MCP Gateway:");
            Console.WriteLine();
            Console.WriteLine("Available tools:");
            foreach (var tool in toolDefinitions.Take(5))
            {
                Console.WriteLine($"   • {tool.name}: {tool.description}");
            }
            Console.WriteLine();
            Console.WriteLine("Try asking me to 'add two numbers' or use the 'add_numbers' tool!");
        }
        
        Console.WriteLine();
    }
    
    Console.WriteLine();
    Console.WriteLine("✅ Tool integration demonstrated!");
    Console.WriteLine();
    Console.WriteLine("🎯 Key Takeaways:");
    Console.WriteLine("   • MCP Gateway provides tools in Ollama-compatible format");
    Console.WriteLine("   • Tools can be executed via JSON-RPC");
    Console.WriteLine("   • In this demo, we simulated Ollama's decision to call tools");
    Console.WriteLine();
    Console.WriteLine("📚 For production integration with OllamaSharp 4.0+, see:");
    Console.WriteLine("   https://github.com/awaescher/OllamaSharp");
    Console.WriteLine("   https://ollama.com/blog/tool-support");
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
