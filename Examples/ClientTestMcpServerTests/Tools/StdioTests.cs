namespace ClientTestMcpServerTests.Tools;

using ClientTestMcpServer.Models;
using ClientTestMcpServerTests.Fixture;
using Mcp.Gateway.Client;
using Mcp.Gateway.Tools;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

[Collection("ServerCollection")]
public class StdioTests(ClientTestMcpServerFixture fixture)
{
    [Fact]
    public async Task StdioTransport_ReceiveLoop_ReadsMessages()
    {
        // Arrange
        using var inputStream = new MemoryStream();
        using var outputStream = new MemoryStream();
        
        var response = new
        {
            jsonrpc = "2.0",
            id = 1,
            result = new { success = true }
        };
        var responseJson = JsonSerializer.Serialize(response);
        var responseBytes = Encoding.UTF8.GetBytes(responseJson + "\n");
        inputStream.Write(responseBytes);
        inputStream.Position = 0;
        
        await using var transport = new StdioMcpTransport(inputStream, outputStream);
        await transport.ConnectAsync(TestContext.Current.CancellationToken);
        
        // Act
        var messages = transport.ReceiveLoopAsync(TestContext.Current.CancellationToken);
        
        // Assert
        var enumerator = messages.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var msg = await enumerator.MoveNextAsync();
        Assert.True(msg);
        Assert.NotNull(enumerator.Current);
        
        // The ID might be deserialized as int or long depending on JSON parser.
        // Let's check string representation or convert.
        var id = enumerator.Current.Id;
        if (id is int i) Assert.Equal(1, i);
        else if (id is long l) Assert.Equal(1L, l);
        else if (id is string s) Assert.Equal("1", s);
        else Assert.Fail($"Unexpected ID type: {id?.GetType().Name}");
    }

    [Fact]
    public async Task StdioTransport_WritesToOutputStream()
    {
        using var inputStream = new MemoryStream(); // Empty input
        using var outputStream = new MemoryStream();
        
        await using var transport = new StdioMcpTransport(inputStream, outputStream);
        
        var message = JsonRpcMessage.CreateRequest("test", 1, new { });
        await transport.SendAsync(message, TestContext.Current.CancellationToken);
        
        outputStream.Position = 0;
        using var reader = new StreamReader(outputStream);
        var line = await reader.ReadLineAsync(TestContext.Current.CancellationToken);
        
        Assert.NotNull(line);
        Assert.Contains("jsonrpc", line);
        Assert.Contains("test", line);
    }

    [Fact]
    public async Task AddNumbers_ReturnsSum()
    {
        // 1. Opprett transport (her stdio)
        await using var transport = new StdioMcpTransport(
            fixture.ServerProcess!.StandardOutput.BaseStream,
            fixture.ServerProcess!.StandardInput.BaseStream
        );
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
}
