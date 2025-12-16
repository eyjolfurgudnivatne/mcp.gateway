namespace CalculatorMcpServerTests.Tools;

using CalculatorMcpServer.Models;
using CalculatorMcpServerTests.Fixture;
using Mcp.Gateway.Tools;
using System;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

[Collection("ServerCollection")]
public class CalculatorToolTests(CalculatorMcpServerFixture fixture)
{
    [Fact]
    public async Task AddNumbers_ReturnsSum()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "add_numbers",
            Guid.NewGuid().ToString("D"),
            new AddNumbersRequest(5, 10));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to add numbers");

        var result = content.GetResult<AddNumbersResponse>();
        Assert.NotNull(result);
        Assert.Equal(15, result.Result);
    }

    [Fact]
    public async Task MultiplyNumbers_ReturnsProduct()
    {
        // Arrange
        using var ws = await fixture.CreateWebSocketClientAsync("/ws");
        var request = JsonRpcMessage.CreateRequest(
            "multiply_numbers",
            Guid.NewGuid().ToString("D"),
            new MultiplyRequest(5, 10));

        var requestJson = JsonSerializer.Serialize(request, JsonOptions.Default);
        var requestBytes = Encoding.UTF8.GetBytes(requestJson);

        // Act - Send
        await ws.SendAsync(requestBytes, WebSocketMessageType.Text, true, fixture.CancellationToken);

        // Act - Receive
        var buffer = new byte[4096];
        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), fixture.CancellationToken);

        // Assert
        var responseJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

        var content = JsonSerializer.Deserialize<JsonRpcMessage>(responseJson, JsonOptions.Default);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to multiply numbers");

        var cResult = content.GetResult<MultiplyResponse>();
        Assert.NotNull(cResult);
        Assert.Equal(50, cResult.Result);
    }
}
