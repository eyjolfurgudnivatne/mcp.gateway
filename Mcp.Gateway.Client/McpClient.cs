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
public class McpClient : IMcpClient
{
    private readonly IMcpTransport _transport;
    private readonly ConcurrentDictionary<object, TaskCompletionSource<JsonRpcMessage>> _pendingRequests = new();
    private readonly Channel<JsonRpcMessage> _incomingMessages = Channel.CreateUnbounded<JsonRpcMessage>();
    private Task? _receiveLoopTask;
    private readonly CancellationTokenSource _disposeCts = new();
    private int _nextId = 1;
    private bool _isInitialized = false;

    public object? ServerCapabilities { get; private set; }
    public object? ServerInfo { get; private set; }

    public event EventHandler<NotificationMessage>? NotificationReceived;

    public McpClient(IMcpTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

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
            ServerCapabilities = caps.Deserialize<object>(JsonOptions.Default);
        }
        if (result.TryGetProperty("serverInfo", out var info))
        {
            ServerInfo = info.Deserialize<object>(JsonOptions.Default);
        }

        // Send initialized notification
        var initializedNotify = JsonRpcMessage.CreateNotification("notifications/initialized");
        await _transport.SendAsync(initializedNotify, ct);

        _isInitialized = true;
    }

    public async Task<JsonRpcMessage> ListToolsAsync(string? cursor = null, CancellationToken ct = default)
    {
        EnsureInitialized();
        var paramsObj = cursor != null ? new { cursor } : (object)new { };
        return await SendRequestAsync(JsonRpcMessage.CreateRequest("tools/list", GetNextId(), paramsObj), ct);
    }

    public async Task<TResult?> CallToolAsync<TResult>(string toolName, object arguments, CancellationToken ct = default)
    {
        EnsureInitialized();
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

    public async Task<JsonRpcMessage> ListResourcesAsync(string? cursor = null, CancellationToken ct = default)
    {
        EnsureInitialized();
        var paramsObj = cursor != null ? new { cursor } : (object)new { };
        return await SendRequestAsync(JsonRpcMessage.CreateRequest("resources/list", GetNextId(), paramsObj), ct);
    }

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

    public async Task<JsonRpcMessage> ListPromptsAsync(string? cursor = null, CancellationToken ct = default)
    {
        EnsureInitialized();
        var paramsObj = cursor != null ? new { cursor } : (object)new { };
        return await SendRequestAsync(JsonRpcMessage.CreateRequest("prompts/list", GetNextId(), paramsObj), ct);
    }

    public async Task<JsonRpcMessage> GetPromptAsync(string name, object arguments, CancellationToken ct = default)
    {
        EnsureInitialized();
        var request = JsonRpcMessage.CreateRequest("prompts/get", GetNextId(), new
        {
            name,
            arguments
        });
        return await SendRequestAsync(request, ct);
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
                            }
                            else if (message.Id is int i)
                            {
                                var l2 = (long)i;
                                if (_pendingRequests.ContainsKey(l2)) matchingKey = l2;
                            }

                            if (matchingKey != null && _pendingRequests.TryGetValue(matchingKey, out tcs))
                            {
                                tcs.TrySetResult(message);
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

    public async ValueTask DisposeAsync()
    {
        _disposeCts.Cancel();
        if (_receiveLoopTask != null)
        {
            try { await _receiveLoopTask; } catch { }
        }
        await _transport.DisposeAsync();
        _disposeCts.Dispose();
    }
}

public class McpClientException : Exception
{
    public JsonRpcError? RpcError { get; }

    public McpClientException(string message, JsonRpcError? rpcError = null) : base(message)
    {
        RpcError = rpcError;
    }
}
