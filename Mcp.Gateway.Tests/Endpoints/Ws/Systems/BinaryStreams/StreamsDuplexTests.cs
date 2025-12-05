using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Mcp.Gateway.Tests.Endpoints.Ws.Systems.BinaryStreams;

[Collection("ServerCollection")]
public class StreamsDuplexTests(McpGatewayFixture fixture)
{
    // Path to the duplex tool inside MCP
    private const string ToolPath = "system_binary_streams_duplex";    

    [Fact]
    public async Task StreamDuplex_EchoesBinaryData_ClientReceivesAll()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        
        // 1. Client sends StreamMessage start to initiate duplex
        var startMsg = new
        {
            type = "start",
            id = "duplex-1",
            timestamp = DateTimeOffset.UtcNow,
            meta = new
            {
                method = ToolPath,
                binary = true,
                mime = "application/octet-stream"
            }
        };

        var startJson = JsonSerializer.Serialize(startMsg, JsonOptions.Default);
        var startBytes = Encoding.UTF8.GetBytes(startJson);
        await ws.SendAsync(startBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // Prepare test data
        var testData = Encoding.UTF8.GetBytes("Hello duplex streaming!");
        var streamId = "duplex-1";

        // Start receiving in background task
        var receivedMessages = new List<string>();
        var receivedBinaryChunks = new List<byte[]>();
        var receiveTask = Task.Run(async () =>
        {
            var buffer = new byte[4096];
            while (ws.State == WebSocketState.Open)
            {
                try
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), fixture.CancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                        break;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        receivedMessages.Add(json);
                        
                        // Check if both done messages received
                        if (receivedMessages.Count(m => m.Contains("\"type\":\"done\"")) >= 2)
                            break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        var chunk = new byte[result.Count];
                        Array.Copy(buffer, chunk, result.Count);
                        receivedBinaryChunks.Add(chunk);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, fixture.CancellationToken);

        // 2. Client sends binary data chunks
        var chunkIndex = 0L;
        for (int offset = 0; offset < testData.Length; offset += 8)
        {
            var chunkSize = Math.Min(8, testData.Length - offset);
            var payload = testData.AsSpan(offset, chunkSize);
            
            // Create binary frame
            var header = StreamMessage.CreateBinaryHeader(streamId, chunkIndex);
            var frame = new byte[StreamMessage.BinaryHeaderSize + payload.Length];
            header.CopyTo(frame, 0);
            payload.CopyTo(frame.AsSpan(StreamMessage.BinaryHeaderSize));
            
            await ws.SendAsync(frame, WebSocketMessageType.Binary, true, fixture.CancellationToken);
            chunkIndex++;
            
            // Small delay to allow server to echo back
            await Task.Delay(10, fixture.CancellationToken);
        }

        // 3. Client sends done message
        var doneMsg = StreamMessage.CreateDoneMessage(streamId, new { uploaded = true });
        var doneJson = JsonSerializer.Serialize(doneMsg, JsonOptions.Default);
        var doneBytes = Encoding.UTF8.GetBytes(doneJson);
        await ws.SendAsync(doneBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // Wait for receive task to complete
        await Task.WhenAny(receiveTask, Task.Delay(5000, fixture.CancellationToken));

        // Assert
        // Should receive: server start, server done, client done (from server echo)
        Assert.True(receivedMessages.Count >= 2, $"Should receive at least 2 messages, got {receivedMessages.Count}");
        
        // Find start message from server
        var hasServerStart = receivedMessages.Any(m => m.Contains("\"type\":\"start\"") && m.Contains("result_echo"));
        Assert.True(hasServerStart, "Should receive server start message for echo stream");
        
        // Find done messages
        var doneMessages = receivedMessages.Where(m => m.Contains("\"type\":\"done\"")).ToList();
        Assert.True(doneMessages.Count >= 1, "Should receive at least one done message");

        // Should receive echoed binary chunks
        Assert.True(receivedBinaryChunks.Count > 0, "Should receive echoed binary chunks");

        // Cleanup
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", fixture.CancellationToken);
        }
    }
}