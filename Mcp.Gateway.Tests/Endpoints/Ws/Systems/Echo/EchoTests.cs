namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.Echo;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

[Collection("ServerCollection")]
public class EchoTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system_echo";

    public sealed record JsonRpcEcho(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("uniqueGuid")] string UniqueGuid);

    [Fact]
    public async Task Echo_OverWebSocket_ReturnsParams()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        
        var testData = new JsonRpcEcho("Hello WS Echo!", Guid.NewGuid().ToString());
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            id = "test-ws-echo-1",
            @params = testData
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOptions.Default);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        // Act - Send
        await ws.SendAsync(requestBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // Act - Receive
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), fixture.CancellationToken);

        // Assert
        var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var jsonDoc = JsonDocument.Parse(responseJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("result", out var resultProp));
        Assert.True(resultProp.TryGetProperty("message", out var message));
        Assert.Equal(testData.Message, message.GetString());
        
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", fixture.CancellationToken);
    }
}
