namespace NotificationMcpServer.Tools;

using Mcp.Gateway.Tools;

/// <summary>
/// Demonstrates dynamic tool registration and hot-reload notifications.
/// Tools can be added/removed at runtime, triggering notifications to clients.
/// </summary>
public class DynamicToolRegistry
{
    // Static base tools that are always available
    [McpTool("ping", Description = "Simple ping tool that always responds with pong")]
    public JsonRpcMessage Ping(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new { message = "pong" });
    }

    [McpTool("echo", Description = "Echoes back the input message")]
    public JsonRpcMessage Echo(JsonRpcMessage request)
    {
        var message = request.GetParams().GetProperty("message").GetString();
        return ToolResponse.Success(request.Id, new { echo = message });
    }

    [McpTool("get_time", Description = "Returns the current server time")]
    public JsonRpcMessage GetTime(JsonRpcMessage request)
    {
        return ToolResponse.Success(request.Id, new 
        { 
            time = DateTime.UtcNow.ToString("o"),
            timezone = "UTC"
        });
    }

    // Note: In a real implementation, you would have a mechanism to
    // dynamically register/unregister tools at runtime and trigger
    // notifications via INotificationSender.
    //
    // For this demo, we'll use the /api/notify/* endpoints to manually
    // trigger notifications, simulating what would happen when tools change.
}
