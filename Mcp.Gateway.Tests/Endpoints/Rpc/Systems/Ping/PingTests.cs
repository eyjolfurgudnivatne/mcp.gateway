namespace Mcp.Gateway.Tests.Endpoints.Rpc.Systems.Ping;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class PingTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system_ping";

    [Fact]
    public async Task Ping_OverHttp_ReturnsPong()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            id = "test-1",
            @params = new { }
        };

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        
        // Debug output
        System.Diagnostics.Debug.WriteLine($"Response: {content}");
        
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("result", out var result), $"No 'result' property in response: {content}");
        Assert.True(result.TryGetProperty("message", out var message));
        Assert.Equal("Pong", message.GetString());
    }
}
