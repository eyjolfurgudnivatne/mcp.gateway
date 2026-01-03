namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Abstraction for MCP transport layer (HTTP, WebSocket, Stdio).
/// </summary>
public interface IMcpTransport : IAsyncDisposable
{
    /// <summary>
    /// Connects the transport.
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a JSON-RPC message.
    /// </summary>
    Task SendAsync(JsonRpcMessage message, CancellationToken ct = default);

    /// <summary>
    /// Receives messages from the transport.
    /// </summary>
    IAsyncEnumerable<JsonRpcMessage> ReceiveLoopAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Whether the transport supports bidirectional communication (server-initiated messages).
    /// </summary>
    bool IsBidirectional { get; }
}
