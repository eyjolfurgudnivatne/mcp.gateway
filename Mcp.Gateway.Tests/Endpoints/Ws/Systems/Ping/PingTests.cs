namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.Ping;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class PingTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system_ping";

    [Fact]
    public async Task Ping_OverWebSocket_ReturnsPong()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            id = "test-ws-1",
            @params = new { }
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOptions.Default);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        // Act - Send request
        await ws.SendAsync(
            requestBytes,
            WebSocketMessageType.Text,
            endOfMessage: true,
            fixture.CancellationToken);

        // Act - Receive response
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(
            new ArraySegment<byte>(buffer),
            fixture.CancellationToken);

        // Assert
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        
        var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var jsonDoc = JsonDocument.Parse(responseJson);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("result", out var resultProp), $"No 'result' in response: {responseJson}");
        Assert.True(resultProp.TryGetProperty("message", out var message));
        Assert.Equal("Pong", message.GetString());
        
        // Cleanup
        await ws.CloseAsync(
            WebSocketCloseStatus.NormalClosure,
            "Test complete",
            fixture.CancellationToken);
    }
}
