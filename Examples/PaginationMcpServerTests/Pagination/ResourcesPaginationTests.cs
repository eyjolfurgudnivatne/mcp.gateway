namespace PaginationMcpServerTests.Pagination;

using PaginationMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class ResourcesPaginationTests(PaginationMcpServerFixture fixture)
{
    [Fact]
    public async Task ResourcesList_WithPageSize10_ReturnsFirst10Resources()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/list",
            id = "test-1",
            @params = new { pageSize = 10 }
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
        Assert.Equal(10, resources.Count);

        // Verify resource structure (should have uri, name, description, mimeType)
        var firstResource = resources.First();
        Assert.True(firstResource.TryGetProperty("uri", out var uri));
        Assert.StartsWith("mock://resource/", uri.GetString());
        Assert.True(firstResource.TryGetProperty("mimeType", out var mimeType));
        Assert.Equal("application/json", mimeType.GetString());

        // Should have nextCursor
        Assert.True(result.TryGetProperty("nextCursor", out var nextCursor));
        Assert.False(string.IsNullOrEmpty(nextCursor.GetString()));
    }

    [Fact]
    public async Task ResourcesList_WithCursor_ReturnsNextPage()
    {
        // Arrange - Get first page with cursor
        var request1 = new
        {
            jsonrpc = "2.0",
            method = "resources/list",
            id = "test-1",
            @params = new { pageSize = 10 }
        };

        var response1 = await fixture.HttpClient.PostAsJsonAsync("/rpc", request1, fixture.CancellationToken);
        var content1 = await response1.Content.ReadAsStringAsync(fixture.CancellationToken);
        var doc1 = JsonDocument.Parse(content1).RootElement;
        var cursor = doc1.GetProperty("result").GetProperty("nextCursor").GetString();

        // Act - Get second page
        var request2 = new
        {
            jsonrpc = "2.0",
            method = "resources/list",
            id = "test-2",
            @params = new { cursor, pageSize = 10 }
        };

        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request2, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("resources", out var resourcesElement));

        var resources = resourcesElement.EnumerateArray().ToList();
        Assert.Equal(10, resources.Count); // Second page

        // Should NOT have nextCursor (last page with 20 total resources)
        Assert.False(result.TryGetProperty("nextCursor", out _));
    }
}
