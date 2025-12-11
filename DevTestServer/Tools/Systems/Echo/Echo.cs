namespace DevTestServer.Tools.Systems.Echo;

using Mcp.Gateway.Tools;

internal class Echo
{
    [McpTool("system_echo", 
        Title = "Echo", 
        Description = "Echoes back the input parameters",
        InputSchema = @"{
            ""type"":""object"",
            ""properties"":{
                ""message"":{""type"":""string"",""description"":""Message to echo""}
            }
        }")]
    //public async Task<JsonRpcMessage> EchoTool(JsonRpcMessage message)
    public JsonRpcMessage EchoTool(JsonRpcMessage message)
    {
        //await Task.CompletedTask;
        return ToolResponse.Success(message.Id, message.Params);
    }
}
