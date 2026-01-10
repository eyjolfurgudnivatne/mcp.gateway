namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// Default implementation of IMcpClient.
/// Handles JSON-RPC message correlation, initialization, and tool/resource/prompt invocations.
/// </summary>
public class McpClient(IMcpTransport transport) : IMcpClient
{
    private readonly IMcpTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    private readonly ConcurrentDictionary<object, TaskCompletionSource<JsonRpcMessage>> _pendingRequests = new();
    private readonly Channel<JsonRpcMessage> _incomingMessages = Channel.CreateUnbounded<JsonRpcMessage>();
    private Task? _receiveLoopTask;
    private readonly CancellationTokenSource _disposeCts = new();
    private int _nextId = 1;
    private bool _isInitialized = false;

    /// <inheritdoc/>
    public ServerCapabilities? ServerCapabilities { get; private set; }
    /// <inheritdoc/>
    public ImplementationInfo? ServerInfo { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<NotificationMessage>? NotificationReceived;

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken ct = default)
    {
        await _transport.ConnectAsync(ct);

        // Start processing incoming messages from transport
        _receiveLoopTask = ProcessTransportMessagesAsync(_disposeCts.Token);

        // Send initialize request
        var initRequest = JsonRpcMessage.CreateRequest("initialize", GetNextId(), new
        {
            protocolVersion = "2025-11-25",
            capabilities = new { },
            clientInfo = new
            {
                name = "mcp-dotnet-client",
                version = "1.0.0"
            }
        });

        var response = await SendRequestAsync(initRequest, ct);

        if (response.Error != null)
        {
            throw new McpClientException($"Initialization failed: {response.Error.Message} ({response.Error.Code})");
        }

        var result = response.GetResult<JsonElement>();
        if (result.TryGetProperty("capabilities", out var caps))
        {
            ServerCapabilities = caps.Deserialize<ServerCapabilities>(JsonOptions.Default);
        }
        if (result.TryGetProperty("serverInfo", out var info))
        {
            ServerInfo = info.Deserialize<ImplementationInfo>(JsonOptions.Default);
        }

        // Send initialized notification
        var initializedNotify = JsonRpcMessage.CreateNotification("notifications/initialized");
        await _transport.SendAsync(initializedNotify, ct);
        
        _isInitialized = true;
    }

    /// <inheritdoc/>
    public async Task PingAsync(CancellationToken ct = default)
    {
        // "ping" er en vanlig konvensjon i JSON-RPC.
        // https://modelcontextprotocol.io/specification/2025-11-25/schema#pingrequest
        var request = JsonRpcMessage.CreateRequest("ping", GetNextId(), new { });

        var response = await SendRequestAsync(request, ct);

        if (response?.Error != null)
        {
            throw new McpClientException("Ping failed", response.Error);
        }
    }

    /// <inheritdoc/>
    public async Task<ListToolsResult?> ListToolsAsync(string? cursor = null, CancellationToken ct = default)
    {
        EnsureInitialized();
        var paramsObj = cursor != null ? new { cursor } : (object)new { };
        var result = await SendRequestAsync(JsonRpcMessage.CreateRequest("tools/list", GetNextId(), paramsObj), ct);
        if (result is null || !result.IsSuccessResponse)
            return null;
        return result.GetResult<ListToolsResult>();
    }

    /// <inheritdoc/>
    public async Task<TResult?> CallToolAsync<TResult>(string toolName, object? arguments, CancellationToken ct = default)
    {
        EnsureInitialized();
        arguments ??= new { };
        var request = JsonRpcMessage.CreateRequest("tools/call", GetNextId(), new
        {
            name = toolName,
            arguments
        });

        var response = await SendRequestAsync(request, ct);

        if (response.Error != null)
        {
            throw new McpClientException($"Tool call failed: {response.Error.Message}", response.Error);
        }

        // Helper to extract result from MCP content format if needed
        return response.GetToolsCallResult<TResult>();
    }

