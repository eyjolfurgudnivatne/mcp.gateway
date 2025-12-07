namespace Mcp.Gateway.Server.Tools.Systems.BinaryStreams;

using Mcp.Gateway.Tools;

public class StreamIn
{
    [McpTool("system_binary_streams_in", 
        Title = "Binary Stream In", 
        Description = "Receives binary data from client (use StreamMessage start, not tools/call)",
        InputSchema = @"{""type"":""object"",""properties"":{}}",
        Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket)]
    public static async Task StreamInTool(ToolConnector connector)
    {
        var tcs = new TaskCompletionSource();
        using var ms = new MemoryStream();
        var totalChunks = 0L;

        // Subscribe to binary chunk events
        connector.OnBinaryChunk += async (ctx, index, payload) =>
        {
            if (ctx.Id == connector.Context?.Id)
            {
                ms.Write(payload.Span);
                totalChunks++;
            }
            await Task.CompletedTask;
        };

        // Subscribe to done event
        connector.OnDone += async (ctx, summary) =>
        {
            if (ctx.Id == connector.Context?.Id)
            {
                // Upload complete
                tcs.SetResult();
            }
            await Task.CompletedTask;
        };

        // Subscribe to error event
        connector.OnError += async (ctx, error) =>
        {
            if (ctx.Id == connector.Context?.Id)
            {
                tcs.SetException(new Exception(error.Message));
            }
            await Task.CompletedTask;
        };

        // Start receive loop (uses connector.StreamMessage set by ToolInvoker)
        _ = connector.StartReceiveLoopAsync();

        // Wait for completion
        try
        {
            await tcs.Task;

            // Send success response
            await connector.SendDoneAsync(
                connector.Context!.Id,
                new { bytesReceived = ms.Length, chunks = totalChunks, message = "Upload complete" },
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Send error response
            await connector.SendErrorAsync(
                connector.Context?.Id ?? "unknown",
                new JsonRpcError(-32603, "Upload failed", new { detail = ex.Message }),
                CancellationToken.None);
        }
    }
}
