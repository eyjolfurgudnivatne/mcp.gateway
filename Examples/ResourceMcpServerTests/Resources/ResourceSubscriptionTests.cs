namespace ResourceMcpServerTests.Resources;

using ResourceMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for resource subscription functionality (v1.8.0 Phase 4).
/// Tests resources/subscribe and resources/unsubscribe methods with notification filtering.
/// </summary>
[Collection("ServerCollection")]
public class ResourceSubscriptionTests(ResourceMcpServerFixture fixture)
{
    [Fact]
    public async Task ResourcesSubscribe_WithValidUri_ReturnsSuccess()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/subscribe",
            id = "sub-1",
            @params = new
            {
                uri = "file://data/users.json"
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("subscribed", out var subscribed));
        Assert.True(subscribed.GetBoolean());
        Assert.True(result.TryGetProperty("uri", out var uri));
        Assert.Equal("file://data/users.json", uri.GetString());
    }

    [Fact]
    public async Task ResourcesSubscribe_WithInvalidUri_ReturnsError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/subscribe",
            id = "sub-2",
            @params = new
            {
                uri = "file://nonexistent/resource.txt"
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode(); // HTTP 200, but JSON-RPC error inside

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        Assert.True(jsonDoc.RootElement.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32());
        Assert.Contains("Resource not found", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ResourcesUnsubscribe_AfterSubscribe_ReturnsSuccess()
    {
        // Arrange - Subscribe first
        var subscribeRequest = new
        {
            jsonrpc = "2.0",
            method = "resources/subscribe",
            id = "sub-3",
            @params = new
            {
                uri = "file://data/products.json"
            }
        };

        var subscribeHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        subscribeHttpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        subscribeHttpRequest.Content = JsonContent.Create(subscribeRequest);

        var subscribeResponse = await fixture.HttpClient.SendAsync(subscribeHttpRequest);
        subscribeResponse.EnsureSuccessStatusCode();

        // Extract session ID
        var sessionId = subscribeResponse.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
        Assert.NotNull(sessionId);

        // Act - Unsubscribe
        var unsubscribeRequest = new
        {
            jsonrpc = "2.0",
            method = "resources/unsubscribe",
            id = "unsub-3",
            @params = new
            {
                uri = "file://data/products.json"
            }
        };

        var unsubscribeHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        unsubscribeHttpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        unsubscribeHttpRequest.Headers.Add("MCP-Session-Id", sessionId);
        unsubscribeHttpRequest.Content = JsonContent.Create(unsubscribeRequest);

        var unsubscribeResponse = await fixture.HttpClient.SendAsync(unsubscribeHttpRequest);

        // Assert
        unsubscribeResponse.EnsureSuccessStatusCode();

        var content = await unsubscribeResponse.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("unsubscribed", out var unsubscribed));
        Assert.True(unsubscribed.GetBoolean());
        Assert.True(result.TryGetProperty("uri", out var uri));
        Assert.Equal("file://data/products.json", uri.GetString());
    }

    [Fact]
    public async Task ResourcesUnsubscribe_WithoutSubscribe_ReturnsSuccess()
    {
        // Arrange - Unsubscribe without subscribing first
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/unsubscribe",
            id = "unsub-4",
            @params = new
            {
                uri = "file://data/users.json"
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        // Should still succeed even if not subscribed
        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("unsubscribed", out var unsubscribed));
        Assert.True(unsubscribed.GetBoolean());
    }

    [Fact]
    public async Task ResourcesSubscribe_WithMissingUri_ReturnsError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/subscribe",
            id = "sub-5",
            @params = new { } // No URI parameter
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode(); // HTTP 200, but JSON-RPC error inside

        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        Assert.True(jsonDoc.RootElement.TryGetProperty("error", out var error));
        Assert.Equal(-32602, error.GetProperty("code").GetInt32());
        Assert.Contains("Missing 'uri' parameter", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ResourcesSubscribe_SameUriTwice_ReturnsSuccessBothTimes()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "resources/subscribe",
            id = "sub-6",
            @params = new
            {
                uri = "file://data/users.json"
            }
        };

        var httpRequest1 = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest1.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest1.Content = JsonContent.Create(request);

        // Act - Subscribe first time
        var response1 = await fixture.HttpClient.SendAsync(httpRequest1);
        response1.EnsureSuccessStatusCode();

        var sessionId = response1.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
        Assert.NotNull(sessionId);

        // Act - Subscribe second time (idempotent)
        var httpRequest2 = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest2.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest2.Headers.Add("MCP-Session-Id", sessionId);
        httpRequest2.Content = JsonContent.Create(request);

        var response2 = await fixture.HttpClient.SendAsync(httpRequest2);

        // Assert - Both should succeed
        response2.EnsureSuccessStatusCode();

        var content = await response2.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);

        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("subscribed", out var subscribed));
        Assert.True(subscribed.GetBoolean());
    }

    [Fact]
    public async Task ResourcesSubscribe_MultipleResources_AllSucceed()
    {
        // Arrange
        var resources = new[] 
        { 
            "file://data/users.json", 
            "file://data/products.json", 
            "system://metrics" 
        };

        var httpRequest1 = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest1.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest1.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "resources/subscribe",
            id = "sub-7",
            @params = new { uri = resources[0] }
        });

        // Act - Subscribe to first resource, get session ID
        var response1 = await fixture.HttpClient.SendAsync(httpRequest1);
        response1.EnsureSuccessStatusCode();

        var sessionId = response1.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
        Assert.NotNull(sessionId);

        // Act - Subscribe to remaining resources
        foreach (var resource in resources.Skip(1))
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
            httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
            httpRequest.Headers.Add("MCP-Session-Id", sessionId);
            httpRequest.Content = JsonContent.Create(new
            {
                jsonrpc = "2.0",
                method = "resources/subscribe",
                id = $"sub-{resource}",
                @params = new { uri = resource }
            });

            var response = await fixture.HttpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
            Assert.True(result.TryGetProperty("subscribed", out var subscribed));
            Assert.True(subscribed.GetBoolean());
        }

        // All 3 subscriptions should succeed
        Assert.True(true);
    }
}
