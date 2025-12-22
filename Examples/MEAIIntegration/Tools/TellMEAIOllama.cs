namespace MEAIIntegration.Tools;

using Mcp.Gateway.Tools;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text.Json.Serialization;

public class TellMEAIOllama
{
    public enum SecretFormat
    {
        Guid,
        Hex,
        Base64
    }

    public sealed record TellRequest(
        [property: JsonPropertyName("format")]
        [property: Description("Format of the secret. Options: 'guid' (default), 'hex', 'base64'")] SecretFormat? Format);

    public sealed record TellResponse(
        [property: JsonPropertyName("response")] string Response);

    [McpTool("tell_ollama",
        Title = "Tell Ollama To Get a secret",
        Description = "Tell Ollama to execute a function to get a secret in guid, hex or base64 format.")]
    public async Task<JsonRpcMessage> TellMEAIOllamaTool(
        TypedJsonRpc<TellRequest> message,
        IChatClient chat,
        IMEAIInvoker meaiInvoker)
    {
        // Get format parameter (default to guid)
        var tellRequest = message.GetParams();
        string format = tellRequest?.Format?.ToString() ?? "guid";

        var systemPrompt = "You are Ollama, an AI assistant integrated with MCP Gateway tools. " +
                   "You can call tools to help answer user questions. " +
                   "When a user asks a question that requires a tool, decide which tool to call (NOT tell_ollama) and provide the necessary parameters. " +
                   "After calling a tool, use its result to formulate your response to the user.";

        var userInput = $"Can you give me a random secret {format} string?";

        var result = await chat.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, "You are a assistant helping the developer to debug a tool package. You will return the exact result of the tool response. IMPORTANT: If tool do not respond properly, say so. Do not hallusinate!"),
                new ChatMessage(ChatRole.User, userInput)
            ],
            new ChatOptions()
            {
                Tools = [..await meaiInvoker.BuildToolListAsync()]
            }
        );

        return ToolResponse.Success(message.Id, new TellResponse(result.Text));
    }
}
