namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// Extension methods for mapping the unified /mcp endpoint (MCP 2025-11-25).
/// Implements Streamable HTTP transport with SSE support.
/// </summary>
public static class StreamableHttpEndpoint
{
    /// <summary>
    /// Maps the unified /mcp endpoint that handles POST, GET, and DELETE.
    /// Implements MCP 2025-11-25 Streamable HTTP transport.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <param name="pattern">The endpoint pattern (e.g., "/mcp")</param>
    /// <returns>The application builder for chaining</returns>
    /// <remarks>
    /// POST: Send JSON-RPC requests (immediate response or SSE stream)
    /// GET: Open long-lived SSE stream for notifications
    /// DELETE: Terminate session
    /// 
    /// Requires UseProtocolVersionValidation() middleware before this endpoint.
    /// Session management is optional (auto-detected via SessionService DI).
    /// </remarks>
    public static WebApplication MapStreamableHttpEndpoint(
        this WebApplication app,
        string pattern)
    {
        // Handle POST (send JSON-RPC request)
        app.MapPost(pattern, async (
            HttpContext context,
            ToolInvoker invoker,
            SessionService? sessionService,
            EventIdGenerator? eventIdGenerator,
            ILogger<ToolInvoker> logger,
            CancellationToken ct) =>
        {
            // 1. Session management (optional)
            var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();

            if (sessionService != null)
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    // Create new session on first request
                    sessionId = sessionService.CreateSession();
                    context.Response.Headers["MCP-Session-Id"] = sessionId;
                    logger.LogInformation("Created new session: {SessionId}", sessionId);
                }
                else if (!sessionService.ValidateSession(sessionId))
                {
                    // Session expired or invalid
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = -32001,
                            message = "Session not found or expired",
                            data = new { sessionId }
                        }
                    }, ct);
                    return;
                }
            }

            // Store sessionId in HttpContext.Items for access by handlers (v1.8.0 Phase 4)
            if (!string.IsNullOrEmpty(sessionId))
            {
                context.Items["SessionId"] = sessionId;
            }

            // 2. Parse JSON-RPC request
            JsonDocument? doc = null;
            try
            {
                doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: ct);
                var element = doc.RootElement;

                // 3. Invoke tool (returns immediate JSON response OR IAsyncEnumerable)
                var response = await invoker.InvokeSingleAsync(element, "http", ct);

                // 4. Return session ID in response header
                if (!string.IsNullOrEmpty(sessionId))
                {
                    context.Response.Headers["MCP-Session-Id"] = sessionId;
                }

                // 5. Handle response
                if (response is IAsyncEnumerable<JsonRpcMessage> stream)
                {
                    // Streaming response (multiple JSON-RPC messages)
                    // We write them sequentially to the response body, separated by newlines (JSON Lines)
                    // This allows the client to read them one by one.
                    
                    // Set content type to application/json-seq or similar? 
                    // Standard application/json is fine if we just write whitespace separated JSONs.
                    // But `WriteAsJsonAsync` closes the stream.
                    
                    // We need to write manually.
                    context.Response.ContentType = "application/json"; // Or application/json-seq
                    
                    await foreach (var message in stream.WithCancellation(ct))
                    {
                        await JsonSerializer.SerializeAsync(context.Response.Body, message, JsonOptions.Default, ct);
                        await context.Response.WriteAsync("\n", ct); // Newline delimiter
                        await context.Response.Body.FlushAsync(ct);
                    }
                }
                else
                {
                    // Standard single response
                    await context.Response.WriteAsJsonAsync(response, ct);
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "JSON parse error in POST /mcp");
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32700,
                        message = "Parse error",
                        data = new { detail = ex.Message }
                    }
                }, ct);
            }
            finally
            {
                doc?.Dispose();
            }
        });

        // Handle GET (open SSE stream for notifications)
        app.MapGet(pattern, async (
            HttpContext context,
            SessionService? sessionService,
            SseStreamRegistry? sseRegistry,
            ILogger<ToolInvoker> logger,
            CancellationToken ct) =>
        {
            // 1. Validate session (required for GET)
            var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();

            if (sessionService != null)
            {
                if (string.IsNullOrEmpty(sessionId))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = -32602,
                            message = "Missing MCP-Session-Id header for SSE stream"
                        }
                    }, ct);
                    return;
                }

                if (!sessionService.ValidateSession(sessionId))
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = new
                        {
                            code = -32001,
                            message = "Session not found or expired",
                            data = new { sessionId }
                        }
                    }, ct);
                    return;
                }
            }

            // 2. Set SSE headers
            context.Response.ContentType = "text/event-stream; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no"; // Disable nginx buffering

            if (!string.IsNullOrEmpty(sessionId))
            {
                context.Response.Headers["MCP-Session-Id"] = sessionId;
            }

            await context.Response.StartAsync(ct);

            // 3. Get Last-Event-ID for resumption
            var lastEventId = context.Request.Headers["Last-Event-ID"].ToString();
            
            // 4. Replay buffered messages (v1.7.0 Phase 2)
            if (sessionService != null && !string.IsNullOrEmpty(sessionId))
            {
                var session = sessionService.GetSession(sessionId);
                if (session != null)
                {
                    var bufferedMessages = session.MessageBuffer.GetMessagesAfter(lastEventId);
                    var replayCount = 0;
                    
                    foreach (var buffered in bufferedMessages)
                    {
                        var sseMessage = SseEventMessage.CreateMessage(buffered.EventId, buffered.Message);
                        await WriteSseEventAsync(context.Response, sseMessage, ct);
                        replayCount++;
                    }
                    
                    if (replayCount > 0)
                    {
                        logger.LogInformation(
                            "Replayed {Count} buffered messages to session {SessionId} (after event ID: {LastEventId})",
                            replayCount,
                            sessionId,
                            lastEventId ?? "none");
                    }
                }
            }

            // 5. Register SSE stream for future notifications (v1.7.0 Phase 2)
            if (sseRegistry != null && !string.IsNullOrEmpty(sessionId))
            {
                sseRegistry.Register(sessionId, context.Response, ct);
                logger.LogInformation("Registered SSE stream for session: {SessionId}", sessionId);
            }

            // 6. Open long-lived SSE stream
            await OpenSseStreamAsync(
                context.Response,
                sessionId,
                sessionService,
                sseRegistry,
                logger,
                ct);
        });

        // Handle DELETE (terminate session)
        app.MapDelete(pattern, async (
            HttpContext context,
            SessionService? sessionService,
            ResourceSubscriptionRegistry? subscriptionRegistry,  // v1.8.0 Phase 4
            ILogger<ToolInvoker> logger) =>
        {
            if (sessionService == null)
            {
                context.Response.StatusCode = 501;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32601,
                        message = "Session management not enabled"
                    }
                });
                return;
            }

            var sessionId = context.Request.Headers["MCP-Session-Id"].ToString();

            if (string.IsNullOrEmpty(sessionId))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32602,
                        message = "Missing MCP-Session-Id header"
                    }
                });
                return;
            }

            // Cleanup resource subscriptions first (v1.8.0 Phase 4)
            if (subscriptionRegistry != null)
            {
                var clearedCount = subscriptionRegistry.ClearSession(sessionId);
                if (clearedCount > 0)
                {
                    logger.LogInformation(
                        "Cleared {Count} resource subscription(s) for session {SessionId}",
                        clearedCount,
                        sessionId);
                }
            }

            var deleted = sessionService.DeleteSession(sessionId);
            if (!deleted)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = new
                    {
                        code = -32001,
                        message = "Session not found",
                        data = new { sessionId }
                    }
                });
                return;
            }

            logger.LogInformation("Terminated session: {SessionId}", sessionId);
            context.Response.StatusCode = 204; // No Content
        });

        return app;
    }

    /// <summary>
    /// Opens a long-lived SSE stream for server-to-client messages.
    /// Sends keep-alive pings every 30 seconds.
    /// Handles cleanup on disconnect (v1.7.0 Phase 2).
    /// </summary>
    private static async Task OpenSseStreamAsync(
        HttpResponse response,
        string? sessionId,
        SessionService? sessionService,
        SseStreamRegistry? sseRegistry,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            // Keep connection alive with periodic pings
            while (!ct.IsCancellationRequested)
            {
                // Send keep-alive comment (no event ID needed)
                await response.WriteAsync(": keep-alive\n\n", ct);
                await response.Body.FlushAsync(ct);

                // Wait 30 seconds before next ping
                await Task.Delay(30_000, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected - normal flow
            logger.LogInformation("SSE stream closed for session: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SSE stream for session: {SessionId}", sessionId);
        }
        finally
        {
            // Unregister SSE stream on disconnect (v1.7.0 Phase 2)
            if (sseRegistry != null && !string.IsNullOrEmpty(sessionId))
            {
                sseRegistry.Unregister(sessionId, response);
                logger.LogInformation("Unregistered SSE stream for session: {SessionId}", sessionId);
            }
        }
    }

    /// <summary>
    /// Writes an SSE event to the HTTP response stream (v1.7.0 Phase 2).
    /// </summary>
    private static async Task WriteSseEventAsync(
        HttpResponse response,
        SseEventMessage message,
        CancellationToken ct)
    {
        // Write event ID
        if (!string.IsNullOrEmpty(message.Id))
        {
            await response.WriteAsync($"id: {message.Id}\n", ct);
        }

        // Write event type (optional, defaults to "message")
        if (!string.IsNullOrEmpty(message.Event))
        {
            await response.WriteAsync($"event: {message.Event}\n", ct);
        }

        // Write retry interval (optional)
        if (message.Retry.HasValue)
        {
            await response.WriteAsync($"retry: {message.Retry.Value}\n", ct);
        }

        // Write data
        var json = JsonSerializer.Serialize(message.Data, JsonOptions.Default);
        await response.WriteAsync($"data: {json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}
