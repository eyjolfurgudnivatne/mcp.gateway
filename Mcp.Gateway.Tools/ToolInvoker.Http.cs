namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// ToolInvoker partial class - HTTP transport (v1.5.0)
/// </summary>
public partial class ToolInvoker
{
    /// <summary>
    /// Invokes a JSON-RPC request over HTTP.
    /// Supports single requests and batch requests.
    /// </summary>
    public async Task<IResult> InvokeHttpRpcAsync(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var doc = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);

            // Detect transport for capability-based filtering
            var transport = DetectTransport(request.HttpContext);

            // Batch request (array)
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var responses = new List<object>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var response = await InvokeSingleAsync(element, transport, cancellationToken);
                    if (response is not null)
                        responses.Add(response);
                }

                // If all were notifications (no responses), return 204 No Content
                if (responses.Count == 0)
                    return Results.NoContent();

                return Results.Json(responses, JsonOptions.Default);
            }
            // Single request
            else
            {
                var response = await InvokeSingleAsync(doc.RootElement, transport, cancellationToken);
                
                // Notification (no response)
                if (response is null)
                    return Results.NoContent();

                return Results.Json(response, JsonOptions.Default);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parse error in HTTP RPC request");
            return Results.Json(
                ToolResponse.Error(null, -32700, "Parse error", new { detail = ex.Message }),
                JsonOptions.Default,
                statusCode: 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in HTTP RPC request");
            return Results.Json(
                ToolResponse.Error(null, -32603, "Internal error", new { detail = ex.Message }),
                JsonOptions.Default,
                statusCode: 500);
        }
    }
}
