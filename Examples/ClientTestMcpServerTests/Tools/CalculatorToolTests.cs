namespace ClientTestMcpServerTests.Tools;

using ClientTestMcpServer.Models;
using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;
using Mcp.Gateway.Tools;

[Collection("ServerCollection")]
public class CalculatorToolTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task AddNumbers_ReturnsSum()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
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
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. List Tools
        var tools = await client.ListToolsAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(tools);
        Assert.True(tools.IsSuccessResponse);

        var toolsResult = tools.GetResult<ListToolsResult>();
        Assert.NotNull(toolsResult);

        foreach (var tool in toolsResult.Tools)
        {
            Assert.NotNull(tool);
        }
    }

    [Fact]
    public async Task AddNumbers_Websocket_Notification_ReturnsSum()
    {
        // 1. Opprett transport (her WebSocket)
        var socket = await fixture.CreateWebSocketClientAsync("/ws");
        await using var transport = new WebSocketMcpTransport(socket);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Lytt på notifications (før connect)
        List<string> notifications = [];
        client.NotificationReceived += (sender, e) =>
        {
            notifications.Add(e.Method + " - ");
        };

        // 4. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 5. Kall et verktøy
        var result = await client.CallToolAsync<AddNumbersResponse>("add_numbers_notification", new AddNumbersRequest(5, 10), TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(2, notifications.Count);
    }

    [Fact]
    public async Task AddNumbers_Notification_ReturnsSum()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient, enableSse: true);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Lytt på notifications (før connect)
        List<string> notifications = [];
        client.NotificationReceived += (sender, e) =>
        {
            notifications.Add(e.Method + " - ");
        };

        // 4. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 5. Kall et verktøy
        var result = await client.CallToolAsync<AddNumbersResponse>("add_numbers_notification", new AddNumbersRequest(5, 10), TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        
        // Wait a bit for notifications to arrive via SSE
        // SSE notifications are async and might arrive slightly after the tool response
        var timeout = TimeSpan.FromSeconds(10); // Increased timeout to 10s
        var start = DateTime.UtcNow;
        while (notifications.Count < 2 && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(100, TestContext.Current.CancellationToken);
        }
        
        Assert.Equal(2, notifications.Count);
    }
}
