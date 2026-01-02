namespace ClientTestMcpServerTests.EpPrompts;

using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;
using Mcp.Gateway.Tools;
using System.Collections.Generic;
using static ClientTestMcpServer.EpPrompts.SantaPrompt;

[Collection("ServerCollection")]
public class SantaPromptTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task List_Prompts()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. List prompts
        var prompts = await client.ListPromptsAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(prompts);

        foreach (var prompt in prompts.Prompts)
        {
            Assert.NotNull(prompt.Name);
        }
    }

    [Fact]
    public async Task List_Websocket_Prompts()
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

        // 4. List prompts
        var prompts = await client.ListPromptsAsync(ct: TestContext.Current.CancellationToken);
        Assert.NotNull(prompts);

        foreach (var prompt in prompts.Prompts)
        {
            Assert.NotNull(prompt.Name);
        }
    }

    [Fact]
    public async Task Get_Prompt()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Get prompt
        var prompt = await client.GetPromptAsync("santa_report_prompt", new { name = "Lise", behavior = "Good" }, TestContext.Current.CancellationToken);
        Assert.NotNull(prompt);
        Assert.NotNull(prompt.Messages);

        var content = Assert.IsType<TextContent>(prompt.Messages.First().Content);
        Assert.NotNull(content.Text);

        var content2 = Assert.IsType<TextContent>(prompt.Messages.Last().Content);
        Assert.NotNull(content2.Text);
        Assert.Equal("Send a letter to Santa Claus and tell him that Lise has behaved Good.", content2.Text);
    }

    [Fact]
    public async Task Get_PromptII()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Get prompt
        var prompt = await client.GetPromptAsync(
            new PromptRequest
            {
                Name = "santa_report_prompt",
                Arguments = new Dictionary<string, object> {
                    { "name", "Ola" },
                    { "behavior", "Naughty" }
                }
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(prompt);
        Assert.NotNull(prompt.Messages);

        var content = Assert.IsType<TextContent>(prompt.Messages.First().Content);
        Assert.NotNull(content.Text);

        var content2 = Assert.IsType<TextContent>(prompt.Messages.Last().Content);
        Assert.NotNull(content2.Text);
        Assert.Equal("Send a letter to Santa Claus and tell him that Ola has behaved Naughty.", content2.Text);
    }

    [Fact]
    public async Task Get_PromptIII()
    {
        // 1. Opprett transport (her HTTP)
        await using var transport = new HttpMcpTransport(fixture.HttpClient);
        Assert.NotNull(transport);

        // 2. Opprett klient
        await using var client = new McpClient(transport);
        Assert.NotNull(client);

        // 3. Koble til (utfører handshake)
        await client.ConnectAsync(TestContext.Current.CancellationToken);

        // 4. Get prompt
        var prompt = await client.GetPromptAsync(
            new PromptRequest<SantaReportPromptRequest>
            {
                Name  = "santa_report_prompt",
                Arguments = new SantaReportPromptRequest(
                    "Ola",
                    BehaviorEnum.Naughty
                )
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(prompt);
        Assert.NotNull(prompt.Messages);

        var content = Assert.IsType<TextContent>(prompt.Messages.First().Content);
        Assert.NotNull(content.Text);

        var content2 = Assert.IsType<TextContent>(prompt.Messages.Last().Content);
        Assert.NotNull(content2.Text);
        Assert.Equal("Send a letter to Santa Claus and tell him that Ola has behaved Naughty.", content2.Text);
    }
}
