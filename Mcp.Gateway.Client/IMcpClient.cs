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
    Task<ListToolsResult?> ListToolsAsync(string? cursor = null, CancellationToken ct = default);
    Task<TResult?> CallToolAsync<TResult>(string toolName, object arguments, CancellationToken ct = default);
    IAsyncEnumerable<TResult> CallToolStreamAsync<TResult>(string toolName, object arguments, CancellationToken ct = default);
    
    // Resources
    Task<ListResourcesResult?> ListResourcesAsync(string? cursor = null, CancellationToken ct = default);
    Task<ResourceContent> ReadResourceAsync(string uri, CancellationToken ct = default);
    Task SubscribeResourceAsync(string uri, CancellationToken ct = default);

    // Prompts
    Task<ListPromptsResult?> ListPromptsAsync(string? cursor = null, CancellationToken ct = default);
    Task<PromptResponse?> GetPromptAsync(string name, object arguments, CancellationToken ct = default);
    Task<PromptResponse?> GetPromptAsync(PromptRequest request, CancellationToken ct = default);
    Task<PromptResponse?> GetPromptAsync<TArguments>(PromptRequest<TArguments> request, CancellationToken ct = default);

    // Notifications
    event EventHandler<NotificationMessage> NotificationReceived;
}
