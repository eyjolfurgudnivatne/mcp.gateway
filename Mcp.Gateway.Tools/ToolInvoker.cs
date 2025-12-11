namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text.Json;
using Mcp.Gateway.Tools.Formatters;

/// <summary>
/// Handles JSON-RPC tool invocation over HTTP and WebSocket.
/// Class-based for dependency injection and testability.
/// </summary>
public class ToolInvoker
{
    private readonly ToolService _toolService;
    private readonly ILogger<ToolInvoker> _logger;
    
    // Default buffer size for WebSocket frame accumulation
    private const int DefaultBufferSize = 64 * 1024;

    public ToolInvoker(ToolService toolService, ILogger<ToolInvoker> logger)
    {
        _toolService = toolService;
        _logger = logger;
    }

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

    /// <summary>
    /// Invokes JSON-RPC requests over WebSocket.
    /// Handles single and batch requests.
    /// </summary>
    public async Task InvokeWsRpcAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        using var stopToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var buffer = new byte[DefaultBufferSize];
        
        try
        {
            while (socket.State == WebSocketState.Open && !stopToken.Token.IsCancellationRequested)
            {
                using var messageStream = new MemoryStream();
                WebSocketReceiveResult? result = null;

                // Accumulate fragments until EndOfMessage
                do
                {
                    try
                    {
                        result = await socket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            stopToken.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (WebSocketException)
                    {
                        return;
                    }

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                        {
                            await socket.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Closing",
                                CancellationToken.None);
                        }
                        return;
                    }

                    if (result.Count > 0)
                    {
                        messageStream.Write(buffer, 0, result.Count);
                    }

                } while (!result.EndOfMessage);

                // Only handle text messages (JSON-RPC)
                if (result.MessageType != WebSocketMessageType.Text)
                    continue;

                messageStream.Position = 0;
                using var doc = await JsonDocument.ParseAsync(messageStream, cancellationToken: stopToken.Token);

                // Try to parse as StreamMessage first (for streaming tools)
                if (StreamMessage.TryGetFromJsonElement(doc.RootElement, out var streamMsg) && 
                    streamMsg != null && 
                    streamMsg.IsStart)
                {
                    // This is a streaming start message
                    var method = streamMsg.GetMethod;
                    if (!string.IsNullOrEmpty(method))
                    {
                        try
                        {
                            var toolDetails = _toolService.GetToolDetails(method);
                            
                            if (toolDetails.ToolArgumentType.IsToolConnector)
                            {
                                // Create ToolConnector with actual StreamMessage
                                var connector = new ToolConnector(socket);
                                connector.StreamMessage = streamMsg;
                                
                                // Invoke tool
                                var toolResult = _toolService.InvokeToolDelegate(method, toolDetails, connector);
                                
                                if (toolResult is Task toolConnectorTask)
                                {
                                    try
                                    {
                                        await toolConnectorTask.ConfigureAwait(false);
                                        
                                        if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                                        {
                                            await socket.CloseAsync(
                                                WebSocketCloseStatus.NormalClosure,
                                                "Tool complete",
                                                CancellationToken.None);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "ToolConnector tool failed");
                                        
                                        if (socket.State == WebSocketState.Open)
                                        {
                                            try
                                            {
                                                await socket.CloseAsync(
                                                    WebSocketCloseStatus.InternalServerError,
                                                    "Tool error",
                                                    CancellationToken.None);
                                            }
                                            catch { /* Best effort */ }
                                        }
                                    }
                                    
                                    return;
                                }
                            }
                            else
                            {
                                // Tool expects JSON-RPC but got StreamMessage
                                var errorMsg = StreamMessage.CreateErrorMessage(
                                    streamMsg.Id,
                                    new JsonRpcError(-32601, "Tool does not support streaming", null));
                                await SendJsonAsync(socket, errorMsg, stopToken.Token);
                                continue;
                            }
                        }
                        catch (ToolNotFoundException)
                        {
                            var errorMsg = StreamMessage.CreateErrorMessage(
                                streamMsg.Id,
                                new JsonRpcError(-32601, "Method not found", new { method }));
                            await SendJsonAsync(socket, errorMsg, stopToken.Token);
                            continue;
                        }
                    }
                }

                // Fall back to JSON-RPC parsing
                // Batch request
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var responses = new List<object>();
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        var response = await InvokeSingleAsync(element, stopToken.Token);
                        if (response is not null)
                            responses.Add(response);
                    }

