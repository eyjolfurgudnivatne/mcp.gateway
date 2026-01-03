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

    /// <summary>
    /// Lists the available tools on the MCP server.
    /// </summary>
    /// <param name="cursor">An optional cursor for paginated results.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains a <see cref="ListToolsResult"/> with the list of tools, or <c>null</c> if the operation fails.
    /// </returns>
    Task<ListToolsResult?> ListToolsAsync(string? cursor = null, CancellationToken ct = default);

    /// <summary>
    /// Calls a tool on the MCP server with the specified arguments and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The expected type of the result returned by the tool.</typeparam>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">The arguments to pass to the tool.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the tool's response, or <c>null</c> if the operation fails.
    /// </returns>
    Task<TResult?> CallToolAsync<TResult>(string toolName, object arguments, CancellationToken ct = default);

    /// <summary>
    /// Calls a tool on the MCP server with the specified arguments and returns a stream of results.
    /// </summary>
    /// <typeparam name="TResult">The expected type of each result item returned by the tool.</typeparam>
    /// <param name="toolName">The name of the tool to call.</param>
    /// <param name="arguments">The arguments to pass to the tool.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{TResult}"/> representing the asynchronous stream of results from the tool.
    /// </returns>
    IAsyncEnumerable<TResult> CallToolStreamAsync<TResult>(string toolName, object arguments, CancellationToken ct = default);
    
    /// <summary>
    /// Lists the available resources on the MCP server.
    /// </summary>
    /// <param name="cursor">An optional cursor for paginated results.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains a <see cref="ListResourcesResult"/> with the list of resources, or <c>null</c> if the operation fails.
    /// </returns>
    Task<ListResourcesResult?> ListResourcesAsync(string? cursor = null, CancellationToken ct = default);

    /// <summary>
    /// Reads the content of a resource from the MCP server by its URI.
    /// </summary>
    /// <param name="uri">The URI of the resource to read.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains the <see cref="ResourceContent"/> of the resource.
    /// </returns>
    Task<ResourceContent> ReadResourceAsync(string uri, CancellationToken ct = default);

    /// <summary>
    /// Subscribes to updates for a resource on the MCP server by its URI.
    /// </summary>
    /// <param name="uri">The URI of the resource to subscribe to.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation.
    /// </returns>
    Task SubscribeResourceAsync(string uri, CancellationToken ct = default);

    /// <summary>
    /// Lists the available prompts on the MCP server.
    /// </summary>
    /// <param name="cursor">An optional cursor for paginated results.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation. The result contains a <see cref="ListPromptsResult"/> with the list of prompts, or <c>null</c> if the operation fails.
    /// </returns>
    Task<ListPromptsResult?> ListPromptsAsync(string? cursor = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a prompt response from the MCP server by prompt name and arguments.
    /// </summary>
    /// <param name="name">The name of the prompt to retrieve.</param>
    /// <param name="arguments">The arguments to pass to the prompt.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{PromptResponse}"/> representing the asynchronous operation. The result contains the <see cref="PromptResponse"/> for the specified prompt, or <c>null</c> if the operation fails.
    /// </returns>
    Task<PromptResponse?> GetPromptAsync(string name, object arguments, CancellationToken ct = default);

    /// <summary>
    /// Gets a prompt response from the MCP server by prompt name and arguments.
    /// </summary>
    /// <param name="request">The name and arguments to pass to the prompt.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{PromptResponse}"/> representing the asynchronous operation. The result contains the <see cref="PromptResponse"/> for the specified prompt, or <c>null</c> if the operation fails.
    /// </returns>
    Task<PromptResponse?> GetPromptAsync(PromptRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets a prompt response from the MCP server by prompt name and arguments.
    /// </summary>
    /// <param name="request">The name and arguments to pass to the prompt.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Task{PromptResponse}"/> representing the asynchronous operation. The result contains the <see cref="PromptResponse"/> for the specified prompt, or <c>null</c> if the operation fails.
    /// </returns>
    Task<PromptResponse?> GetPromptAsync<TArguments>(PromptRequest<TArguments> request, CancellationToken ct = default);

    /// <summary>
    /// Occurs when a notification message is received from the MCP server.
    /// </summary>
    event EventHandler<NotificationMessage> NotificationReceived;
}
