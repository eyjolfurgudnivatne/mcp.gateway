namespace MEAIIntegration;

using Mcp.Gateway.Tools;
using Microsoft.Extensions.AI;
using System.Text.Json;

public class MEAILocalInvoker(ToolService toolService, ToolInvoker gatewayInvoker) : IMEAIInvoker
{
    public async ValueTask<AIFunction[]> BuildToolListAsync()
    {
        var tools = toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "http");

        return [.. tools.Items.Select(t => new McpGatewayTool(
            new ToolDetails
            {
                Name = t.Name,
                Description = t.Description,
                JsonSchema = t.InputSchema != null
                    ? JsonDocument.Parse(t.InputSchema).RootElement
                    : JsonDocument.Parse("{\"type\": \"object\", \"properties\": {}}").RootElement,
                ReturnJsonSchema = t.OutputSchema == null ? null
                    : JsonDocument.Parse(t.OutputSchema).RootElement
            },
            async (args, ct) =>
            {
                // Call tool locally via ToolService
                var request = JsonRpcMessage.CreateRequest(
                    t.Name,
                    Guid.NewGuid(),
                    args.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                );

                // Call ToolInvoker DIRECTLY (no HTTP!)
                var result = await gatewayInvoker.InvokeSingleAsync(
                    JsonSerializer.SerializeToElement(request),
                    "http",
                    ct
                );

                // Handle result properly!
                return await ExtractToolResult(result, ct);
            }
        ))];
    }

    /// <summary>
    /// Extracts the actual result from tool invocation.
    /// Handles both sync and async results.
    /// </summary>
    private static async ValueTask<object?> ExtractToolResult(object? result, CancellationToken ct)
    {
        // 1. Handle async Task<JsonRpcMessage>
        if (result is Task<JsonRpcMessage> asyncTask)
        {
            var msgResult = await asyncTask.WaitAsync(ct);  // ← Await the task!
            return ExtractResultFromMessage(msgResult);
        }

        // 2. Handle sync JsonRpcMessage
        if (result is JsonRpcMessage msg)
        {
            return ExtractResultFromMessage(msg);
        }

        // 3. Fallback: return as-is
        return result;
    }

    /// <summary>
    /// Extracts the actual content from MCP message format.
    /// </summary>
    private static object? ExtractResultFromMessage(JsonRpcMessage msg)
    {
        if (msg.Result is null)
        {
            return null;
        }

        // MCP tools/call wraps result in { content: [{ type: "text", text: "..." }] }
        if (msg.Result is JsonElement resultElement)
        {
            // Try to extract from MCP content format
            if (resultElement.TryGetProperty("content", out var content))
            {
                var firstContent = content.EnumerateArray().FirstOrDefault();

                if (firstContent.ValueKind != JsonValueKind.Undefined &&
                    firstContent.TryGetProperty("text", out var text))
                {
                    var textValue = text.GetString();

                    if (!string.IsNullOrEmpty(textValue))
                    {
                        // Try to parse as JSON
                        try
                        {
                            return JsonSerializer.Deserialize<object>(
                                textValue,
                                JsonOptions.Default);
                        }
                        catch (JsonException)
                        {
                            // Not JSON, return as string
                            return textValue;
                        }
                    }
                }
            }

            // No content wrapper, return raw result
            return resultElement;
        }

        // Direct object result
        return msg.Result;
    }
}
