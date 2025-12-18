namespace DevTestServer.Endpoints;

using DevTestServer.Endpoints.Health;
using Mcp.Gateway.Tools;

internal static class Routes
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("");

        group.MapGroup("/health")
            .MapHealthEndpoints();

        // MCP 2025-11-25 unified endpoint (NEW in v1.7.0)
        app.UseProtocolVersionValidation();
        app.MapStreamableHttpEndpoint("/mcp");

        // Legacy endpoints (still supported)
        group.MapHttpRpcEndpoint("/rpc");
        group.MapWsRpcEndpoint("/ws");
        group.MapSseRpcEndpoint("/sse");

        return app;
    }
}
