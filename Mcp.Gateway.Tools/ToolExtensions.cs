namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    /// Adds ToolService, ToolInvoker, NotificationService, EventIdGenerator, SessionService, SseStreamRegistry, and ResourceSubscriptionRegistry to the DI container.
    /// Tools, Prompts, and Resources are scanned lazily on first use.
    /// </summary>
    /// <remarks>
    /// v1.6.0: Added NotificationService
    /// v1.7.0: Added EventIdGenerator, SessionService, and SseStreamRegistry for MCP 2025-11-25 compliance
    /// v1.8.0: Added ResourceSubscriptionRegistry for resource subscriptions, Lifecycle hooks are optional - register your own implementations via AddSingleton&lt;IToolLifecycleHook&gt;
    /// v1.8.0 Phase 4: SessionService cleanup callback for resource subscriptions
    /// </remarks>
    /// <param name="builder"></param>
    /// <param name="serverInfoConfigKey">Key in appsettings where server information is stored (typeof ImplementationInfo)</param>
    public static void AddToolsService(this WebApplicationBuilder builder, string serverInfoConfigKey = "ServerInfo")
    {
        builder.Services.AddSingleton<ToolService>();
        builder.Services.AddScoped<ToolInvoker>();
        builder.Services.AddSingleton<EventIdGenerator>();     // v1.7.0
        builder.Services.AddSingleton<SseStreamRegistry>();    // v1.7.0 Phase 2
        builder.Services.AddSingleton<ResourceSubscriptionRegistry>();  // v1.8.0 Phase 4

        // Configure ImplementationInfo from "ServerInfo" section
        builder.Services.Configure<ImplementationInfo>(builder.Configuration.GetSection(serverInfoConfigKey));
        builder.Services.PostConfigure<ImplementationInfo>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Name))
                options.Name = "mcp-gateway";

            if (string.IsNullOrWhiteSpace(options.Version))
                options.Version = typeof(ToolExtensions).Assembly.GetName().Version?.ToString() ?? "2.0.0";
        });

        // Register SessionService with cleanup callback (v1.8.0 Phase 4)
        builder.Services.AddSingleton<SessionService>(sp =>
        {
            var subscriptionRegistry = sp.GetService<ResourceSubscriptionRegistry>();
            
            // Create SessionService with cleanup callback
            return new SessionService(
                sessionTimeout: null,  // Use default timeout
                onSessionDeleted: sessionId =>
                {
                    // Cleanup resource subscriptions when session is deleted/expired
                    subscriptionRegistry?.ClearSession(sessionId);
                });
        });
        
        // Register NotificationService with ResourceSubscriptionRegistry (v1.8.0 Phase 4)
        builder.Services.AddSingleton<Notifications.INotificationSender>(sp =>
        {
            var eventIdGenerator = sp.GetRequiredService<EventIdGenerator>();
            var sessionService = sp.GetRequiredService<SessionService>();
            var sseRegistry = sp.GetRequiredService<SseStreamRegistry>();
            var logger = sp.GetRequiredService<ILogger<Notifications.NotificationService>>();
            var subscriptionRegistry = sp.GetService<ResourceSubscriptionRegistry>();
            
            return new Notifications.NotificationService(
                eventIdGenerator,
                sessionService,
                sseRegistry,
                logger,
                subscriptionRegistry);
        });
        
        // v1.8.0: Lifecycle hooks are optional
        // Users can register their own hooks via:
        // builder.Services.AddSingleton<IToolLifecycleHook, MyCustomHook>();
        // 
        // Built-in hooks available:
        // - LoggingToolLifecycleHook (simple ILogger integration)
        // - MetricsToolLifecycleHook (in-memory metrics)
    }

    /// <summary>
    /// Adds a tool lifecycle hook for monitoring tool invocations (v1.8.0).
    /// Multiple hooks can be registered and will be invoked in registration order.
    /// </summary>
    /// <typeparam name="T">Hook implementation type</typeparam>
    /// <param name="builder">WebApplicationBuilder</param>
    /// <returns>WebApplicationBuilder for chaining</returns>
    /// <example>
    /// <code>
    /// // Add logging hook
    /// builder.AddToolsService();
    /// builder.AddToolLifecycleHook&lt;LoggingToolLifecycleHook&gt;();
    /// 
    /// // Add metrics hook
    /// builder.AddToolLifecycleHook&lt;MetricsToolLifecycleHook&gt;();
    /// 
    /// // Add custom hook
    /// builder.AddToolLifecycleHook&lt;MyCustomHook&gt;();
    /// </code>
    /// </example>
    public static WebApplicationBuilder AddToolLifecycleHook<T>(this WebApplicationBuilder builder)
        where T : class, Lifecycle.IToolLifecycleHook
    {
        builder.Services.AddSingleton<Lifecycle.IToolLifecycleHook, T>();
        return builder;
    }
}
