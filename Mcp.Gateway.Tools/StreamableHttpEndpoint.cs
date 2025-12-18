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

            // 2. Parse JSON-RPC request
            JsonDocument? doc = null;
            try
            {
                doc = await JsonDocument.ParseAsync(context.Request.Body, cancellationToken: ct);
                var element = doc.RootElement;

                // 3. Invoke tool (returns immediate JSON response)
                var response = await invoker.InvokeSingleAsync(element, "http", ct);

                // 4. Return session ID in response header
                if (!string.IsNullOrEmpty(sessionId))
                {
                    context.Response.Headers["MCP-Session-Id"] = sessionId;
                }

                // 5. Return JSON response
                await context.Response.WriteAsJsonAsync(response, ct);
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
            EventIdGenerator? eventIdGenerator,
            Notifications.INotificationSender? notificationService,
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

            // 3. Get Last-Event-ID for resumption (future enhancement)
            var lastEventId = context.Request.Headers["Last-Event-ID"].ToString();
            if (!string.IsNullOrEmpty(lastEventId))
            {
                logger.LogInformation(
                    "SSE stream resumption requested from event ID: {LastEventId}",
                    lastEventId);
            }

            // 4. Open long-lived SSE stream
            logger.LogInformation("Opening SSE stream for session: {SessionId}", sessionId);
            await OpenSseStreamAsync(
                context.Response,
                sessionId,
                lastEventId,
                sessionService,
                eventIdGenerator,
                notificationService,
                logger,
                ct);
        });

        // Handle DELETE (terminate session)
        app.MapDelete(pattern, async (
            HttpContext context,
            SessionService? sessionService,
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
    /// </summary>
    private static async Task OpenSseStreamAsync(
        HttpResponse response,
        string? sessionId,
        string? lastEventId,
        SessionService? sessionService,
        EventIdGenerator? eventIdGenerator,
        Notifications.INotificationSender? notificationService,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            // TODO Phase 2: Implement message replay after lastEventId
            // TODO Phase 2: Subscribe to notification channels
            // For now: Send keep-alive pings to maintain connection

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
    }
}
