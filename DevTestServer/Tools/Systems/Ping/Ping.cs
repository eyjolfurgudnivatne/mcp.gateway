namespace DevTestServer.Tools.Systems.Ping;

using Mcp.Gateway.Tools;
using System.Text.Json.Serialization;

internal class Ping
{
    public sealed record JsonRpcPing(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("utcTimestamp")] DateTimeOffset UtcTimestamp,
        [property: JsonPropertyName("assemblyVersion")] string AssemblyVersion);

    [McpTool("system_ping", 
        Title = "Ping", 
        Description = "Simple ping tool that returns pong with timestamp",
        InputSchema = @"{""type"":""object"",""properties"":{}}")]
    public JsonRpcMessage PingTool(JsonRpcMessage message)
    {
        string assemblyVersion = typeof(Ping).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        var result = new JsonRpcPing("Pong", DateTimeOffset.UtcNow, assemblyVersion);

        return ToolResponse.Success(message.Id, result);
    }

}
