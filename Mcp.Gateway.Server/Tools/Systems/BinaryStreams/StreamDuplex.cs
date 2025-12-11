namespace Mcp.Gateway.Server.Tools.Systems.BinaryStreams;

using Mcp.Gateway.Tools;

public class StreamDuplex
{
    [McpTool("system_binary_streams_duplex", 
        Title = "Binary Stream Duplex", 
        Description = "Bidirectional binary streaming - echoes data back (use StreamMessage start, not tools/call)",
        InputSchema = @"{""type"":""object"",""properties"":{}}",
        Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket)]
    public static async Task StreamDuplexTool(ToolConnector connector)
    {
        var tcs = new TaskCompletionSource();
        var totalBytesReceived = 0L;
        var totalBytesSent = 0L;

        // Open write handle for sending back to client
        var writeMeta = new StreamMessageMeta(
            Method: "result_echo",
            Binary: true,
            Mime: "application/octet-stream");
        
        using var writeHandle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(writeMeta);

        // Subscribe to binary chunk events (receiving from client)
        connector.OnBinaryChunk += async (ctx, index, payload) =>
        {
            if (ctx.Id == connector.Context?.Id)
            {
                totalBytesReceived += payload.Length;
                
                // Echo back immediately (duplex!)
                await writeHandle.WriteAsync(payload);
                totalBytesSent += payload.Length;
            }
        };

        // Subscribe to done event (client finished sending)
        connector.OnDone += async (ctx, summary) =>
        {
            if (ctx.Id == connector.Context?.Id)
            {
                // Client is done sending, finish our write
                await writeHandle.CompleteAsync(new 
                { 
                    echoed = true,
                    bytesSent = totalBytesSent 
                });
                
                // Mark duplex complete
                tcs.SetResult();
            }
        };

        // Subscribe to error event
        connector.OnError += async (ctx, error) =>
        {
            if (ctx.Id == connector.Context?.Id)
            {
                await writeHandle.FailAsync(error);
                tcs.SetException(new Exception(error.Message));
            }
        };

        // Start receive loop
        _ = connector.StartReceiveLoopAsync();

        // Wait for duplex to complete
        try
        {
            await tcs.Task;

            // Send final done message (server-side completion)
            await connector.SendDoneAsync(
                connector.Context!.Id,
                new 
                { 
                    bytesReceived = totalBytesReceived,
                    bytesSent = totalBytesSent,
                    message = "Duplex complete" 
                },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            await connector.SendErrorAsync(
                connector.Context?.Id ?? "unknown",
                new JsonRpcError(-32603, "Duplex failed", new { detail = ex.Message }),
                CancellationToken.None);
        }
    }
}
