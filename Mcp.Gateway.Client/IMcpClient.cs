namespace Mcp.Gateway.Client;

using Mcp.Gateway.Tools;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a client for the Model Context Protocol (MCP).
/// </summary>
public interface IMcpClient : IAsyncDisposable
{
    /// <summary>
    /// Connects to the MCP server and performs the handshake (initialize).
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the server capabilities received during initialization.
    /// </summary>
    ServerCapabilities? ServerCapabilities { get; }

    /// <summary>
    /// Gets the server info received during initialization.
    /// </summary>
    ImplementationInfo? ServerInfo { get; }

    // Tools
    Task<JsonRpcMessage> ListToolsAsync(string? cursor = null, CancellationToken ct = default);
    Task<TResult?> CallToolAsync<TResult>(string toolName, object arguments, CancellationToken ct = default);
    
    // Resources
    Task<JsonRpcMessage> ListResourcesAsync(string? cursor = null, CancellationToken ct = default);
    Task<ResourceContent> ReadResourceAsync(string uri, CancellationToken ct = default);
    Task SubscribeResourceAsync(string uri, CancellationToken ct = default);

    // Prompts
    Task<JsonRpcMessage> ListPromptsAsync(string? cursor = null, CancellationToken ct = default);
    Task<JsonRpcMessage> GetPromptAsync(string name, object arguments, CancellationToken ct = default);

    // Notifications
    event EventHandler<NotificationMessage> NotificationReceived;
}
