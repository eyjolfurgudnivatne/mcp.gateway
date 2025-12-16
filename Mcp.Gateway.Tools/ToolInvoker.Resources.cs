namespace Mcp.Gateway.Tools;

using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// ToolInvoker partial class - MCP Resources support (v1.5.0)
/// </summary>
public partial class ToolInvoker
{
    /// <summary>
    /// Handles MCP resources/list request.
    /// Returns all available resources with their metadata.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <returns>JSON-RPC response with resources list</returns>
    private JsonRpcMessage HandleResourcesList(JsonRpcMessage request)
    {
        try
        {
            var resources = _toolService.GetAllResourceDefinitions();
            
            var resourcesList = resources.Select(r => new
            {
                uri = r.Uri,
                name = r.Name,
                description = r.Description,
                mimeType = r.MimeType
            }).ToList();

            return ToolResponse.Success(request.Id, new
            {
                resources = resourcesList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in resources/list");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Handles MCP resources/read request.
    /// Reads the content of a specific resource by URI.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>JSON-RPC response with resource content</returns>
    private async Task<JsonRpcMessage> HandleResourcesReadAsync(
        JsonRpcMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestParams = request.GetParams();
            var uri = requestParams.GetProperty("uri").GetString();
            
            if (string.IsNullOrEmpty(uri))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
            }

            // Get resource definition for MIME type
            ResourceDefinition resourceDef;
            try
            {
                resourceDef = _toolService.GetResourceDefinition(uri);
            }
            catch (ToolNotFoundException)
            {
                return ToolResponse.Error(request.Id, -32601, "Resource not found", new { detail = $"Resource '{uri}' is not configured" });
            }

            // Build resource request (similar to tools/call)
            var resourceRequest = JsonRpcMessage.CreateRequest("resource_read", request.Id, new { uri });

            // Invoke resource method
            var result = _toolService.InvokeResourceDelegate(uri, resourceRequest);

            // Process result (sync or async)
            var processedResult = await ProcessResourceResultAsync(result, cancellationToken);

            // Extract content
            object? resultData = null;
            if (processedResult is JsonRpcMessage msg)
            {
                resultData = msg.Result;
            }
            else
            {
                resultData = processedResult;
            }

            // Parse as ResourceContent if possible
            ResourceContent? content = null;
            if (resultData != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(resultData, JsonOptions.Default);
                    content = JsonSerializer.Deserialize<ResourceContent>(json, JsonOptions.Default);
                }
                catch
                {
                    // If not ResourceContent, wrap in one
                    var textContent = resultData.ToString() ?? "";
                    content = new ResourceContent(
                        Uri: uri,
                        MimeType: resourceDef.MimeType,
                        Text: textContent
                    );
                }
            }

            // Wrap in MCP resources/read format
            return ToolResponse.Success(request.Id, new
            {
                contents = new[]
                {
                    new
                    {
                        uri = content?.Uri ?? uri,
                        mimeType = content?.MimeType ?? resourceDef.MimeType ?? "text/plain",
                        text = content?.Text
                    }
                }
            });
        }
        catch (ToolNotFoundException ex)
        {
            return ToolResponse.Error(request.Id, -32601, "Resource not found", new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in resources/read");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Processes resource method results (sync or async).
    /// </summary>
    private static async Task<object?> ProcessResourceResultAsync(
        object? result,
        CancellationToken cancellationToken)
    {
        // Task<JsonRpcMessage>
        if (result is Task<JsonRpcMessage> jsonRpcTask)
        {
            return await jsonRpcTask.ConfigureAwait(false);
        }

        // Task<object?>
        if (result is Task<object?> objectTask)
        {
            return await objectTask.ConfigureAwait(false);
        }

        // Task (void) - shouldn't happen for resources
        if (result is Task voidTask)
        {
            await voidTask.ConfigureAwait(false);
            return null;
        }

        // JsonRpcMessage (sync)
        if (result is JsonRpcMessage jsonRpcMessage)
        {
            return jsonRpcMessage;
        }

        // Regular sync return
        return result;
    }
}
