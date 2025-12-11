namespace Mcp.Gateway.Examples.OllamaIntegration;

using Mcp.Gateway.Tools;
using OllamaSharp.Models.Chat;
using OllamaSharp.Tools;
using System.Text.Json;

public class DirectToolInvoker(ToolInvoker gatewayInvoker) : IToolInvoker
{
    public async Task<ToolResult> InvokeAsync(
        Message.ToolCall toolCall,
        IEnumerable<object> tools,
        CancellationToken ct)
    {
        // Convert tool call to JSON-RPC message
        var jsonRpcRequest = JsonRpcMessage.CreateRequest(
            toolCall.Function?.Name ?? "unknown",
            toolCall.Id,
            ConvertArgumentsToJsonElement(toolCall.Function?.Arguments)
        );

        // Call ToolInvoker DIRECTLY (no HTTP!)
        var response = await gatewayInvoker.InvokeSingleAsync(
            JsonSerializer.SerializeToElement(jsonRpcRequest),
            "http",
            ct
        );

        // Convert response to ToolResult
        var result = response is JsonRpcMessage msg ? msg.Result : response;

        var matchingToolElement = (JsonElement)tools.FirstOrDefault(t =>
        {
            var element = (JsonElement)t;
            return element.GetProperty("function").GetProperty("name").GetString()
                   == toolCall.Function?.Name;
        })!;

        var matchingTool = JsonSerializer.Deserialize<Tool>(matchingToolElement.GetRawText());

        return new ToolResult(
            Tool: matchingTool!,
            ToolCall: toolCall,
            Result: result
        );
    }

    private JsonElement? ConvertArgumentsToJsonElement(IDictionary<string, object?>? args)
    {
        if (args == null) return null;
        var json = JsonSerializer.Serialize(args);
        return JsonDocument.Parse(json).RootElement;
    }
}
