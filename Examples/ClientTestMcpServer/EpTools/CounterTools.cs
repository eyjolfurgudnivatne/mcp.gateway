namespace ClientTestMcpServer.EpTools;

using ClientTestMcpServer.Models;
using Mcp.Gateway.Tools;

public class CounterTools
{
    [McpTool("count_to_10",
        Title = "Count to 10",
        Description = "Counts from 1 to 10 and returns the result.")]
    public async IAsyncEnumerable<JsonRpcMessage> CountTo10Tool(JsonRpcMessage request)
    {
        for (int counter = 1; counter <= 10; counter++)
        {
            yield return ToolResponse.Success(
                request.Id,
                new CountTo10Response(counter));
            await Task.Delay(500); // Simulate some delay
        }
    }
}
