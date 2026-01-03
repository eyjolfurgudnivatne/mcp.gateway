namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.WebSockets;
using System.Text.Json;

/// <summary>
/// ToolInvoker partial class - WebSocket transport (v1.5.0)
/// </summary>
public partial class ToolInvoker
{
    /// <summary>
    /// Invokes JSON-RPC requests over WebSocket.
    /// Handles single and batch requests, including StreamMessage for binary streaming.
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
        
        // Register for notifications (legacy support)
        if (_notificationSender is Notifications.NotificationService notificationService)
        {
            notificationService.AddSubscriber(socket);
        }
        
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

                // Try to parse as StreamMessage first (for streaming functions)
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
                            var toolDetails = _toolService.GetFunctionDetails(method);
                            
                            if (toolDetails.FunctionArgumentType.IsToolConnector)
                            {
                                // Create ToolConnector with actual StreamMessage
                                var connector = new ToolConnector(socket);
                                connector.StreamMessage = streamMsg;
                                
                                // Invoke tool
                                var toolResult = _toolService.InvokeFunctionDelegate(method, toolDetails, connector);
                                
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
        finally
        {
            // Unregister from notifications
            if (_notificationSender is Notifications.NotificationService ns)
            {
                ns.RemoveSubscriber(socket);
            }
        }
    }

    /// <summary>
    /// Invokes a single JSON-RPC request over WebSocket.
    /// Handles ToolConnector-based functions specially.
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

            // MCP protocol methods (check BEFORE GetFunctionDetails!)
            if (message.Method == "initialize")
            {
                return HandleInitialize(message);
            }
            
            if (message.Method == "tools/list" ||
                message.Method == "prompts/list")
            {
                return HandleFunctionsList(message, "ws");
            }

            // Formatted tool lists (functions/list/{format})
            if (message.Method?.StartsWith("tools/list/") == true ||
                message.Method?.StartsWith("prompts/list/") == true)
            {
                return HandleFormattedFunctionsList(message, "ws");
            }

            if (message.Method == "tools/call")
            {
                return await HandleFunctionsCallAsync(message, cancellationToken);
            }

            // MCP Resources support (v1.5.0)
            if (message.Method == "resources/list")
            {
                return HandleResourcesList(message);
            }
            
            if (message.Method == "resources/read")
            {
                return await HandleResourcesReadAsync(message, cancellationToken);
            }

            // MCP notifications (client → server, no response expected)
            if (message.Method?.StartsWith("notifications/") == true)
            {
                // Log and ignore MCP notifications (e.g., "notifications/initialized")
                _logger.LogInformation("Received MCP notification: {Method}", message.Method);
                return null; // No response for notifications
            }

            // Fix for CS8604: Add null check for message.Method before calling GetFunctionDetails
            if (string.IsNullOrEmpty(message.Method))
            {
                return ToolResponse.Error(
                    id,
                    -32600,
                    "Invalid Request",
                    "Method name must not be null or empty");
            }
            var toolDetails = _toolService.GetFunctionDetails(message.Method);
            
            // Check if this is a ToolConnector-based tool (streaming)
            if (toolDetails.FunctionArgumentType.IsToolConnector)
            {
                // Create ToolConnector and pass WebSocket ownership
                var connector = new ToolConnector(socket);
                
                // For read functions: create a synthetic StreamMessage from JSON-RPC request
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
                var result = _toolService.InvokeFunctionDelegate(
                    message.Method,
                    toolDetails,
                    connector);
                
                // Return the Task so caller knows this is ToolConnector
                return result;
            }
            
            // Regular tool (non-streaming)
            object[] args;

            // If the tool expects a TypedJsonRpc<T>, wrap the JsonRpcMessage accordingly.
            if (toolDetails.FunctionArgumentType.IsTypedJsonRpc)
            {
                var paramType = toolDetails.FunctionArgumentType.ParameterType;

                args = [Activator.CreateInstance(paramType, message)
                        ?? throw new ToolInternalErrorException(
                            $"{message.Method}: Failed to create TypedJsonRpc instance for parameter type '{paramType.FullName}'")];
            }
            else
            {
                args = [message];
            }

            var regularResult = _toolService.InvokeFunctionDelegate(
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
    /// Sends JSON object as text message over WebSocket
    /// </summary>
    private static async Task SendJsonAsync(
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
}
