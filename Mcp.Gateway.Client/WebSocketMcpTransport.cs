namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// WebSocket transport implementation for MCP.
/// Supports full bidirectional communication (requests, responses, notifications).
/// </summary>
public class WebSocketMcpTransport : IMcpTransport
{
    private readonly WebSocket _socket;
    private readonly Uri? _uri;
    private readonly Channel<JsonRpcMessage> _incomingMessages = Channel.CreateUnbounded<JsonRpcMessage>();
    private readonly CancellationTokenSource _disposeCts = new();
    private Task? _receiveTask;
    private bool _disposed;

    public bool IsBidirectional => true;

    public WebSocketMcpTransport(string url) : this(new Uri(url)) { }

    public WebSocketMcpTransport(Uri uri)
    {
        _socket = new ClientWebSocket();
        _uri = uri;
    }

    public WebSocketMcpTransport(WebSocket socket)
    {
        _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        _uri = null;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_socket.State == WebSocketState.Open)
        {
            if (_receiveTask == null)
            {
                _receiveTask = ReceiveLoopInternalAsync(_disposeCts.Token);
            }
            return;
        }

        if (_socket is ClientWebSocket clientSocket && _uri != null)
        {
            await clientSocket.ConnectAsync(_uri, ct);
            _receiveTask = ReceiveLoopInternalAsync(_disposeCts.Token);
        }
        else
        {
            throw new InvalidOperationException("WebSocket is not connected and cannot be connected automatically (not a ClientWebSocket or missing URI).");
        }
    }

    public async Task SendAsync(JsonRpcMessage message, CancellationToken ct = default)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions.Default);
        await _socket.SendAsync(json, WebSocketMessageType.Text, true, ct);
    }

    public IAsyncEnumerable<JsonRpcMessage> ReceiveLoopAsync(CancellationToken ct = default)
    {
        return _incomingMessages.Reader.ReadAllAsync(ct);
    }

    private async Task ReceiveLoopInternalAsync(CancellationToken ct)
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (_socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server closed", CancellationToken.None);
                        _incomingMessages.Writer.TryComplete();
                        return;
                    }
                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                ms.Position = 0;
                
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = await JsonSerializer.DeserializeAsync<JsonRpcMessage>(ms, JsonOptions.Default, ct);
                    if (message != null)
                    {
                        message = NormalizeMessageId(message);
                        await _incomingMessages.Writer.WriteAsync(message, ct);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _incomingMessages.Writer.TryComplete(ex);
        }
        finally
        {
            _incomingMessages.Writer.TryComplete();
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _disposeCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Ignore if already disposed
        }

        if (_socket.State == WebSocketState.Open)
        {
            try { await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disposing", CancellationToken.None); } catch { }
        }
        _socket.Dispose();
        _disposeCts.Dispose();
    }
}
