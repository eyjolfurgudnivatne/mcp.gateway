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

    /// <inheritdoc/>
    public bool IsBidirectional => true;

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioMcpTransport"/> class using the standard input and output streams.
    /// </summary>
    public StdioMcpTransport() : this(Console.OpenStandardInput(), Console.OpenStandardOutput()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="StdioMcpTransport"/> class using the specified input and output streams.
    /// </summary>
    /// <param name="inputStream">The input stream to read from.</param>
    /// <param name="outputStream">The output stream to write to.</param>
    public StdioMcpTransport(Stream inputStream, Stream outputStream)
        : this(inputStream, outputStream, false)
    {
    }

    // Private constructor to support primary constructor style and avoid code duplication
    private StdioMcpTransport(Stream inputStream, Stream outputStream, bool _)
    {
        _inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
        _outputStream = outputStream ?? throw new ArgumentNullException(nameof(outputStream));
    }

    /// <inheritdoc/>
    public Task ConnectAsync(CancellationToken ct = default)
    {
        _receiveTask ??= ReceiveLoopInternalAsync(_disposeCts.Token);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task SendAsync(JsonRpcMessage message, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(message, JsonOptions.Default);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json + "\n");
        await _outputStream.WriteAsync(bytes, ct);
        await _outputStream.FlushAsync(ct);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

        GC.SuppressFinalize(this);
    }
}
