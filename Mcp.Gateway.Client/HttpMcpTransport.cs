namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// HTTP transport implementation for MCP.
/// Uses HTTP POST for RPC calls.
/// Does not support server-initiated notifications (unless SSE is added).
/// </summary>
public class HttpMcpTransport : IMcpTransport
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly Channel<JsonRpcMessage> _incomingMessages = Channel.CreateUnbounded<JsonRpcMessage>();
    private readonly bool _ownsHttpClient;

    public bool IsBidirectional => false;

    public HttpMcpTransport(string baseUrl, string endpoint = "/mcp")
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _endpoint = endpoint;
        _ownsHttpClient = true;
    }

    public HttpMcpTransport(HttpClient httpClient, string endpoint = "/mcp")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint;
        _ownsHttpClient = false;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        // HTTP is stateless, no connection needed
        return Task.CompletedTask;
    }

    public async Task SendAsync(JsonRpcMessage message, CancellationToken ct = default)
    {
        // Send request
        var response = await _httpClient.PostAsJsonAsync(_endpoint, message, JsonOptions.Default, ct);
        response.EnsureSuccessStatusCode();

        // Read response
        // Note: Notifications might return 204 No Content
        if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            var responseMessage = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(JsonOptions.Default, ct);
            if (responseMessage != null)
            {
                // Fix ID if it's a JsonElement (System.Text.Json deserializes object properties as JsonElement)
                var fixedMsg = NormalizeMessageId(responseMessage);
                await _incomingMessages.Writer.WriteAsync(fixedMsg, ct);
            }
        }
    }

    private static JsonRpcMessage NormalizeMessageId(JsonRpcMessage message)
    {
        if (message.Id is JsonElement idElem)
        {
            object? fixedId = idElem.ValueKind switch
            {
                JsonValueKind.String => idElem.GetString(),
                JsonValueKind.Number => idElem.TryGetInt32(out var i) ? i : idElem.GetInt64(),
                JsonValueKind.Null => null,
                _ => idElem.ToString()
            };
            return message with { Id = fixedId };
        }
        return message;
    }

    public IAsyncEnumerable<JsonRpcMessage> ReceiveLoopAsync(CancellationToken ct = default)
    {
        return _incomingMessages.Reader.ReadAllAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        _incomingMessages.Writer.TryComplete();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        await Task.CompletedTask;
    }
}
