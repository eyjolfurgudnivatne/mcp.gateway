namespace Mcp.Gateway.Tools;

using Microsoft.Extensions.Logging;
using System.Text.Json;

/// <summary>
/// ToolInvoker partial class - MCP Resources support (v1.5.0)
/// Resource Subscriptions support (v1.8.0 Phase 4)
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

            // Get all resources
            var allResources = _toolService.GetAllResourceDefinitions();
            
            // Apply pagination
            var paginatedResult = Pagination.CursorHelper.Paginate(allResources, cursor, pageSize);
            
            var resourcesList = paginatedResult.Items.Select(r =>
            {
                r.MimeType ??= "text/plain";
                return r;
            }).ToList();

            // Build response with pagination
            ListResourcesResult response = new()
            {
                Resources = resourcesList,
                NextCursor = paginatedResult.NextCursor
            };
            
            return ToolResponse.Success(request.Id, response);
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
            
            // Check if 'uri' parameter exists
            if (!requestParams.TryGetProperty("uri", out var uriElement))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
            }
            
            var uri = uriElement.GetString();
            
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

            // Get function details to process the result correctly
            var functionDetails = _toolService.GetFunctionDetails(uri);

            // Process result (sync or async) using the shared tool result processor
            var processedResult = await ProcessToolResultAsync(result, functionDetails, false, request.Id, cancellationToken);

            // Extract the ResourceContent from the JsonRpcMessage result
            ResourceContent? content = null;
            if (processedResult is JsonRpcMessage msg && msg.Result != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(msg.Result, JsonOptions.Default);
                    content = JsonSerializer.Deserialize<ResourceContent>(json, JsonOptions.Default);
                }
                catch
                {
                    // Fallback: treat result as plain text
                    content = new ResourceContent(
                        Uri: uri,
                        MimeType: resourceDef.MimeType,
                        Text: msg.Result is string s ? s : JsonSerializer.Serialize(msg.Result, JsonOptions.Default)
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
    /// Handles MCP resources/subscribe request (v1.8.0 Phase 4).
    /// Subscribes a session to updates for a specific resource URI.
    /// Exact URI matching only (no wildcards in v1.8.0).
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <returns>JSON-RPC response indicating success or failure</returns>
    private JsonRpcMessage HandleResourcesSubscribe(JsonRpcMessage request)
    {
        try
        {
            // Extract session ID from HttpContext (if available)
            string? sessionId = null;
            
            if (_httpContextAccessor?.HttpContext is not null)
            {
                sessionId = _httpContextAccessor.HttpContext.Request.Headers["MCP-Session-Id"].ToString();
                
                // Fallback: Check HttpContext.Items (set by StreamableHttpEndpoint)
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = _httpContextAccessor.HttpContext.Items["SessionId"] as string;
                }
            }
            
            if (string.IsNullOrEmpty(sessionId))
            {
                return ToolResponse.Error(
                    request.Id,
                    -32000,
                    "Session required",
                    new { detail = "Resource subscriptions require an active session. Use POST /mcp to initialize." });
            }

            var requestParams = request.GetParams();
            
            // Check if 'uri' parameter exists
            if (!requestParams.TryGetProperty("uri", out var uriElement))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
            }
            
            var uri = uriElement.GetString();
            
            if (string.IsNullOrEmpty(uri))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
            }

            // Validate that resource exists
            try
            {
                _ = _toolService.GetResourceDefinition(uri);
            }
            catch (ToolNotFoundException)
            {
                return ToolResponse.Error(
                    request.Id,
                    -32601,
                    "Resource not found",
                    new { detail = $"Resource '{uri}' is not configured" });
            }

            // Get ResourceSubscriptionRegistry from DI
            if (_serviceProvider is null)
            {
                _logger.LogWarning("IServiceProvider not available for resource subscriptions");
                return ToolResponse.Error(
                    request.Id,
                    -32603,
                    "Internal error",
                    new { detail = "Subscription service not available" });
            }


            if (_serviceProvider.GetService(typeof(ResourceSubscriptionRegistry))
                is not ResourceSubscriptionRegistry subscriptionRegistry)
            {
                _logger.LogError("ResourceSubscriptionRegistry not found in DI container");
                return ToolResponse.Error(
                    request.Id,
                    -32603,
                    "Internal error",
                    new { detail = "Subscription service not available" });
            }

            // Subscribe session to resource
            var wasAdded = subscriptionRegistry.Subscribe(sessionId, uri);

            //_logger.LogInformation(
            //    "Session '{SessionId}' subscribed to resource '{Uri}' (new={WasAdded})",
            //    sessionId, uri, wasAdded);

            return ToolResponse.Success(request.Id, new { subscribed = true, uri });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in resources/subscribe");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }

    /// <summary>
    /// Handles MCP resources/unsubscribe request (v1.8.0 Phase 4).
    /// Unsubscribes a session from updates for a specific resource URI.
    /// </summary>
    /// <param name="request">The JSON-RPC request</param>
    /// <returns>JSON-RPC response indicating success or failure</returns>
    private JsonRpcMessage HandleResourcesUnsubscribe(JsonRpcMessage request)
    {
        try
        {
            // Extract session ID from HttpContext (if available)
            string? sessionId = null;
            
            if (_httpContextAccessor?.HttpContext is not null)
            {
                sessionId = _httpContextAccessor.HttpContext.Request.Headers["MCP-Session-Id"].ToString();
                
                // Fallback: Check HttpContext.Items (set by StreamableHttpEndpoint)
                if (string.IsNullOrEmpty(sessionId))
                {
                    sessionId = _httpContextAccessor.HttpContext.Items["SessionId"] as string;
                }
            }
            
            if (string.IsNullOrEmpty(sessionId))
            {
                return ToolResponse.Error(
                    request.Id,
                    -32000,
                    "Session required",
                    new { detail = "Resource subscriptions require an active session. Use POST /mcp to initialize." });
            }

            var requestParams = request.GetParams();
            
            // Check if 'uri' parameter exists
            if (!requestParams.TryGetProperty("uri", out var uriElement))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
            }
            
            var uri = uriElement.GetString();
            
            if (string.IsNullOrEmpty(uri))
            {
                return ToolResponse.Error(request.Id, -32602, "Invalid params", "Missing 'uri' parameter");
            }

            // Get ResourceSubscriptionRegistry from DI
            if (_serviceProvider is null)
            {
                _logger.LogWarning("IServiceProvider not available for resource subscriptions");
                return ToolResponse.Error(
                    request.Id,
                    -32603,
                    "Internal error",
                    new { detail = "Subscription service not available" });
            }


            if (_serviceProvider.GetService(typeof(ResourceSubscriptionRegistry))
                is not ResourceSubscriptionRegistry subscriptionRegistry)
            {
                _logger.LogError("ResourceSubscriptionRegistry not found in DI container");
                return ToolResponse.Error(
                    request.Id,
                    -32603,
                    "Internal error",
                    new { detail = "Subscription service not available" });
            }

            // Unsubscribe session from resource
            var wasRemoved = subscriptionRegistry.Unsubscribe(sessionId, uri);
            
            //_logger.LogInformation(
            //    "Session '{SessionId}' unsubscribed from resource '{Uri}' (removed={WasRemoved})",
            //    sessionId, uri, wasRemoved);

            return ToolResponse.Success(request.Id, new { unsubscribed = true, uri });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in resources/unsubscribe");
            return ToolResponse.Error(request.Id, -32603, "Internal error", new { detail = ex.Message });
        }
    }
}
