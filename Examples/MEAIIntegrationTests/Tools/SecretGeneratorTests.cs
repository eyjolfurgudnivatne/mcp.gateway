namespace MEAIIntegrationTests.Tools;

using Mcp.Gateway.Tools;
using MEAIIntegrationTests.Fixture;
using System;
using System.Net.Http.Json;

[Collection("ServerCollection")]
public class SecretGeneratorTests(MEAIIntegrationFixture fixture)
{
    [Fact]
    public async Task Generate_Secret_ReturnsSecret()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "generate_secret",
            Guid.NewGuid().ToString("D"),
            new MEAIIntegration.Tools.SecretGenerator.SecretRequest(MEAIIntegration.Tools.SecretGenerator.SecretFormat.Base64));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to generate secret");

        var result = content.GetResult<MEAIIntegration.Tools.SecretGenerator.SecretResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Secret);
    }
}
