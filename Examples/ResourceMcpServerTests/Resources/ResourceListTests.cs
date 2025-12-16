namespace ResourceMcpServerTests.Resources;

using ResourceMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class ResourceListTests(ResourceMcpServerFixture fixture)
{
    [Fact]
    public async Task ResourcesList_ReturnsAllResources()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/list",
            id = "res-list-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("resources", out var resourcesElement));

        var resources = resourcesElement.EnumerateArray().ToList();
        Assert.True(resources.Count >= 6, $"Expected at least 6 resources, got {resources.Count}");

        // Verify file://logs/app.log resource
        var appLog = resources.FirstOrDefault(r => r.GetProperty("uri").GetString() == "file://logs/app.log");
        Assert.True(appLog.ValueKind != JsonValueKind.Undefined, "App log resource not found");
        Assert.Equal("Application Logs", appLog.GetProperty("name").GetString());
        Assert.Equal("text/plain", appLog.GetProperty("mimeType").GetString());
        Assert.False(string.IsNullOrEmpty(appLog.GetProperty("description").GetString()));

        // Verify system://status resource
        var systemStatus = resources.FirstOrDefault(r => r.GetProperty("uri").GetString() == "system://status");
        Assert.True(systemStatus.ValueKind != JsonValueKind.Undefined, "System status resource not found");
        Assert.Equal("System Status", systemStatus.GetProperty("name").GetString());
        Assert.Equal("application/json", systemStatus.GetProperty("mimeType").GetString());

        // Verify db://users/example resource
        var exampleUser = resources.FirstOrDefault(r => r.GetProperty("uri").GetString() == "db://users/example");
        Assert.True(exampleUser.ValueKind != JsonValueKind.Undefined, "Example user resource not found");
        Assert.Equal("Example User", exampleUser.GetProperty("name").GetString());
        Assert.Equal("application/json", exampleUser.GetProperty("mimeType").GetString());
    }

    [Fact]
    public async Task ResourcesList_HasValidUriFormat()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/list",
            id = "res-list-2"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("resources", out var resourcesElement));

        var resources = resourcesElement.EnumerateArray().ToList();

        foreach (var resource in resources)
        {
            var uri = resource.GetProperty("uri").GetString();
            Assert.NotNull(uri);
            
            // Verify URI format (scheme://path)
            Assert.Contains("://", uri);
            
            var parts = uri.Split("://");
            Assert.Equal(2, parts.Length);
            Assert.NotEmpty(parts[0]); // Scheme
            Assert.NotEmpty(parts[1]); // Path
        }
    }
}
