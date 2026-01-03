namespace ClientTestMcpServerTests.Tools;

using ClientTestMcpServer.Models;
using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;
using System.Threading.Tasks;
using Xunit;

[Collection("ServerCollection")]
public class StreamingToolTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task CountTo10_ReturnsStreamOfNumbers()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Kall verktøy som streamer
        var count = 0;
        var lastValue = 0;
        
        // Use a cancellation token to stop the stream if it hangs (since we don't have an explicit "done" message yet)
        // But CountTo10Tool sends exactly 10 messages.
        // The client will keep waiting for the 11th message.
        // So we need to cancel manually or break the loop.
        
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10)); // Safety timeout

        try
        {
            await foreach (var result in client.CallToolStreamAsync<CountTo10Response>("count_to_10", new { }, cts.Token))
            {
                Assert.NotNull(result);
                count++;
                lastValue = result.Result;
                
                // Since we know we expect 10 items, we can break manually to finish the test gracefully
                if (count == 10)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if we timeout waiting for more messages
        }

        Assert.Equal(10, count);
        Assert.Equal(10, lastValue);
    }
}
