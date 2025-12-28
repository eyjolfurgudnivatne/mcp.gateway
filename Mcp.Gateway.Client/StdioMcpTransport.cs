namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/// <summary>
/// Stdio transport implementation for MCP.
/// Uses standard input/output for communication.
/// </summary>
public class StdioMcpTransport : IMcpTransport
{
    private readonly Stream _inputStream;
    private readonly Stream _outputStream;
    private readonly Channel<JsonRpcMessage> _incomingMessages = Channel.CreateUnbounded<JsonRpcMessage>();
    private readonly CancellationTokenSource _disposeCts = new();
    private Task? _receiveTask;
    private bool _disposed;

    public bool IsBidirectional => true;

    public StdioMcpTransport() : this(Console.OpenStandardInput(), Console.OpenStandardOutput()) { }

    public StdioMcpTransport(Stream inputStream, Stream outputStream)
    {
        _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        if (_receiveTask == null)
        {
            _receiveTask = ReceiveLoopInternalAsync(_disposeCts.Token);
        }
        return Task.CompletedTask;
    }

    public async Task SendAsync(JsonRpcMessage message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions.Default);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");
        await _outputStream.WriteAsync(bytes, ct);
        await _outputStream.FlushAsync(ct);
    }

    public IAsyncEnumerable<JsonRpcMessage> ReceiveLoopAsync(CancellationToken ct = default)
    {
        return _incomingMessages.Reader.ReadAllAsync(ct);
    }

    private async Task ReceiveLoopInternalAsync(CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(_inputStream, System.Text.Encoding.UTF8, leaveOpen: true);
            while (!ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line == null) break; // EOF

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var message = JsonSerializer.Deserialize<JsonRpcMessage>(line, JsonOptions.Default);
                    if (message != null)
                    {
                        message = NormalizeMessageId(message);
                        await _incomingMessages.Writer.WriteAsync(message, ct);
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed JSON lines
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
        catch (ObjectDisposedException) { }

        if (_receiveTask != null)
        {
            try { await _receiveTask; } catch { }
        }
        
        _disposeCts.Dispose();
        
        // We do not dispose the streams as they might be Console streams which should stay open
        // or managed by the caller.
    }
}
