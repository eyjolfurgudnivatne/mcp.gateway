namespace ClientTestMcpServerTests;

using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;
using System;

[Collection("ServerCollection")]
public class SystemPingTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task SystemPing_ReturnsOk()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Kall ping
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // Rask sjekk
        try
        {
            await client.PingAsync(cts.Token);
        }
        catch (Exception)
        {
            Assert.Fail("System ping failed");
        }
    }

    [Fact]
    public async Task SystemPing_Websocket_ReturnsOk()
    {
        // 1. Opprett transport (her WebSocket)
        var socket = await fixture.CreateWebSocketClientAsync("/ws");
        await using var transport = new WebSocketMcpTransport(socket);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Kall ping
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1)); // Rask sjekk
        try
        {
            await client.PingAsync(cts.Token);
        }
        catch (Exception)
        {
            Assert.Fail("System ping failed");
        }
    }
}
