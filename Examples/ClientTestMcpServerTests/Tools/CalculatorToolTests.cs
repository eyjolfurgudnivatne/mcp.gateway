namespace ClientTestMcpServerTests.Tools;

using ClientTestMcpServer.Models;
using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;

[Collection("ServerCollection")]
public class CalculatorToolTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task AddNumbers_ReturnsSum()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient, "/rpc");
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Kall et verktøy
        var result = await client.CallToolAsync<AddNumbersResponse>("add_numbers", new AddNumbersRequest(5, 10), TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddNumbers_Inspect_Lifecycle()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient, "/rpc");
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. List Tools
        var tools = await client.ListToolsAsync(ct: TestContext.Current.CancellationToken);

        var result = await client.CallToolAsync<AddNumbersResponse>("add_numbers", new AddNumbersRequest(5, 10), TestContext.Current.CancellationToken);
        Assert.NotNull(result);
    }
}
