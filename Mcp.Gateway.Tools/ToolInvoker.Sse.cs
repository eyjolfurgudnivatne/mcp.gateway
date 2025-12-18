namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// ToolInvoker partial class - Server-Sent Events (SSE) transport (v1.5.0+)
/// Updated for MCP 2025-11-25 with event IDs (v1.7.0)
/// </summary>
public partial class ToolInvoker
{
    private readonly EventIdGenerator? _eventIdGenerator;
    
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
                        
                        // Generate event ID and send
                        var eventId = _eventIdGenerator?.GenerateEventId() ?? string.Empty;
                        var sseEvent = SseEventMessage.CreateMessage(eventId, response);
                        await SendSseEventAsync(context.Response, sseEvent, cancellationToken);
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
                    
                    // Generate event ID and send
                    var eventId = _eventIdGenerator?.GenerateEventId() ?? string.Empty;
                    var sseEvent = SseEventMessage.CreateMessage(eventId, response);
                    await SendSseEventAsync(context.Response, sseEvent, cancellationToken);
                }
            }

            // Send completion event - signals end of this SSE response
            var doneEventId = _eventIdGenerator?.GenerateEventId() ?? string.Empty;
            var doneEvent = SseEventMessage.CreateDone(doneEventId);
            await SendSseEventAsync(context.Response, doneEvent, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parse error in SSE request");
            
            var error = ToolResponse.Error(null, -32700, "Parse error", new { detail = ex.Message });
            var eventId = _eventIdGenerator?.GenerateEventId() ?? string.Empty;
            var errorEvent = SseEventMessage.CreateError(eventId, new JsonRpcError(-32700, "Parse error", new { detail = ex.Message }));
            await SendSseEventAsync(context.Response, errorEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SSE request");
            
            var error = ToolResponse.Error(null, -32603, "Internal error", new { detail = ex.Message });
            var eventId = _eventIdGenerator?.GenerateEventId() ?? string.Empty;
            var errorEvent = SseEventMessage.CreateError(eventId, new JsonRpcError(-32603, "Internal error", new { detail = ex.Message }));
            await SendSseEventAsync(context.Response, errorEvent, cancellationToken);
        }
    }

    /// <summary>
    /// Sends an SSE event with proper formatting (v1.7.0+).
    /// Supports event IDs, event types, and retry intervals.
    /// </summary>
    private static async Task SendSseEventAsync(
        HttpResponse response,
        SseEventMessage message,
        CancellationToken cancellationToken)
    {
        // Write event ID (if present)
        if (!string.IsNullOrEmpty(message.Id))
        {
            await response.WriteAsync($"id: {message.Id}\n", cancellationToken);
        }
        
        // Write event type (if present, defaults to "message")
        if (!string.IsNullOrEmpty(message.Event))
        {
            await response.WriteAsync($"event: {message.Event}\n", cancellationToken);
        }
        
        // Write retry interval (for polling, optional)
        if (message.Retry.HasValue)
        {
            await response.WriteAsync($"retry: {message.Retry.Value}\n", cancellationToken);
        }
        
        // Write data
        var json = JsonSerializer.Serialize(message.Data, JsonOptions.Default);
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
    
    /// <summary>
    /// Legacy method for backward compatibility (v1.5.0).
    /// Use SendSseEventAsync(SseEventMessage) instead.
    /// </summary>
    [Obsolete("Use SendSseEventAsync(SseEventMessage) instead")]
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
