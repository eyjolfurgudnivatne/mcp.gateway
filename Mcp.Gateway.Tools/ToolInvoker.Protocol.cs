namespace Mcp.Gateway.Tools;

using Mcp.Gateway.Tools.Formatters;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static Mcp.Gateway.Tools.ToolService;

/// <summary>
/// ToolInvoker partial class - MCP Protocol handlers (v1.5.0)
/// </summary>
public partial class ToolInvoker
{
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

            // MCP protocol methods (check BEFORE GetFunctionDetails!)
            if (message.Method == "initialize")
            {
                return HandleInitialize(message);
            }
            
            if (message.Method == "tools/list" ||
                message.Method == "prompts/list")
            {
                // Use transport-aware filtering
                return HandleFunctionsList(message, transport);
            }
            
            // Formatted tool lists (functions/list/{format})
            if (message.Method?.StartsWith("tools/list/") == true ||
                message.Method?.StartsWith("prompts/list/") == true)
            {
                return HandleFormattedFunctionsList(message, transport);
            }
            
            if (message.Method == "tools/call" ||
                message.Method == "prompts/get")
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
                // ToolConnector functions should be initiated via StreamMessage start, not JSON-RPC
                return ToolResponse.Error(
                    id,
                    -32601,
                    "Use StreamMessage to initiate streaming",
                    "Send a StreamMessage with type='start' to begin streaming");
            }
            
            // Build arguments for tool method
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

            // Invoke the tool
            var result = _toolService.InvokeFunctionDelegate(
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
    /// Handles MCP initialize request
    /// </summary>
    private JsonRpcMessage HandleInitialize(JsonRpcMessage request)
    {
        bool isTools = _toolService.GetAllFunctionDefinitions(ToolService.FunctionTypeEnum.Tool).Any();
        bool isPrompts = _toolService.GetAllFunctionDefinitions(ToolService.FunctionTypeEnum.Prompt).Any();
        bool hasResources = _toolService.GetAllResourceDefinitions().Any();

        Dictionary<string, object> capabilities = [];

        if (isTools)
        {
            capabilities["tools"] = new { };
        }
        if (isPrompts)
        {
            capabilities["prompts"] = new { };
        }
        if (hasResources)
        {
            capabilities["resources"] = new { };
        }

        // Add notification capabilities (v1.6.0+)
        // Note: Notifications require WebSocket transport
        if (_notificationSender is not null)
        {
            var notifications = new Dictionary<string, object>();
            
            if (isTools)
                notifications["tools"] = new { };
            
            if (isPrompts)
                notifications["prompts"] = new { };
            
            if (hasResources)
                notifications["resources"] = new { };

            if (notifications.Count > 0)
                capabilities["notifications"] = notifications;
        }
        
        return ToolResponse.Success(request.Id, new
        {
            protocolVersion = "2025-11-25", // Updated to MCP 2025-11-25 (v1.6.5+)
            serverInfo = new
            {
                name = "mcp-gateway",
                version = "2.0.0"
            },
            capabilities
        });
    }

    /// <summary>
    /// Handles MCP functions/list request with transport filtering.
    /// Filters functions based on transport capabilities to prevent clients from seeing incompatible functions.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="transport">Transport type: "stdio", "http", "ws", or "sse"</param>
    private JsonRpcMessage HandleFunctionsList(JsonRpcMessage request, string transport)
    {
        try
        {
            FunctionTypeEnum functionType = FunctionTypeEnum.Tool;

            if (request.Method == "tools/list")
            {
                functionType = FunctionTypeEnum.Tool;
            }
            if (request.Method == "prompts/list")
            {
                functionType = FunctionTypeEnum.Prompt;
            }

            // Extract pagination parameters (v1.6.0+)
            string? cursor = null;
            int pageSize = Pagination.CursorHelper.DefaultPageSize;

            if (request.Params is not null)
            {
                var @params = request.GetParams();
                
                // Extract cursor (optional)
                if (@params.TryGetProperty("cursor", out var cursorProp) && 
                    cursorProp.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    cursor = cursorProp.GetString();
                }
                
                // Extract pageSize (optional, default 100)
                if (@params.TryGetProperty("pageSize", out var pageSizeProp) && 
                    pageSizeProp.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    pageSize = pageSizeProp.GetInt32();
                }
            }

            // Get paginated functions for this transport
            var paginatedResult = _toolService.GetFunctionsForTransport(functionType, transport, cursor, pageSize);

            // Tools: serialize with inputSchema (object)
            if (functionType == FunctionTypeEnum.Tool)
            {
                var toolsList = paginatedResult.Items.Select(t =>
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

                    var toolObj = new Dictionary<string, object>
                    {
                        ["name"] = t.Name,
                        ["description"] = t.Description,
                        ["inputSchema"] = schema!
                    };

                    // Add icons if present (MCP 2025-11-25)
                    if (!string.IsNullOrEmpty(t.Icon))
                    {
                        toolObj["icons"] = new[]
                        {
                            new
                            {
                                src = t.Icon,
                                mimeType = (string?)null,
                                sizes = (string[]?)null
                            }
                        };
                    }

                    return toolObj;
                }).ToList();

                // Build response with pagination
                var response = new Dictionary<string, object>
                {
                    ["tools"] = toolsList
                };

                // Add nextCursor if more results available
                if (paginatedResult.NextCursor is not null)
                {
                    response["nextCursor"] = paginatedResult.NextCursor;
                }

                return ToolResponse.Success(request.Id, response);
            }

            // Prompts: serialize with arguments (array)
            else if (functionType == FunctionTypeEnum.Prompt)
            {
                var promptsList = paginatedResult.Items.Select(p =>
                {
                    var promptObj = new Dictionary<string, object>
                    {
                        ["name"] = p.Name,
                        ["description"] = p.Description,
                        ["arguments"] = p.Arguments ?? Array.Empty<PromptArgument>()
                    };

                    // Add icons if present (MCP 2025-11-25)
                    if (!string.IsNullOrEmpty(p.Icon))
                    {
                        promptObj["icons"] = new[]
                        {
                            new
                            {
                                src = p.Icon,
                                mimeType = (string?)null,
                                sizes = (string[]?)null
                            }
                        };
                    }

                    return promptObj;
                }).ToList();

                // Build response with pagination
                var response = new Dictionary<string, object>
                {
                    ["prompts"] = promptsList
                };

                // Add nextCursor if more results available
                if (paginatedResult.NextCursor is not null)
                {
                    response["nextCursor"] = paginatedResult.NextCursor;
                }

                return ToolResponse.Success(request.Id, response);
            }

            else
            {
                return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = "Invalid method" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tools/list");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Handles formatted function list requests (functions/list/{format}) (prompts/list/{format}).
    /// Supports multiple AI platform formats: ollama, microsoft-ai, openai, etc.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <param name="transport">Transport type for capability filtering</param>
    /// <returns>Formatted tool list in the requested format</returns>
    private JsonRpcMessage HandleFormattedFunctionsList(JsonRpcMessage request, string transport)
    {
        try
        {
            ToolService.FunctionTypeEnum functionType = ToolService.FunctionTypeEnum.Tool;
            string? format = null;

            if (!string.IsNullOrEmpty(request.Method) && request.Method.Contains("tools/list/"))
            {
                functionType = ToolService.FunctionTypeEnum.Tool;
                // Extract format from method name (e.g., "tools/list/ollama" → "ollama")
                format = request.Method?.Replace("tools/list/", "") ?? "";
            }
            if (!string.IsNullOrEmpty(request.Method) && request.Method.Contains("prompts/list/"))
            {
                functionType = ToolService.FunctionTypeEnum.Prompt;
                // Extract format from method name (e.g., "prompts/list/ollama" → "ollama")
                format = request.Method?.Replace("prompts/list/", "") ?? "";
            }

            
            if (string.IsNullOrEmpty(format))
            {
                return ToolResponse.Error(
                    request.Id,
                    -32600,
                    "Invalid Request",
                    "Format must be specified (e.g., tools/list/ollama)");
            }
            
            // Get filtered functions for this transport (no pagination for formatted lists)
            var paginatedTools = _toolService.GetFunctionsForTransport(functionType, transport);
            
            // Get appropriate formatter
            IToolListFormatter formatter = format.ToLowerInvariant() switch
            {
                "ollama" => new OllamaToolListFormatter(),
                "microsoft-ai" => new MicrosoftAIToolListFormatter(),
                "mcp" => new McpToolListFormatter(),
                _ => throw new ToolNotFoundException($"Unknown format: {format}")
            };
            
            // Format functions (use Items from paginated result)
            var formattedTools = formatter.FormatToolList(paginatedTools.Items);
            
            return ToolResponse.Success(request.Id, formattedTools);
        }
        catch (ToolNotFoundException ex)
        {
            _logger.LogWarning(ex, "Unknown function list format: {Method}", request.Method);
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
    /// Handles MCP functions/call request
    /// </summary>
    private async Task<JsonRpcMessage> HandleFunctionsCallAsync(JsonRpcMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var requestParams = request.GetParams();
            var functionName = requestParams.GetProperty("name").GetString();
            if (string.IsNullOrEmpty(functionName))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'name' parameter");
            }

            // Get arguments (tools) if provided
            JsonElement? args = null;
            if (requestParams.TryGetProperty("arguments", out var argsElement))
            {
                args = argsElement;
            }

            // Get params (prompt) if provided
            else if (requestParams.TryGetProperty("params", out var paramsElement))
            {
                args = paramsElement;
            }

            // Build tool request
            var functionRequest = JsonRpcMessage.CreateRequest(functionName, request.Id, args);

            // Fix for CS8604: Add null check for functionName before calling GetFunctionDetails
            if (string.IsNullOrEmpty(functionName))
            {
                return ToolResponse.Error(
                    request.Id,
                    -32602,
                    "Invalid params",
                    "Tool name must not be null or empty");
            }
            var functionDetails = _toolService.GetFunctionDetails(functionName);
            
            if (functionDetails.FunctionArgumentType.IsToolConnector)
            {
                return ToolResponse.Error(
                    request.Id,
                    -32601,
                    "Tool requires streaming",
                    "This tool must be called via StreamMessage, not tools/call");
            }

            object? result = null;

            // If the tool expects a TypedJsonRpc<T>, wrap the JsonRpcMessage accordingly.
            if (functionDetails.FunctionArgumentType.IsTypedJsonRpc)
            {
                var paramType = functionDetails.FunctionArgumentType.ParameterType;

                var functionTypedRequest = Activator.CreateInstance(paramType, functionRequest)
                    ?? throw new ToolInternalErrorException($"{functionName}: Failed to create TypedJsonRpc instance for parameter type '{paramType}'");

                result = _toolService.InvokeFunctionDelegate(functionName, functionDetails, functionTypedRequest);
            }
            else
            {
                result = _toolService.InvokeFunctionDelegate(functionName, functionDetails, functionRequest);
            }

            var processedResult = await ProcessToolResultAsync(result, functionDetails, false, request.Id, cancellationToken);

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

            // TOOL: Wrap result in MCP content format
            if (functionDetails.FunctionType == FunctionTypeEnum.Tool)
            {
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

            if (functionDetails.FunctionType == FunctionTypeEnum.Prompt)
            {
                return ToolResponse.Success(request.Id, resultData);
            }

            throw new ToolNotFoundException("Unknown function type.");

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
        ToolService.FunctionDetails toolDetails,
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
}
