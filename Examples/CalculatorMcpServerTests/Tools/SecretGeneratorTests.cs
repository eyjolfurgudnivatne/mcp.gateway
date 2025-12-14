namespace CalculatorMcpServerTests.Tools;

using CalculatorMcpServerTests.Fixture;
using Mcp.Gateway.Tools;
using System;
using System.Net.Http.Json;

[Collection("ServerCollection")]
public class SecretGeneratorTests(CalculatorMcpServerFixture fixture)
{
    [Fact]
    public async Task Generate_Secret_ReturnsSecret()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "generate_secret",
            Guid.NewGuid().ToString("D"),
            new CalculatorMcpServer.Tools.SecretGenerator.SecretRequest(CalculatorMcpServer.Tools.SecretGenerator.SecretType.Base64));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to generate secret");

        var result = content.GetResult<CalculatorMcpServer.Tools.SecretGenerator.SecretResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Secret);
    }
}
