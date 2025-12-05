namespace Mcp.Gateway.Tests.Endpoints.Rpc.Systems.Echo;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

[Collection("ServerCollection")]
public class EchoTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system_echo";

    public sealed record JsonRpcEcho(
        [property: JsonPropertyName("message")] string Message,
        [property: JsonPropertyName("uniqueGuid")] string UniqueGuid);

    [Fact]
    public async Task Echo_OverHttp_ReturnsParams()
    {
        // Arrange
        var testData = new JsonRpcEcho("Hello Echo!", Guid.NewGuid().ToString());
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            id = "test-echo-1",
            @params = testData
        };

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("result", out var result), $"No 'result' in response: {content}");
        Assert.True(result.TryGetProperty("message", out var message));
        Assert.Equal(testData.Message, message.GetString());
        Assert.True(result.TryGetProperty("uniqueGuid", out var guid));
        Assert.Equal(testData.UniqueGuid, guid.GetString());
    }
}