                    if (responses.Count > 0)
                    {
                        await SendJsonAsync(socket, responses, stopToken.Token);
                    }
                }
                // Single request
                else
                {
                    var response = await InvokeSingleWsAsync(doc.RootElement, socket, stopToken.Token);
                    
                    // Check if this was a ToolConnector tool (streaming)
                    if (response is Task toolConnectorTask)
                    {
                        // ToolConnector tool has taken over WebSocket ownership
                        // Wait for it to complete, then close WebSocket cleanly
                        try
                        {
                            await toolConnectorTask.ConfigureAwait(false);
                            
                            // Close WebSocket after tool completes
                            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                            {
                                await socket.CloseAsync(
                                    WebSocketCloseStatus.NormalClosure,
                                    "Tool complete",
                                    CancellationToken.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "ToolConnector tool failed");
                            
                            // Try to close with error status
                            if (socket.State == WebSocketState.Open)
                            {
                                try
                                {
                                    await socket.CloseAsync(
                                        WebSocketCloseStatus.InternalServerError,
                                        "Tool error",
                                        CancellationToken.None);
                                }
                                catch { /* Best effort */ }
                            }
                        }
                        
                        // Exit immediately - done handling this WebSocket
                        return;
                    }
                    
                    if (response is not null)
                    {
                        await SendJsonAsync(socket, response, stopToken.Token);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket RPC handler");
            
            // Try to send error response if socket is still open
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    var errorResponse = ToolResponse.Error(
                        null, 
                        -32603, 
                        "Internal error", 
                        new { detail = ex.Message });
                    await SendJsonAsync(socket, errorResponse, CancellationToken.None);
                }
                catch
                {
                    // Silent - best effort
                }
            }
        }
    }

    /// <summary>
    /// Invokes a single JSON-RPC request.
    /// Returns null for notifications (no response expected).
    /// </summary>
    public async Task<object?> InvokeSingleAsync(
        JsonElement element,
        CancellationToken cancellationToken)
    {
        // Delegate to overloaded method with default transport detection
        // This method is used by WebSocket (non-WS specific) path
        return await InvokeSingleAsync(element, "http", cancellationToken);
    }

    /// <summary>
    /// Invokes a single JSON-RPC request from stdio transport.
    /// Delegates to InvokeSingleAsync - stdio is just a different event loop.
    /// </summary>
    public async Task<object?> InvokeSingleStdioAsync(
        JsonElement element,
        CancellationToken cancellationToken = default)
    {
        // Delegate to transport-aware InvokeSingleAsync with stdio transport
        return await InvokeSingleAsync(element, "stdio", cancellationToken);
    }

    /// <summary>
    /// Invokes a single JSON-RPC request with transport filtering.
    /// Returns null for notifications (no response expected).
    /// </summary>
    public async Task<object?> InvokeSingleAsync(
        JsonElement element,
        string transport,
        CancellationToken cancellationToken = default)
    {
        object? id = null;
        
        try
        {
            // Parse JSON-RPC message
            if (!JsonRpcMessage.TryGetFromJsonElement(element, out var message) || message is null)
            {
                return ToolResponse.Error(
                    null,
                    -32600,
                    "Invalid Request",
                    "Must be valid JSON-RPC 2.0 message");
            }

            id = message.Id;

            // Only process requests and notifications
            if (!message.IsRequest && !message.IsNotification)
            {
                return ToolResponse.Error(
                    id,
                    -32600,
                    "Invalid Request",
                    "Message must be a request or notification");
            }

            // MCP protocol methods (check BEFORE GetToolDetails!)
            if (message.Method == "initialize")
            {
                return HandleInitialize(message);
            }
            
            if (message.Method == "tools/list")
            {
                // Use transport-aware filtering
                return HandleToolsList(message, transport);
            }
            
            // NEW: Formatted tool lists (tools/list/{format})
            if (message.Method?.StartsWith("tools/list/") == true)
            {
                return HandleFormattedToolsList(message, transport);
            }
            
            if (message.Method == "tools/call")
            {
                return await HandleToolsCallAsync(message, cancellationToken);
            }

            // MCP notifications (client → server, no response expected)
            if (message.Method?.StartsWith("notifications/") == true)
            {
                // Log and ignore MCP notifications (e.g., "notifications/initialized")
                _logger.LogInformation("Received MCP notification: {Method}", message.Method);
                return null; // No response for notifications
            }

            // Get tool details
            var toolDetails = _toolService.GetToolDetails(message.Method);
            
            // Check if this is a ToolConnector-based tool (streaming)
            if (toolDetails.ToolArgumentType.IsToolConnector)
            {
                // ToolConnector tools should be initiated via StreamMessage start, not JSON-RPC
                return ToolResponse.Error(
                    id,
                    -32601,
                    "Use StreamMessage to initiate streaming",
                    "Send a StreamMessage with type='start' to begin streaming");
            }
            
            // Build arguments for tool method
            object[] args = [message];

            // Invoke the tool
            var result = _toolService.InvokeToolDelegate(
                message.Method,
                toolDetails,
                args);

            // Handle different return types
            return await ProcessToolResultAsync(result, toolDetails, message.IsNotification, id, cancellationToken);
        }
        catch (ToolNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tool not found: {Method}", ex.Message);
            return ToolResponse.Error(id, -32601, "Method not found", new { detail = ex.Message });
        }
        catch (ToolInvalidParamsException ex)
        {
            _logger.LogWarning(ex, "Invalid params for tool");
            return ToolResponse.Error(id, -32602, "Invalid params", new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking tool");
            return ToolResponse.Error(id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Invokes a single JSON-RPC request over WebSocket.
    /// Handles ToolConnector-based tools specially.
    /// Returns null for notifications (no response expected).
    /// </summary>
    private async Task<object?> InvokeSingleWsAsync(
        JsonElement element,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        object? id = null;
        
        try
        {
            // Parse JSON-RPC message
            if (!JsonRpcMessage.TryGetFromJsonElement(element, out var message) || message is null)
            {
                return ToolResponse.Error(
                    null,
                    -32600,
                    "Invalid Request",
                    "Must be valid JSON-RPC 2.0 message");
            }

            id = message.Id;

            // Only process requests and notifications
            if (!message.IsRequest && !message.IsNotification)
            {
                return ToolResponse.Error(
                    id,
                    -32600,
                    "Invalid Request",
                    "Message must be a request or notification");
            }

            // MCP protocol methods (check BEFORE GetToolDetails!)
            if (message.Method == "initialize")
            {
                return HandleInitialize(message);
            }
            
            if (message.Method == "tools/list")
            {
                return HandleToolsList(message, "ws");
            }
            
            if (message.Method == "tools/call")
            {
                return await HandleToolsCallAsync(message, cancellationToken);
            }

            // MCP notifications (client → server, no response expected)
            if (message.Method?.StartsWith("notifications/") == true)
            {
                // Log and ignore MCP notifications (e.g., "notifications/initialized")
                _logger.LogInformation("Received MCP notification: {Method}", message.Method);
                return null; // No response for notifications
            }

            // Get tool details
            var toolDetails = _toolService.GetToolDetails(message.Method);
            
            // Check if this is a ToolConnector-based tool (streaming)
            if (toolDetails.ToolArgumentType.IsToolConnector)
            {
                // Create ToolConnector and pass WebSocket ownership
                var connector = new ToolConnector(socket);
                
                // For read tools: create a synthetic StreamMessage from JSON-RPC request
                // This allows tool to start receive loop with proper context
                var metaObj = new
                {
                    method = message.Method,
                    binary = true, // Default to binary for now
                    correlationId = message.Id
                };
                
                // Serialize to JsonElement so ToolConnector can parse it
                var metaJson = JsonSerializer.Serialize(metaObj, JsonOptions.Default);
                var metaElement = JsonDocument.Parse(metaJson).RootElement.Clone();
                
                connector.StreamMessage = StreamMessage.CreateStartMessage(metaElement) with { Id = message.IdAsString };
                
                // Invoke tool with connector
                var result = _toolService.InvokeToolDelegate(
                    message.Method,
                    toolDetails,
                    connector);
                
                // Return the Task so caller knows this is ToolConnector
                return result;
            }
            
            // Regular tool (non-streaming)
            object[] args = [message];
            var regularResult = _toolService.InvokeToolDelegate(
                message.Method,
                toolDetails,
                args);

            // Handle different return types
            return await ProcessToolResultAsync(regularResult, toolDetails, message.IsNotification, id, cancellationToken);
        }
        catch (ToolNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tool not found: {Method}", ex.Message);
            return ToolResponse.Error(id, -32601, "Method not found", new { detail = ex.Message });
        }
        catch (ToolInvalidParamsException ex)
        {
            _logger.LogWarning(ex, "Invalid params for tool");
            return ToolResponse.Error(id, -32602, "Invalid params", new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invoking tool");
            return ToolResponse.Error(id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Handles MCP initialize request
    /// </summary>
    private JsonRpcMessage HandleInitialize(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new
        {
            protocolVersion = "2025-06-18", // Updated to latest MCP protocol version
            //protocolVersion = "2024-11-05",
            serverInfo = new
            {
                name = "mcp-gateway",
                version = "2.0.0"
            },
            capabilities = new
            {
                tools = new { }
            }
        });
    }

    /// <summary>
    /// Handles MCP tools/list request
    /// </summary>
    private JsonRpcMessage HandleToolsList(JsonRpcMessage request)
    {
        try
        {
            var tools = _toolService.GetAllToolDefinitions();
            var toolsList = tools.Select(t =>
            {
                object? schema = null;
                try
                {
                    schema = JsonSerializer.Deserialize<object>(t.InputSchema, JsonOptions.Default);
                }
                catch
                {
                    // Fallback to empty object schema if deserialization fails
                    schema = new { type = "object", properties = new { } };
                }

                return new
                {
                    name = t.Name,
                    description = t.Description,
                    inputSchema = schema
                };
            }).ToList();

            return ToolResponse.Success(request.Id, new
            {
                tools = toolsList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tools/list");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Detects the transport type from HttpContext.
    /// Used for capability-based tool filtering.
    /// </summary>
    /// <param name="context">HttpContext (null for stdio)</param>
    /// <returns>Transport type: "stdio", "http", "ws", or "sse"</returns>
    private static string DetectTransport(HttpContext? context)
    {
        if (context == null) return "stdio";
        if (context.WebSockets.IsWebSocketRequest) return "ws";
        if (context.Request.Headers.Accept.ToString().Contains("text/event-stream")) return "sse";
        return "http";
    }

    /// <summary>
    /// Handles MCP tools/list request with transport filtering.
    /// Filters tools based on transport capabilities to prevent clients from seeing incompatible tools.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="transport">Transport type: "stdio", "http", "ws", or "sse"</param>
    private JsonRpcMessage HandleToolsList(JsonRpcMessage request, string transport)
    {
        try
        {
            // Get filtered tools for this transport
            var tools = _toolService.GetToolsForTransport(transport);
            var toolsList = tools.Select(t =>
            {
                object? schema = null;
                try
                {
                    schema = JsonSerializer.Deserialize<object>(t.InputSchema, JsonOptions.Default);
                }
                catch
                {
                    // Fallback to empty object schema if deserialization fails
                    schema = new { type = "object", properties = new { } };
                }

                return new
                {
                    name = t.Name,
                    description = t.Description,
                    inputSchema = schema
                };
            }).ToList();

            return ToolResponse.Success(request.Id, new
            {
                tools = toolsList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tools/list");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Handles formatted tool list requests (tools/list/{format}).
    /// Supports multiple AI platform formats: ollama, microsoft-ai, openai, etc.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="transport">Transport type for capability filtering</param>
    /// <returns>Formatted tool list in the requested format</returns>
    private JsonRpcMessage HandleFormattedToolsList(JsonRpcMessage request, string transport)
    {
        try
        {
            // Extract format from method name (e.g., "tools/list/ollama" → "ollama")
            var format = request.Method?.Replace("tools/list/", "") ?? "";
            
            if (string.IsNullOrEmpty(format))
            {
                return ToolResponse.Error(
                    request.Id,
                    -32600,
                    "Invalid Request",
                    "Format must be specified (e.g., tools/list/ollama)");
            }
            
            // Get filtered tools for this transport
            var tools = _toolService.GetToolsForTransport(transport);
            
            // Get appropriate formatter
            IToolListFormatter formatter = format.ToLowerInvariant() switch
            {
                "ollama" => new OllamaToolListFormatter(),
                "microsoft-ai" => new MicrosoftAIToolListFormatter(),
                "mcp" => new McpToolListFormatter(),
                _ => throw new ToolNotFoundException($"Unknown format: {format}")
            };
            
            // Format tools
            var formattedTools = formatter.FormatToolList(tools);
            
            return ToolResponse.Success(request.Id, formattedTools);
        }
        catch (ToolNotFoundException ex)
        {
            _logger.LogWarning(ex, "Unknown tool list format: {Method}", request.Method);
            return ToolResponse.Error(
                request.Id,
                -32601,
                "Unknown format",
                new { detail = ex.Message, supportedFormats = new[] { "ollama", "microsoft-ai", "mcp" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in formatted tools/list");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Handles MCP tools/call request
    /// </summary>
    private async Task<JsonRpcMessage> HandleToolsCallAsync(JsonRpcMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var requestParams = request.GetParams();
            var toolName = requestParams.GetProperty("name").GetString();
            if (string.IsNullOrEmpty(toolName))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'name' parameter");
            }

            // Get arguments if provided
            JsonElement? args = null;
            if (requestParams.TryGetProperty("arguments", out var argsElement))
            {
                args = argsElement;
            }

            // Build tool request
            var toolRequest = JsonRpcMessage.CreateRequest(toolName, request.Id, args);

            // Get tool details and invoke
            var toolDetails = _toolService.GetToolDetails(toolName);
            
            if (toolDetails.ToolArgumentType.IsToolConnector)
            {
                return ToolResponse.Error(
                    request.Id,
                    -32601,
                    "Tool requires streaming",
                    "This tool must be called via StreamMessage, not tools/call");
            }

            var result = _toolService.InvokeToolDelegate(toolName, toolDetails, toolRequest);
            var processedResult = await ProcessToolResultAsync(result, toolDetails, false, request.Id, cancellationToken);

            // Extract the actual result data
            object? resultData = null;
            if (processedResult is JsonRpcMessage msg)
            {
                resultData = msg.Result; // Unwrap JsonRpcMessage to get just the result
            }
            else
            {
                resultData = processedResult;
            }

            // Wrap result in MCP content format
            var resultJson = JsonSerializer.Serialize(resultData, JsonOptions.Default);
            var mcpResult = new
            {
                content = new []
                {
                    new
                    {
                        type = "text",
                        text = resultJson
                    }
                }
            };

            return ToolResponse.Success(request.Id, mcpResult);
        }
        catch (ToolNotFoundException ex)
        {
            return ToolResponse.Error(request.Id, -32601, "Method not found", new { detail = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tools/call");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Processes different tool return types (sync, async, void, etc.)
    /// </summary>
    private async Task<object?> ProcessToolResultAsync(
        object? result,
        ToolService.ToolDetails toolDetails,
        bool isNotification,
        object? id,
        CancellationToken cancellationToken)
    {
        // Task<JsonRpcMessage>
        if (result is Task<JsonRpcMessage> jsonRpcTask)
        {
            var response = await jsonRpcTask.ConfigureAwait(false);
            return isNotification ? null : response;
        }

        // Task<object?>
        if (result is Task<object?> objectTask)
        {
            var response = await objectTask.ConfigureAwait(false);
            return isNotification ? null : response;
        }

        // Task (void)
        if (result is Task voidTask)
        {
            await voidTask.ConfigureAwait(false);
            return null; // Notifications return nothing
        }

        // JsonRpcMessage (sync)
        if (result is JsonRpcMessage jsonRpcMessage)
        {
            return isNotification ? null : jsonRpcMessage;
        }

        // Notification (void method)
        if (isNotification)
        {
            return null;
        }

        // Regular sync return
        return result;
    }

    /// <summary>
    /// Sends JSON object as text message over WebSocket
    /// </summary>
    private async Task SendJsonAsync(
        WebSocket socket,
        object data,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(data, JsonOptions.Default);
        await socket.SendAsync(
            json,
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

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
