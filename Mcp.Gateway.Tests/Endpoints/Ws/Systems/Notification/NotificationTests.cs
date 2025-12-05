namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.Notification;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class NotificationTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system_notification";

    [Fact]
    public async Task Notification_OverWebSocket_ReturnsNothing()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            @params = new { message = "Test WS notification" }
            // No "id" = notification
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOptions.Default);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        // Act - Send notification
        await ws.SendAsync(requestBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // Wait a bit to ensure no response comes back
        var buffer = new byte[4096];
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));
        
        try
        {
            await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            Assert.Fail("Notification should not return a response");
        }
        catch (OperationCanceledException)
        {
            // Expected - no response received within timeout
            Assert.True(true);
        }
        
        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", fixture.CancellationToken);
    }
}
