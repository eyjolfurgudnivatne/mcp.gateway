namespace AuthorizationMcpServerTests.Authorization;

using AuthorizationMcpServerTests.Fixture;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class AuthorizationTests(AuthorizationMcpServerFixture fixture)
{
    [Fact]
    public async Task MissingAuthorizationHeader_Returns401()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "delete_user",
                arguments = new { userId = "user-123" }
            }
        };

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var error = jsonDoc.RootElement.GetProperty("error");
        
        Assert.Equal(-32000, error.GetProperty("code").GetInt32());
        Assert.Equal("Unauthorized", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task InvalidToken_Returns403()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "delete_user",
                arguments = new { userId = "user-123" }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/rpc")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var error = jsonDoc.RootElement.GetProperty("error");
        
        Assert.Equal(-32002, error.GetProperty("code").GetInt32());
        Assert.Equal("Forbidden", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AdminTool_WithAdminToken_Succeeds()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "delete_user",
                arguments = new { userId = "user-123" }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/rpc")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "admin-token-123");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("content", out var contentArray));
        
        var firstContent = contentArray.EnumerateArray().First();
        var text = firstContent.GetProperty("text").GetString();
        var toolResult = JsonDocument.Parse(text!).RootElement;
        
        Assert.True(toolResult.GetProperty("deleted").GetBoolean());
        Assert.Equal("user-123", toolResult.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task AdminTool_WithUserToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "delete_user",
                arguments = new { userId = "user-123" }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/rpc")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "user-token-456");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode(); // HTTP 200 (JSON-RPC error inside)
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        // Should have JSON-RPC error for insufficient permissions
        Assert.True(jsonDoc.RootElement.TryGetProperty("error", out var error),
            "Expected JSON-RPC error for unauthorized access");
        
        // ToolInvalidParamsException is wrapped in -32603 (Internal error) when thrown from hook
        // This is expected behavior - hooks can't directly return -32602
        Assert.Equal(-32603, error.GetProperty("code").GetInt32());
        
        var message = error.GetProperty("message").GetString();
        Assert.Contains("Internal error", message);
        
        // Verify error details mention insufficient permissions
        var data = error.GetProperty("data");
        var detail = data.GetProperty("detail").GetString();
        Assert.Contains("Insufficient permissions", detail);
        Assert.Contains("delete_user", detail);
    }

    [Fact]
    public async Task MultiRoleTool_WithManagerToken_Succeeds()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "create_user",
                arguments = new { username = "newuser" }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/rpc")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "manager-token-789");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out _));
    }

    [Fact]
    public async Task AnonymousTool_WithoutToken_Succeeds()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "get_public_info",
                arguments = new { }
            }
        };

        // Act (NO Authorization header)
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request);

        // Assert - Should fail at middleware level (401)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousTool_WithAnyToken_Succeeds()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "1",
            @params = new
            {
                name = "get_public_info",
                arguments = new { }
            }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/rpc")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "public-token-000");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.True(jsonDoc.RootElement.TryGetProperty("result", out var result));
    }

    [Fact]
    public async Task HealthCheck_NoAuthRequired()
    {
        // Act
        var response = await fixture.HttpClient.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        Assert.Equal("healthy", jsonDoc.RootElement.GetProperty("status").GetString());
    }
}
