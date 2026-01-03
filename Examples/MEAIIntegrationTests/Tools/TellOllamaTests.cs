namespace MEAIIntegrationTests.Tools;

using Mcp.Gateway.Tools;
using MEAIIntegrationTests.Fixture;
using System;
using System.Net.Http.Json;

[Collection("ServerCollection")]
public class TellOllamaTests(MEAIIntegrationFixture fixture)
{
    /// <summary>
    /// This will fail during normal test run. See README
    /// </summary>
    /// <returns></returns>
    [Fact]
    public async Task Tell_Ollama_ReturnResults()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "tell_ollama",
            Guid.NewGuid().ToString("D"),
            new MEAIIntegration.Tools.TellMEAIOllama.TellRequest(MEAIIntegration.Tools.TellMEAIOllama.SecretFormat.Hex));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to tell Ollama");

        var result = content.GetResult<MEAIIntegration.Tools.TellMEAIOllama.TellResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Response);
    }
}