namespace ClientTestMcpServerTests.EpResources;

using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;

[Collection("ServerCollection")]
public class SystemResourceTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task List_Resources()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. List resources
        var resources = await client.ListResourcesAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(resources);

        foreach (var resource in resources.Resources)
        {
            Assert.NotNull(resource.Name);
        }
    }

    [Fact]
    public async Task List_Websocket_Resources()
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

        // 4. List resources
        var resources = await client.ListResourcesAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(resources);

        foreach (var resource in resources.Resources)
        {
            Assert.NotNull(resource.Name);
        }
    }

    [Fact]
    public async Task Get_Resource()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Get resource
        var resource = await client.ReadResourceAsync("system://status", TestContext.Current.CancellationToken);
        Assert.NotNull(resource);
        Assert.NotNull(resource.Contents);
        Assert.Single(resource.Contents);
        Assert.NotNull(resource.Meta);
        Assert.True(resource.Meta.ContainsKey("tools.gateway.mcp/status"));
        Assert.Equal("Hello World", resource.Meta["tools.gateway.mcp/status"].ToString());
    }
}
