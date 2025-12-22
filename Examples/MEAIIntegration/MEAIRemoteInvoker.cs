namespace MEAIIntegration;

using Mcp.Gateway.Tools;
using Microsoft.Extensions.AI;
using System.Text.Json;

/// <summary>
/// Remote MCP Gateway tool invoker.
/// Calls tools via HTTP client (for distributed scenarios).
/// </summary>
public class MEAIRemoteInvoker(IHttpClientFactory httpClientFactory) : IMEAIInvoker
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("MCP");

    public async ValueTask<AIFunction[]> BuildToolListAsync()
    {
        // 1. Get tools from remote MCP Gateway via tools/list
        var listRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = 1
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/rpc",
            listRequest,
            JsonOptions.Default);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(
            JsonOptions.Default);

        if (json?.Result is null)
        {
            return [];
        }

        // 2. Parse tools list from result
        var toolsJson = JsonSerializer.SerializeToElement(json.Result, JsonOptions.Default);
        
        if (!toolsJson.TryGetProperty("tools", out var toolsArray))
        {
            return [];
        }

        var tools = new List<AIFunction>();

        // 3. Convert each tool to McpGatewayTool
        foreach (var tool in toolsArray.EnumerateArray())
        {
            var toolDetails = new ToolDetails
            {
                Name = tool.GetProperty("name").GetString()!,
                Description = tool.GetProperty("description").GetString() ?? "",
                JsonSchema = tool.GetProperty("inputSchema"),
                ReturnJsonSchema = tool.TryGetProperty("outputSchema", out var outSchema)
                    ? outSchema
                    : null
            };

            // 4. Create AIFunction with remote invoke
            tools.Add(new McpGatewayTool(
                toolDetails,
                async (args, ct) =>
                {
                    // Call remote tool via HTTP
                    var toolRequest = new
                    {
                        jsonrpc = "2.0",
                        method = "tools/call",
                        @params = new
                        {
                            name = toolDetails.Name,
                            arguments = args.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                        },
                        id = Guid.NewGuid()
                    };

                    var toolResponse = await _httpClient.PostAsJsonAsync(
                        "/rpc",
                        toolRequest,
                        JsonOptions.Default,
                        ct);

                    toolResponse.EnsureSuccessStatusCode();

                    var result = await toolResponse.Content.ReadFromJsonAsync<JsonRpcMessage>(
                        JsonOptions.Default,
                        ct);

                    // Extract result, unwrapping MCP content format if present
                    return ExtractResultFromMessage(result);
                }
            ));
        }

        return [.. tools];
    }

    /// <summary>
    /// Extracts the actual content from MCP message format.
    /// Remote MCP Gateway wraps results in { content: [{ type: "text", text: "..." }] }
    /// </summary>
    private static object? ExtractResultFromMessage(JsonRpcMessage? msg)
    {
        if (msg?.Result is null)
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
