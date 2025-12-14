namespace CalculatorMcpServerTests.Tools;

using CalculatorMcpServer.Models;
using CalculatorMcpServerTests.Fixture;
using Mcp.Gateway.Tools;
using System;
using System.Net.Http.Json;

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
        var request = JsonRpcMessage.CreateRequest(
            "multiply_numbers",
            Guid.NewGuid().ToString("D"),
            new MultiplyRequest(5, 10));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to multiply numbers");

        var result = content.GetResult<MultiplyResponse>();
        Assert.NotNull(result);
        Assert.Equal(50, result.Result);
    }
}
