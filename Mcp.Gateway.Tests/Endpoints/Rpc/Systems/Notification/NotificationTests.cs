namespace Mcp.Gateway.Tests.Endpoints.Rpc.Systems.Notification;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net;
using System.Net.Http.Json;
using Xunit;

[Collection("ServerCollection")]
public class NotificationTests(McpGatewayFixture fixture)
{
    private readonly string ToolPath = "system_notification";

    [Fact]
    public async Task Notification_OverHttp_Returns204NoContent()
    {
        // Arrange - notification has no "id" field
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            @params = new { message = "Test notification" }
        };

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);

        // Assert - notifications return 204 No Content
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
