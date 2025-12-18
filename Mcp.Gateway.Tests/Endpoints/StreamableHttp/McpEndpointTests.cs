namespace Mcp.Gateway.Tests.Endpoints.StreamableHttp;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

/// <summary>
/// Integration tests for unified /mcp endpoint (v1.7.0)
/// Tests POST, GET, DELETE methods with session management
/// </summary>
[Collection("ServerCollection")]
public class McpEndpointTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task PostMcp_InitializeRequest_ReturnsImmediateResponse()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-1"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        // Should get a session ID in response header
        if (response.Headers.TryGetValues("MCP-Session-Id", out var sessionIds))
        {
            var sessionId = sessionIds.FirstOrDefault();
            Assert.NotNull(sessionId);
            Assert.NotEmpty(sessionId);
            Assert.Equal(32, sessionId.Length); // GUID in "N" format
        }
    }

    [Fact]
    public async Task PostMcp_ToolsListRequest_ReturnsTools()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-1"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var tools));
        Assert.True(tools.GetArrayLength() > 0);
    }

    [Fact]
    public async Task PostMcp_WithInvalidSessionId_Returns404()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-2"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Headers.Add("MCP-Session-Id", "invalid-session-id");
        httpRequest.Content = JsonContent.Create(request);

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest, fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMcp_WithInvalidJson_Returns400()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Content = new StringContent("invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest, fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMcp_WithMissingSessionId_Returns400()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Headers.Add("Accept", "text/event-stream");

        // Act
        var response = await fixture.HttpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMcp_WithInvalidSessionId_Returns404()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Headers.Add("MCP-Session-Id", "invalid-session-id");
        httpRequest.Headers.Add("Accept", "text/event-stream");

        // Act
        var response = await fixture.HttpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMcp_WithValidSession_OpensSseStream()
    {
        // Arrange - First create a session
        var initRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        initRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        initRequest.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-sse"
        });

        var initResponse = await fixture.HttpClient.SendAsync(initRequest, fixture.CancellationToken);
        initResponse.EnsureSuccessStatusCode();

        var sessionId = initResponse.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
        Assert.NotNull(sessionId);

        // Act - Open SSE stream
        var sseRequest = new HttpRequestMessage(HttpMethod.Get, "/mcp");
        sseRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        sseRequest.Headers.Add("MCP-Session-Id", sessionId);
        sseRequest.Headers.Add("Accept", "text/event-stream");

        var sseResponse = await fixture.HttpClient.SendAsync(
            sseRequest,
            HttpCompletionOption.ResponseHeadersRead,
            fixture.CancellationToken);

        // Assert
        sseResponse.EnsureSuccessStatusCode();
        Assert.Equal("text/event-stream", sseResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteMcp_WithMissingSessionId_Returns400()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest, fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMcp_WithInvalidSessionId_Returns404()
    {
        // Arrange
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, "/mcp");
        httpRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        httpRequest.Headers.Add("MCP-Session-Id", "invalid-session-id");

        // Act
        var response = await fixture.HttpClient.SendAsync(httpRequest, fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMcp_WithValidSession_Returns204()
    {
        // Arrange - First create a session
        var initRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        initRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        initRequest.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-delete"
        });

        var initResponse = await fixture.HttpClient.SendAsync(initRequest, fixture.CancellationToken);
        initResponse.EnsureSuccessStatusCode();

        var sessionId = initResponse.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
        Assert.NotNull(sessionId);

        // Act - Delete session
        var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, "/mcp");
        deleteRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        deleteRequest.Headers.Add("MCP-Session-Id", sessionId);

        var deleteResponse = await fixture.HttpClient.SendAsync(deleteRequest, fixture.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify session is actually deleted
        var verifyRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        verifyRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        verifyRequest.Headers.Add("MCP-Session-Id", sessionId);
        verifyRequest.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "verify"
        });

        var verifyResponse = await fixture.HttpClient.SendAsync(verifyRequest, fixture.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    [Fact]
    public async Task PostMcp_SessionPersistsAcrossRequests()
    {
        // Arrange - Create session with first request
        var initRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        initRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        initRequest.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-persist"
        });

        var initResponse = await fixture.HttpClient.SendAsync(initRequest, fixture.CancellationToken);
        var sessionId = initResponse.Headers.GetValues("MCP-Session-Id").FirstOrDefault();

        // Act - Make second request with same session ID
        var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        secondRequest.Headers.Add("MCP-Protocol-Version", "2025-11-25");
        secondRequest.Headers.Add("MCP-Session-Id", sessionId);
        secondRequest.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-persist"
        });

        var secondResponse = await fixture.HttpClient.SendAsync(secondRequest, fixture.CancellationToken);

        // Assert
        secondResponse.EnsureSuccessStatusCode();
        var returnedSessionId = secondResponse.Headers.GetValues("MCP-Session-Id").FirstOrDefault();
        Assert.Equal(sessionId, returnedSessionId);
    }
}
