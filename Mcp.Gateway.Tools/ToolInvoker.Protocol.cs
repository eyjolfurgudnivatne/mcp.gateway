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
            
            if (message.Method == "tools/list")
            {
                // Use transport-aware filtering
                return HandleFunctionsList(message, transport);
            }

            if (message.Method == "prompts/list")
            {
                return HandlePromptsList(message);
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

            // MCP Resource Subscriptions (v1.8.0 Phase 4)
            if (message.Method == "resources/subscribe")
            {
                return HandleResourcesSubscribe(message);
            }
            
            if (message.Method == "resources/unsubscribe")
            {
                return HandleResourcesUnsubscribe(message);
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

            // Invoke the tool with lifecycle hooks (v1.8.0)
            return await InvokeToolWithHooksAsync(
                message.Method,
                message,
                async () =>
                {
                    // Invoke the tool
                    var result = _toolService.InvokeFunctionDelegate(
                        message.Method,
                        toolDetails,
                        args);

                    // Handle different return types
                    return await ProcessToolResultAsync(result, toolDetails, message.IsNotification, id, cancellationToken);
                });
        }
        catch (ToolNotFoundException ex)
        {
            _logger.LogWarning(ex, "Tool not found: {Method}", ex.Message);
            
            // Suggest similar tool names (v1.8.0)
            var allTools = _toolService.GetAllFunctionDefinitions(FunctionTypeEnum.Tool)
                .Select(t => t.Name)
                .ToList();
            
            var requestedTool = id?.ToString() ?? "unknown";
            var similarTools = StringSimilarity.FindSimilarStrings(
                ex.Message.Replace("Function '", "").Replace("' is not configured.", ""),
                allTools,
                maxResults: 3,
                maxDistance: 3);
            
            if (similarTools.Any())
            {
                return ToolResponse.Error(id, -32601, "Method not found", new
                {
                    detail = ex.Message,
                    requestedTool = ex.Message.Replace("Function '", "").Replace("' is not configured.", ""),
                    suggestions = similarTools,
                    hint = $"Did you mean: {string.Join(", ", similarTools)}?"
                });
            }
            
            return ToolResponse.Error(id, -32601, "Method not found", new { detail = ex.Message });
        }
        catch (ToolInvalidParamsException ex)
        {
            _logger.LogWarning(ex, "Invalid params for tool");
            
            // Try to get tool details for schema information (v1.8.0)
            try
            {
                // Use ToolName from exception if available
                var toolName = ex.ToolName;
                if (!string.IsNullOrEmpty(toolName))
                {
                    var toolDef = _toolService.GetAllFunctionDefinitions(FunctionTypeEnum.Tool)
                        .FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
                    
                    if (toolDef is not null && !string.IsNullOrEmpty(toolDef.InputSchema))
                    {
                        // Parse schema to extract helpful info
                        var schemaDoc = JsonDocument.Parse(toolDef.InputSchema);
                        var schemaRoot = schemaDoc.RootElement;
                        
                        // Extract required fields
                        var requiredFields = new List<string>();
                        if (schemaRoot.TryGetProperty("required", out var reqProp))
                        {
                            foreach (var field in reqProp.EnumerateArray())
                            {
                                var fieldName = field.GetString();
                                if (!string.IsNullOrEmpty(fieldName))
                                    requiredFields.Add(fieldName);
                            }
                        }
                        
                        // Build example with ALL required fields (v1.8.0)
                        var exampleParams = new List<string>();
                        if (schemaRoot.TryGetProperty("properties", out var propsProp))
                        {
                            foreach (var prop in propsProp.EnumerateObject())
                            {
                                if (requiredFields.Contains(prop.Name))
                                {
                                    var propType = prop.Value.TryGetProperty("type", out var typeProp) 
                                        ? typeProp.GetString() ?? "unknown" 
                                        : "unknown";
                                    
                                    var exampleValue = propType switch
                                    {
                                        "string" => "\"example\"",
                                        "number" => "42",
                                        "integer" => "42",
                                        "boolean" => "true",
                                        _ => "..."
                                    };
                                    
                                    exampleParams.Add($"\"{prop.Name}\": {exampleValue}");
                                }
                            }
                        }
                        
                        var exampleJson = exampleParams.Any() 
                            ? $"{{ {string.Join(", ", exampleParams)} }}" 
                            : null;
                        
                        return ToolResponse.Error(id, -32602, "Invalid params", new
                        {
                            detail = ex.Message,
                            tool = toolName,
                            requiredFields = requiredFields.Any() ? requiredFields : null,
                            example = exampleJson,
                            hint = requiredFields.Any() 
                                ? $"Required fields: {string.Join(", ", requiredFields)}" 
                                : "Check tool schema for parameter details"
                        });
                    }
                }
            }
            catch
            {
                // If schema extraction fails, fall back to simple error
            }
            
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
        bool isPrompts = _toolService.GetAllPromptsDefinitions().Any();
        bool hasResources = _toolService.GetAllResourceDefinitions().Any();

        // https://modelcontextprotocol.io/specification/2025-11-25/schema#servercapabilities
        Dictionary<string, object> capabilities = [];

        if (isTools)
        {
            capabilities["tools"] = _notificationSender is not null ? new { listChanged = true } : new { };
        }
        if (isPrompts)
        {
            capabilities["prompts"] = _notificationSender is not null ? new { listChanged = true } : new { };
        }
        if (hasResources)
        {
            capabilities["resources"] = _notificationSender is not null ? new { listChanged = true, subscribe = true } : new { };
        }
        
        // Make protocol version configurable for compatibility with older clients
        var protocolVersion = Environment.GetEnvironmentVariable("MCP_PROTOCOL_VERSION") ?? "2025-11-25";

        var serverInfo = _implementationInfoOptions.Value;

        return ToolResponse.Success(request.Id, new
        {
            protocolVersion, // Configurable protocol version
            serverInfo,
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
                        schema = JsonSerializer.Deserialize<object>(t.InputSchema!, JsonOptions.Default);
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

                    // Add outputSchema if present (MCP 2025-11-25)
                    if (!string.IsNullOrEmpty(t.OutputSchema))
                    {
                        try
                        {
                            var outputSchemaObj = JsonSerializer.Deserialize<object>(t.OutputSchema, JsonOptions.Default);
                            toolObj["outputSchema"] = outputSchemaObj!;
                        }
                        catch
                        {
                            // Skip outputSchema if deserialization fails
                        }
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
    /// Handles MCP prompt/list request.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    private JsonRpcMessage HandlePromptsList(JsonRpcMessage request)
    {
        try
        {
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
            var allPrompts = _toolService.GetAllPromptsDefinitions();
            var paginatedResult = Pagination.CursorHelper.Paginate(allPrompts, cursor, pageSize);


            // Prompts: serialize with arguments (array)
            var promptsList = paginatedResult.Items.ToList();

            // Build response with pagination
            var response = new ListPromptsResult
            {
                Prompts = promptsList,
                NextCursor = paginatedResult.NextCursor
            };

            return ToolResponse.Success(request.Id, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in prompts/list");
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
    private async Task<object?> HandleFunctionsCallAsync(JsonRpcMessage request, CancellationToken cancellationToken)
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

            // Invoke tool with lifecycle hooks (v1.8.0)
            var processedResult = await InvokeToolWithHooksAsync(
                functionName,
                functionRequest,
                async () =>
                {
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

                    return await ProcessToolResultAsync(result, functionDetails, false, request.Id, cancellationToken);
                });
            
            // Handle IAsyncEnumerable (streaming) result
            if (processedResult is IAsyncEnumerable<JsonRpcMessage> asyncEnumerable)
            {
                // For streaming tools, we can't just return a single JsonRpcMessage.
                // The caller (McpMiddleware) needs to handle this.
                // But McpMiddleware expects a single object response.
                
                // If we are in HTTP context (StreamableHttpEndpoint), we can't easily stream multiple JSON responses 
                // unless we use SSE or a specific streaming format.
                // BUT, the user's test uses HttpMcpTransport which expects standard JSON-RPC responses.
                // If the server sends multiple JSON objects concatenated, the client might be able to read them.
                
                // However, `StreamableHttpEndpoint` writes the response as JSON:
                // await context.Response.WriteAsJsonAsync(response, ct);
                
                // If `response` is `IAsyncEnumerable`, `WriteAsJsonAsync` will serialize it as a JSON array `[...]`.
                // This is NOT what we want for streaming. We want multiple individual JSON-RPC response objects.
                
                // To support this, we need to change `StreamableHttpEndpoint` to handle `IAsyncEnumerable`.
                // But `InvokeSingleAsync` returns `Task<object?>`.
                
                // Let's return the enumerable and let the endpoint handle it.
                return (dynamic)asyncEnumerable; 
            }

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
                // Check if result is already in MCP content format (has "content" field)
                // This happens when tools use ToolResponse.SuccessWithStructured()
                if (resultData is not null)
                {
                    var resultJson = JsonSerializer.Serialize(resultData, JsonOptions.Default);
                    var resultDoc = JsonDocument.Parse(resultJson);
                    
                    // If result already has "content" field, use it directly (structured content support)
                    if (resultDoc.RootElement.TryGetProperty("content", out _))
                    {
                        return ToolResponse.Success(request.Id, resultData);
                    }
                }
                
                // Otherwise, wrap result in MCP content format (legacy behavior)
                var serializedResult = JsonSerializer.Serialize(resultData, JsonOptions.Default);
                var mcpResult = new
                {
                    content = new []
                    {
                        new
                        {
                            type = "text",
                            text = serializedResult
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
        // IAsyncEnumerable<JsonRpcMessage> (streaming)
        if (result is IAsyncEnumerable<JsonRpcMessage> asyncEnumerable)
        {
            // For streaming tools, we iterate and send each message directly to the transport?
            // But ToolInvoker doesn't have access to the transport directly here.
            // It returns a result to the caller (McpMiddleware).
            
            // However, McpMiddleware expects a single response object.
            // If we return IAsyncEnumerable, McpMiddleware needs to handle it.
            
            // BUT, looking at existing code in McpMiddleware (not shown here but inferred),
            // it likely serializes the result.
            
            // Wait, if the tool returns IAsyncEnumerable, we can't just return it as a single object
            // unless we buffer it (which defeats the purpose) or if the caller handles it.
            
            // Let's check if we can return the enumerable itself and let the middleware handle it?
            // Or do we need to execute it here?
            
            // If we look at how `CounterTools` is implemented:
            // public async IAsyncEnumerable<JsonRpcMessage> CountTo10Tool(JsonRpcMessage request)
            
            // This returns an IAsyncEnumerable.
            // If we return this object, the caller (McpMiddleware) needs to know how to stream it.
            
            return asyncEnumerable;
        }

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
