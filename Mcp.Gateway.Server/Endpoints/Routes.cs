namespace Mcp.Gateway.Server.Endpoints;

using Mcp.Gateway.Server.Endpoints.Health;
using Mcp.Gateway.Tools;

internal static class Routes
{
    public static WebApplication MapEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("");

        group.MapGroup("/health")
            .MapHealthEndpoints();

        group.MapHttpRpcEndpoint("/rpc");

        group.MapWsRpcEndpoint("/ws");

        group.MapSseRpcEndpoint("/sse");

        return app;
    }
}
