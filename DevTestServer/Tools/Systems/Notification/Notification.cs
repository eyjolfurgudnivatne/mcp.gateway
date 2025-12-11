namespace DevTestServer.Tools.Systems.Notification;

using Mcp.Gateway.Tools;

public class Notification
{
    [McpTool("system_notification", 
        Title = "Notification", 
        Description = "Sends a notification (no response expected)",
        InputSchema = @"{""type"":""object"",""properties"":{}}")]
    public async Task NotificationTool(JsonRpcMessage message)
    {
        await Task.CompletedTask;
    }
}

