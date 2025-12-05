namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.BinaryStreams;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class StreamsOutTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task StreamOut_SendsBinaryData_ClientReceivesAll()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_binary_streams_out",
            id = "stream-out-1"
        };

        var requestJson = JsonSerializer.Serialize(request, JsonOptions.Default);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        // Act - Send request
        await ws.SendAsync(requestBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // Receive messages
        var buffer = new byte[4096];
        var receivedMessages = new List<string>();
        var binaryChunks = new List<byte[]>();

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), fixture.CancellationToken);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                receivedMessages.Add(json);
                
                // Check if it's done message
                if (json.Contains("\"type\":\"done\""))
                    break;
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                var chunk = new byte[result.Count];
                Array.Copy(buffer, chunk, result.Count);
                binaryChunks.Add(chunk);
            }
        }

        // Assert
        Assert.True(receivedMessages.Count >= 2, "Should receive at least start and done messages");
        Assert.True(receivedMessages[0].Contains("\"type\":\"start\""), "First message should be start");
        Assert.True(receivedMessages[^1].Contains("\"type\":\"done\""), "Last message should be done");
        Assert.True(binaryChunks.Count > 0, "Should receive binary chunks");

        // Cleanup
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", fixture.CancellationToken);
        }
    }
}