    /// <inheritdoc/>
    public async Task CallToolAsync(string toolName, object? arguments, CancellationToken ct = default)
    {
        EnsureInitialized();
        arguments ??= new { };
        var request = JsonRpcMessage.CreateRequest("tools/call", GetNextId(), new
        {
            name = toolName,
            arguments
        });

        var response = await SendRequestAsync(request, ct);

        if (response.Error != null)
        {
            throw new McpClientException($"Tool call failed: {response.Error.Message}", response.Error);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TResult> CallToolStreamAsync<TResult>(string toolName, object arguments, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("tools/call", GetNextId(), new
        {
            name = toolName,
            arguments
        });

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
        if (request.Id == null) throw new ArgumentException("Request must have an ID", nameof(request));
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

        // We need a channel to stream responses
        var channel = Channel.CreateUnbounded<JsonRpcMessage>();
        
        // Register the channel for this request ID
        // Note: We need a way to distinguish between single-response and multi-response requests in _pendingRequests
        // Currently _pendingRequests stores TaskCompletionSource<JsonRpcMessage>.
        // We need to refactor _pendingRequests to support streaming or handle it differently.
        
        // Refactoring strategy:
        // Instead of changing _pendingRequests type (which would be a larger change), 
        // let's introduce a separate dictionary for streaming requests: _streamingRequests.
        // But ProcessTransportMessagesAsync needs to check both.
        
        // Let's use a common interface or base type? No, simpler to just add another dictionary.
        // But wait, ProcessTransportMessagesAsync needs to know where to route the message.
        
        // Alternative: Use a custom TCS-like object that can handle multiple results?
        // Or just use Channel<JsonRpcMessage> in _pendingRequests?
        // If we change _pendingRequests to ConcurrentDictionary<object, Channel<JsonRpcMessage>>, 
        // then single-response methods can just read the first item.
        
        // Let's try to adapt SendRequestAsync to use Channel internally?
        // That would be a good refactor.
        
        // However, to minimize changes and risk, let's add _activeStreams.
        // If a message ID is in _activeStreams, write to that channel.
        // If it's in _pendingRequests, set the TCS.
        
        // But we need to handle the case where a response comes in.
        
        // Let's implement _activeStreams.
        
        _activeStreams[request.Id] = channel;

        try
        {
            await _transport.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _activeStreams.TryRemove(request.Id, out _);
            throw new McpClientException($"Failed to send tool stream request: {ex.Message}", null);
        }

        try
        {
            // Yield results as they come in
            await foreach (var response in channel.Reader.ReadAllAsync(ct))
            {
                if (response.Error != null)
                {
                    throw new McpClientException($"Tool stream failed: {response.Error.Message}", response.Error);
                }

                var result = response.GetToolsCallResult<TResult>();
                if (result != null)
                {
                    yield return result;
                }
            }
        }
        finally
        {
            _activeStreams.TryRemove(request.Id, out _);
        }
    }

    private readonly ConcurrentDictionary<object, Channel<JsonRpcMessage>> _activeStreams = new();

    /// <inheritdoc/>
    public async Task<ListResourcesResult?> ListResourcesAsync(string? cursor = null, CancellationToken ct = default)
    {
        EnsureInitialized();
        var paramsObj = cursor != null ? new { cursor } : (object)new { };
        var result = await SendRequestAsync(JsonRpcMessage.CreateRequest("resources/list", GetNextId(), paramsObj), ct);
        if (result is null || !result.IsSuccessResponse)
            return null;
        return result.GetResult<ListResourcesResult>();
    }

    /// <inheritdoc/>
    public async Task<ResourceContent> ReadResourceAsync(string uri, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("resources/read", GetNextId(), new { uri });
        var response = await SendRequestAsync(request, ct);

        if (response.Error != null)
        {
            throw new McpClientException($"Read resource failed: {response.Error.Message}", response.Error);
        }

        // Expecting { contents: [ { uri, mimeType, text/blob } ] }
        var result = response.GetResult<JsonElement>();
        if (result.TryGetProperty("contents", out var contents) && contents.GetArrayLength() > 0)
        {
            var content = contents[0].Deserialize<ResourceContent>(JsonOptions.Default);
            return content ?? throw new McpClientException("Failed to deserialize resource content");
        }

        throw new McpClientException("No content returned for resource");
    }

    /// <inheritdoc/>
    public async Task SubscribeResourceAsync(string uri, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("resources/subscribe", GetNextId(), new { uri });
        var response = await SendRequestAsync(request, ct);

        if (response.Error != null)
        {
            throw new McpClientException($"Subscribe failed: {response.Error.Message}", response.Error);
        }
    }

    /// <inheritdoc/>
    public async Task<ListPromptsResult?> ListPromptsAsync(string? cursor = null, CancellationToken ct = default)
    {
        EnsureInitialized();
        var paramsObj = cursor != null ? new { cursor } : (object)new { };

        var result = await SendRequestAsync(JsonRpcMessage.CreateRequest("prompts/list", GetNextId(), paramsObj), ct);
        if (result is null || !result.IsSuccessResponse)
            return null;
        return result.GetResult<ListPromptsResult>();
    }

    /// <inheritdoc/>
    public async Task<PromptResponse?> GetPromptAsync(string name, object arguments, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("prompts/get", GetNextId(), new
        {
            name,
            arguments
        });

        var result = await SendRequestAsync(request, ct);
        if (result is null || !result.IsSuccessResponse)
            return null;
        return result.GetResult<PromptResponse>();
    }

    /// <inheritdoc/>
    public async Task<PromptResponse?> GetPromptAsync(PromptRequest promptRequest, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("prompts/get", GetNextId(), promptRequest);

        var result = await SendRequestAsync(request, ct);
        if (result is null || !result.IsSuccessResponse)
            return null;
        return result.GetResult<PromptResponse>();
    }

    /// <inheritdoc/>
    public async Task<PromptResponse?> GetPromptAsync<TArguments>(PromptRequest<TArguments> promptRequest, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("prompts/get", GetNextId(), promptRequest);

        var result = await SendRequestAsync(request, ct);
        if (result is null || !result.IsSuccessResponse)
            return null;
        return result.GetResult<PromptResponse>();
    }

    private async Task<JsonRpcMessage> SendRequestAsync(JsonRpcMessage request, CancellationToken ct)
    {
        if (request.Id == null) throw new ArgumentException("Request must have an ID", nameof(request));

        var tcs = new TaskCompletionSource<JsonRpcMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[request.Id] = tcs;

        try
        {
            await _transport.SendAsync(request, ct);
            
            // Wait for response
            using var reg = ct.Register(() => tcs.TrySetCanceled());
            return await tcs.Task;
        }
        finally
        {
            _pendingRequests.TryRemove(request.Id, out _);
        }
    }

    private async Task ProcessTransportMessagesAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var message in _transport.ReceiveLoopAsync(ct))
            {
                // Console.WriteLine($"Received message: ID={message.Id} ({message.Id?.GetType().Name}), Method={message.Method}, IsResponse={message.IsResponse}");
                
                if (message.IsResponse || message.IsErrorResponse)
                {
                    if (message.Id != null)
                    {
                        // 1. Check streaming requests first
                        if (_activeStreams.TryGetValue(message.Id, out var channel))
                        {
                            await channel.Writer.WriteAsync(message, ct);
                            // Do NOT remove from _activeStreams here, as we expect multiple messages.
                            // The stream is removed when CallToolStreamAsync finishes (e.g. via cancellation).
                            continue; 
                        }
                        
                        // 2. Check pending single-response requests
                        // Try exact match first
                        if (_pendingRequests.TryGetValue(message.Id, out var tcs))
                        {
                            tcs.TrySetResult(message);
                        }
                        // Fallback: Try converting to int/long/string if types mismatch
                        else
                        {
                            // Handle int/long mismatch (e.g. request was int, response is long)
                            object? matchingKey = null;
                            
                            if (message.Id is long l && l <= int.MaxValue && l >= int.MinValue)
                            {
                                var i = (int)l;
                                if (_pendingRequests.ContainsKey(i)) matchingKey = i;
                                else if (_activeStreams.ContainsKey(i)) matchingKey = i; // Check streams too
                            }
                            else if (message.Id is int i)
                            {
                                var l2 = (long)i;
                                if (_pendingRequests.ContainsKey(l2)) matchingKey = l2;
                                else if (_activeStreams.ContainsKey(l2)) matchingKey = l2; // Check streams too
                            }

                            if (matchingKey != null)
                            {
                                if (_activeStreams.TryGetValue(matchingKey, out channel))
                                {
                                    await channel.Writer.WriteAsync(message, ct);
                                }
                                else if (_pendingRequests.TryGetValue(matchingKey, out tcs))
                                {
                                    tcs.TrySetResult(message);
                                }
                            }
                            else
                            {
                                // Console.WriteLine($"Could not find pending request for ID={message.Id}");
                            }
                        }
                    }
                }
                else if (message.IsNotification)
                {
                    // Handle notifications
                    if (message.Method != null)
                    {
                        var notification = new NotificationMessage(message.JsonRpc, message.Method, message.Params);
                        NotificationReceived?.Invoke(this, notification);
                    }
                }
                else if (message.IsRequest)
                {
                    // Server-to-client requests (e.g. sampling) - not implemented yet
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            // Log or handle transport error
            Console.WriteLine($"Transport error: {ex}");
        }
    }

    private object GetNextId() => _nextId++;

    private void EnsureInitialized()
    {
        if (!_isInitialized) throw new InvalidOperationException("Client not initialized. Call ConnectAsync() first.");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        _disposeCts.Cancel();
        if (_receiveLoopTask != null)
        {
            try { await _receiveLoopTask; } catch { }
        }
        await _transport.DisposeAsync();
        _disposeCts.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Exception thrown by <see cref="McpClient"/> when an error occurs during MCP operations.
/// Contains optional JSON-RPC error details if available.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="McpClientException"/> class with a specified error message and optional JSON-RPC error.
/// </remarks>
/// <param name="message">The error message.</param>
/// <param name="rpcError">The JSON-RPC error, if available.</param>
public class McpClientException(string message, JsonRpcError? rpcError = null) : Exception(message)
{
    /// <summary>
    /// Gets the associated JSON-RPC error, if available.
    /// </summary>
    public JsonRpcError? RpcError { get; } = rpcError;
}
