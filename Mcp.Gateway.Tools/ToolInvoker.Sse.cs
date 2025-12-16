namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// ToolInvoker partial class - Server-Sent Events (SSE) transport (v1.5.0)
/// </summary>
public partial class ToolInvoker
{
    /// <summary>
    /// Invokes JSON-RPC request over SSE (Server-Sent Events).
    /// Handles MCP protocol over HTTP with SSE transport.
    /// </summary>
    public async Task InvokeSseAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        // Set SSE headers
        context.Response.ContentType = "text/event-stream; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";
        context.Response.Headers["X-Accel-Buffering"] = "no"; // Disable nginx buffering

        try
        {
            await context.Response.StartAsync(cancellationToken);

            // Read JSON-RPC request from POST body
            using var doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: cancellationToken);

            // Log incoming request
            var requestJson = JsonSerializer.Serialize(doc.RootElement, JsonOptions.Default);
            _logger.LogDebug("SSE Request: {Request}", requestJson);

            // Batch request (array)
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var response = await InvokeSingleAsync(element, "sse", cancellationToken);
                    if (response != null)
                    {
                        // Log outgoing response
                        var responseJson = JsonSerializer.Serialize(response, JsonOptions.Default);
                        _logger.LogDebug("SSE Response: {Response}", responseJson);
                        
                        await SendSseEventAsync(context.Response, response, cancellationToken);
                    }
                }
            }
            // Single request
            else
            {
                var response = await InvokeSingleAsync(doc.RootElement, "sse", cancellationToken);
                if (response != null)
                {
                    // Log outgoing response
                    var responseJson = JsonSerializer.Serialize(response, JsonOptions.Default);
                    _logger.LogDebug("SSE Response: {Response}", responseJson);
                    
                    await SendSseEventAsync(context.Response, response, cancellationToken);
                }
            }

            // Send completion event - signals end of this SSE response
            await context.Response.WriteAsync("event: done\ndata: {}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parse error in SSE request");
            
            var error = ToolResponse.Error(null, -32700, "Parse error", new { detail = ex.Message });
            await SendSseEventAsync(context.Response, error, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SSE request");
            
            var error = ToolResponse.Error(null, -32603, "Internal error", new { detail = ex.Message });
            await SendSseEventAsync(context.Response, error, cancellationToken);
        }
    }

    /// <summary>
    /// Sends a JSON-RPC response as an SSE event.
    /// </summary>
    private static async Task SendSseEventAsync(
        HttpResponse response,
        object data,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions.Default);
        
        // SSE format: "data: {json}\n\n"
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
