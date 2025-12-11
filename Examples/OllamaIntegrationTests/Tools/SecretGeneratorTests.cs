namespace OllamaIntegrationTests.Tools;

using Mcp.Gateway.Tools;
using OllamaIntegrationTests.Fixture;
using System;
using System.Net.Http.Json;

[Collection("ServerCollection")]
public class SecretGeneratorTests(OllamaIntegrationFixture fixture)
{
    [Fact]
    public async Task Generate_Secret_ReturnsSecret()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "generate_secret",
            Guid.NewGuid().ToString("D"),
            new Mcp.Gateway.Examples.OllamaIntegration.Tools.SecretGenerator.SecretRequest("hex"));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to generate secret");

        var result = content.GetResult<Mcp.Gateway.Examples.OllamaIntegration.Tools.SecretGenerator.SecretResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Secret);
    }
}
