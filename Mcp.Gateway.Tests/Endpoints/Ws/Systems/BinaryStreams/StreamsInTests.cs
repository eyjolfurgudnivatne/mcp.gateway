namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.BinaryStreams;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class StreamsInTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task StreamIn_ReceivesBinaryData_ServerProcessesAll()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        
        // 1. Client sends StreamMessage start to initiate upload
        var startMsg = new
        {
            type = "start",
            id = "upload-1",
            timestamp = DateTimeOffset.UtcNow,
            meta = new
            {
                method = "system_binary_streams_in",
                binary = true,
                mime = "application/octet-stream"
            }
        };

        var startJson = JsonSerializer.Serialize(startMsg, JsonOptions.Default);
        var startBytes = Encoding.UTF8.GetBytes(startJson);
        await ws.SendAsync(startBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // 2. Client sends binary data chunks
        var testData = Encoding.UTF8.GetBytes("Hello from client upload!");
        var streamId = "upload-1";
        
        // Send all chunks (8 bytes each)
        var chunkIndex = 0L;
        for (int offset = 0; offset < testData.Length; offset += 8)
        {
            var chunkSize = Math.Min(8, testData.Length - offset);
            var payload = testData.AsSpan(offset, chunkSize);
            
            // Create binary frame: [16 bytes GUID][8 bytes index][payload]
            var header = StreamMessage.CreateBinaryHeader(streamId, chunkIndex);
            var frame = new byte[StreamMessage.BinaryHeaderSize + payload.Length];
            header.CopyTo(frame, 0);
            payload.CopyTo(frame.AsSpan(StreamMessage.BinaryHeaderSize));
            
            await ws.SendAsync(frame, WebSocketMessageType.Binary, true, fixture.CancellationToken);
            chunkIndex++;
        }

        // 3. Client sends done message
        var doneMsg = StreamMessage.CreateDoneMessage(streamId, new { uploaded = true });
        var doneJson = JsonSerializer.Serialize(doneMsg, JsonOptions.Default);
        var doneBytes = Encoding.UTF8.GetBytes(doneJson);
        await ws.SendAsync(doneBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // 4. Receive server response (done message from server)
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), fixture.CancellationToken);

        // Assert
        Assert.Equal(WebSocketMessageType.Text, result.MessageType);
        
        var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
        var jsonDoc = JsonDocument.Parse(responseJson);
        var root = jsonDoc.RootElement;

        // Debug: show what we got
        if (root.TryGetProperty("type", out var typeEl) && typeEl.GetString() == "error")
        {
            if (root.TryGetProperty("error", out var errorEl))
            {
                Assert.Fail($"Got error: {errorEl.GetRawText()}");
            }
        }

        // Should receive done message from server
        Assert.True(root.TryGetProperty("type", out var typeEl2));
        Assert.Equal("done", typeEl2.GetString());
        
        Assert.True(root.TryGetProperty("summary", out var summaryEl));
        Assert.True(summaryEl.TryGetProperty("bytesReceived", out var bytesEl));
        Assert.Equal(testData.Length, bytesEl.GetInt64());

        // Cleanup
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", fixture.CancellationToken);
        }
    }
}
