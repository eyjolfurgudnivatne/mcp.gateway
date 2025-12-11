namespace DevTestServer.Tools.Systems.BinaryStreams;

using Mcp.Gateway.Tools;

public class StreamOut
{
    [McpTool("system_binary_streams_out", 
        Title = "Binary Stream Out", 
        Description = "Streams binary data to client (use StreamMessage start, not tools/call)",
        InputSchema = @"{""type"":""object"",""properties"":{}}",
        Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket)]
    public static async Task StreamOutTool(ToolConnector connector)
    {
        var meta = new StreamMessageMeta(
            Method: "result.data",
            Binary: true,
            Mime: "application/octet-stream");

        using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);

        // Send some test data in chunks
        var testData = System.Text.Encoding.UTF8.GetBytes("Hello from binary stream!");
        
        for (int i = 0; i < testData.Length; i += 5)
        {
            var chunkSize = Math.Min(5, testData.Length - i);
            await handle.WriteAsync(testData.AsMemory(i, chunkSize));
        }

        await handle.CompleteAsync(new { totalBytes = testData.Length, message = "Stream complete" });
    }
}
