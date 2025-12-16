namespace ResourceMcpServerTests.Resources;

using ResourceMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class ResourceReadTests(ResourceMcpServerFixture fixture)
{
    [Fact]
    public async Task ResourcesRead_FileResource_ReturnsContent()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/read",
            id = "res-read-1",
            @params = new
            {
                uri = "file://logs/app.log"
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("contents", out var contentsElement));

        var contents = contentsElement.EnumerateArray().ToList();
        Assert.Single(contents);

        var firstContent = contents[0];
        Assert.Equal("file://logs/app.log", firstContent.GetProperty("uri").GetString());
        Assert.Equal("text/plain", firstContent.GetProperty("mimeType").GetString());
        
        Assert.True(firstContent.TryGetProperty("text", out var text));
        var textValue = text.GetString();
        Assert.NotNull(textValue);
        Assert.NotEmpty(textValue);
        Assert.Contains("INFO:", textValue);
    }

    [Fact]
    public async Task ResourcesRead_SystemResource_ReturnsJsonContent()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/read",
            id = "res-read-2",
            @params = new
            {
                uri = "system://status"
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("contents", out var contentsElement));

        var contents = contentsElement.EnumerateArray().ToList();
        Assert.Single(contents);

        var firstContent = contents[0];
        Assert.Equal("system://status", firstContent.GetProperty("uri").GetString());
        Assert.Equal("application/json", firstContent.GetProperty("mimeType").GetString());
        
        Assert.True(firstContent.TryGetProperty("text", out var text));
        var textValue = text.GetString();
        Assert.NotNull(textValue);
        Assert.NotEmpty(textValue);

        // Verify it's valid JSON
        using var statusDoc = JsonDocument.Parse(textValue!);
        var statusData = statusDoc.RootElement;

        Assert.True(statusData.TryGetProperty("uptime", out _));
        Assert.True(statusData.TryGetProperty("memoryUsed", out _));
        Assert.True(statusData.TryGetProperty("timestamp", out _));
    }

    [Fact]
    public async Task ResourcesRead_DatabaseResource_ReturnsStructuredData()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/read",
            id = "res-read-3",
            @params = new
            {
                uri = "db://users/example"
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("contents", out var contentsElement));

        var contents = contentsElement.EnumerateArray().ToList();
        Assert.Single(contents);

        var firstContent = contents[0];
        Assert.Equal("db://users/example", firstContent.GetProperty("uri").GetString());
        Assert.Equal("application/json", firstContent.GetProperty("mimeType").GetString());
        
        Assert.True(firstContent.TryGetProperty("text", out var text));
        var textValue = text.GetString();
        Assert.NotNull(textValue);
        Assert.NotEmpty(textValue);
        
        // Verify it's valid JSON with expected structure
        using var userDoc = JsonDocument.Parse(textValue!);
        var userData = userDoc.RootElement;
                
        Assert.True(userData.TryGetProperty("id", out var id));
        Assert.Equal("example", id.GetString());
        Assert.True(userData.TryGetProperty("name", out _));
        Assert.True(userData.TryGetProperty("email", out _));
    }

    [Fact]
    public async Task ResourcesRead_InvalidUri_ReturnsError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/read",
            id = "res-read-error",
            @params = new
            {
                uri = "file://nonexistent/resource"
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert - Should get an error response
        Assert.True(response.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32()); // Resource not found
    }

    [Fact]
    public async Task ResourcesRead_MissingUriParameter_ReturnsError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/read",
            id = "res-read-missing",
            @params = new { }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert - Should get an error response
        Assert.True(response.TryGetProperty("error", out var error));
        Assert.Equal(-32602, error.GetProperty("code").GetInt32()); // Invalid params
    }
}
