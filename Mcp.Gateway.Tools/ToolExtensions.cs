namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

public static class ToolExtensions
{
    /// <summary>
    /// Maps a tool group for organizing tools under a common namespace.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="toolName"></param>
    /// <returns></returns>
    public static ToolRouteBuilder MapToolGroup(this WebApplication app, string toolName) =>
        new(app, toolName);

    /// <summary>
    /// Map a tool directly to an handler.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="toolName"></param>
    /// <param name="handler"></param>
    public static void MapTool(this WebApplication app, string toolName, Delegate handler) =>
        new ToolRouteBuilder(app, "").MapTool(toolName, handler);

    /// <summary>
    /// Map a Http JSON-RPC endpoint for tool invocations.
    /// </summary>
    /// <remarks>
    /// - This will handle all requests for different mapped tools.<br></br>
    /// - No streaming support.
    /// </remarks>
    /// <param name="group"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapHttpRpcEndpoint(this RouteGroupBuilder group, string pattern)
    {
        group.MapPost(pattern, async (HttpRequest request, ToolInvoker invoker, CancellationToken ct) =>
            await invoker.InvokeHttpRpcAsync(request, ct));
        return group;
    }

    /// <summary>
    /// Map a Http JSON-RPC endpoint for tool invocations.
    /// </summary>
    /// <remarks>
    /// - This will handle all requests for different mapped tools.<br></br>
    /// - No streaming support.
    /// </remarks>
    /// <param name="app"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static WebApplication MapHttpRpcEndpoint(this WebApplication app, string pattern)
    {
        app.MapPost(pattern, async (HttpRequest request, ToolInvoker invoker, CancellationToken ct) =>
            await invoker.InvokeHttpRpcAsync(request, ct));
        return app;
    }

    /// <summary>
    /// Map a WebSocket JSON-RPC endpoint for tool invocations.
    /// </summary>
    /// <remarks>
    /// - This will handle all requests for different mapped tools.<br></br>
    /// </remarks>
    /// <param name="group"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapWsRpcEndpoint(this RouteGroupBuilder group, string pattern)
    {
        group.Map(pattern, async (HttpContext context, ToolInvoker invoker, CancellationToken ct) =>
            await invoker.InvokeWsRpcAsync(context, ct));
        return group;
    }

    /// <summary>
    /// Map a WebSocket JSON-RPC endpoint for tool invocations.
    /// </summary>
    /// <remarks>
    /// - This will handle all requests for different mapped tools.<br></br>
    /// </remarks>
    /// <param name="app"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static WebApplication MapWsRpcEndpoint(this WebApplication app, string pattern)
    {
        app.Map(pattern, async (HttpContext context, ToolInvoker invoker, CancellationToken ct) =>
            await invoker.InvokeWsRpcAsync(context, ct));
        return app;
    }

    /// <summary>
    /// Map a SSE (Server-Sent Events) JSON-RPC endpoint for tool invocations.
    /// </summary>
    /// <remarks>
    /// - This will handle all requests for different mapped tools.<br></br>
    /// - Uses SSE for response streaming (one-way: server to client).<br></br>
    /// - Compatible with remote MCP clients (Claude Desktop, etc.)
    /// </remarks>
    /// <param name="group"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static RouteGroupBuilder MapSseRpcEndpoint(this RouteGroupBuilder group, string pattern)
    {
        group.MapPost(pattern, async (HttpContext context, ToolInvoker invoker, CancellationToken ct) =>
            await invoker.InvokeSseAsync(context, ct));
        return group;
    }

    /// <summary>
    /// Map a SSE (Server-Sent Events) JSON-RPC endpoint for tool invocations.
    /// </summary>
    /// <remarks>
    /// - This will handle all requests for different mapped tools.<br></br>
    /// - Uses SSE for response streaming (one-way: server to client).<br></br>
    /// - Compatible with remote MCP clients (Claude Desktop, etc.)
    /// </remarks>
    /// <param name="app"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static WebApplication MapSseRpcEndpoint(this WebApplication app, string pattern)
    {
        app.MapPost(pattern, async (HttpContext context, ToolInvoker invoker, CancellationToken ct) =>
            await invoker.InvokeSseAsync(context, ct));
        return app;
    }

    /// <summary>
    /// Adds ToolService and PromptService to the DI container.
    /// Tools and Prompts are scanned lazily on first use.
    /// </summary>
    /// <param name="builder"></param>
    public static void AddToolsService(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ToolService>();
        builder.Services.AddScoped<ToolInvoker>();
    }
}
