namespace Mcp.Gateway.Examples.OllamaIntegration.Tools;

using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Formatters;
using OllamaSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

public class TellOllama
{
    public sealed record TellRequest(
        [property: JsonPropertyName("format")] string? Format);

    public sealed record TellResponse(
        [property: JsonPropertyName("response")] string Response);

    [McpTool("tell_ollama",
        Title = "Tell Ollama To Get a secret",
        Description = "Tell Ollama to execute a function to get a secret in guid, hex or base64 format.",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""format"":{
                    ""type"":""string"",
                    ""description"":""Format of the secret. Options: 'guid' (default), 'hex', 'base64'"",
                    ""enum"":[""guid"",""hex"",""base64""]
                }
            }
        }")]
    public async Task<JsonRpcMessage> TellOllamaTool(
        JsonRpcMessage message,
        ToolService toolService,
        ToolInvoker toolInvoker)
    {
        // Get format parameter (default to guid)
        var tellRequest = message.GetParams<TellRequest>();
        string format = tellRequest?.Format ?? "guid";

        const string ollamaUrl = "http://localhost:11434";
        // const string ollamaUrl = "http://multicom.internal:11434";  // Use remote server if needed
        const string model = "llama3.2";

        // 1. Get tools DIRECTLY from ToolService (no HttpClient!)
        var tools = toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "http");

        // Format tools for Ollama
        var ollamaFormatter = new OllamaToolListFormatter();
        var formattedTools = ollamaFormatter.FormatToolList(tools.Items);

        // Extract tools array
        var toolsJson = JsonSerializer.Serialize(formattedTools);
        var toolsDoc = JsonDocument.Parse(toolsJson);
        var toolsArray = toolsDoc.RootElement.GetProperty("tools");

        var ollama = new OllamaApiClient(ollamaUrl);

        // Verify Ollama is running
        try
        {
            var connected = await ollama.IsRunningAsync();
            var models = await ollama.ListLocalModelsAsync();
            var modelExists = models.Any(m => m.Name == model || m.Name.StartsWith(model + ":"));

            if (!modelExists)
            {
                Console.WriteLine($"Model '{model}' not found locally");
                Console.WriteLine($"Pull model: ollama pull {model}");
                throw new ToolInternalErrorException($"Model '{model}' not found locally");
            }

            Console.WriteLine($"Ollama connected (model: {model})");
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("Failed to connect to Ollama");
            Console.WriteLine("Make sure Ollama is running: ollama serve");
            throw new ToolInternalErrorException($"Failed to connect to Ollama. Make sure Ollama is running: ollama serve");
        }
        ollama.SelectedModel = model;

        var systemPrompt = "You are Ollama, an AI assistant integrated with MCP Gateway tools. " +
                   "You can call tools to help answer user questions. " +
                   "When a user asks a question that requires a tool, decide which tool to call (NOT tell_ollama) and provide the necessary parameters. " +
                   "After calling a tool, use its result to formulate your response to the user.";

        // 3. Create DirectToolInvoker (no HTTP!)
        var directInvoker = new DirectToolInvoker(toolInvoker);

        var chat = new Chat(ollama, systemPrompt)
        {
            ToolInvoker = directInvoker
        };

        string result = "";
        var userInput = $"Can you give me a random secret {format} string?";

        await foreach (var answerToken in chat.SendAsync(userInput, tools: toolsArray.EnumerateArray().Cast<object>()))
            result += $"{answerToken}";

        return ToolResponse.Success(message.Id, new TellResponse(result));
    }
}
