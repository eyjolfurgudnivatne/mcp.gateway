namespace OllamaIntegrationTests.Tools;

using Mcp.Gateway.Tools;
using OllamaIntegrationTests.Fixture;
using System;
using System.Net.Http.Json;

[Collection("ServerCollection")]
public class TellOllamaTests(OllamaIntegrationFixture fixture)
{
    [Fact]
    public async Task Tell_Ollama_ReturnResults()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "tell_ollama",
            Guid.NewGuid().ToString("D"),
            new Mcp.Gateway.Examples.OllamaIntegration.Tools.TellOllama.TellRequest("base64"));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to tell Ollama");

        var result = content.GetResult<Mcp.Gateway.Examples.OllamaIntegration.Tools.TellOllama.TellResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Response);
    }
}