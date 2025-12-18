namespace Mcp.Gateway.Tests.Endpoints.Sse;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class SseProtocolTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task Initialize_OverSse_ReturnsProtocolVersion()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-sse-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Assert SSE content type
        Assert.Equal("text/event-stream; charset=utf-8", httpResponse.Content.Headers.ContentType?.ToString());

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        
        // Parse SSE format: "data: {json}\n\n"
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLine = lines.FirstOrDefault(l => l.StartsWith("data: "));
        
        Assert.NotNull(dataLine);
        var json = dataLine["data: ".Length..];
        var response = JsonDocument.Parse(json).RootElement;

        // Verify response
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.Equal("2025-11-25", result.GetProperty("protocolVersion").GetString());
        Assert.True(result.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());
    }

    [Fact]
    public async Task ToolsList_OverSse_ReturnsAllTools()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-sse-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLine = lines.FirstOrDefault(l => l.StartsWith("data: "));
        
        Assert.NotNull(dataLine);
        var json = dataLine["data: ".Length..];
        var response = JsonDocument.Parse(json).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var toolsElement));
        
        var tools = toolsElement.EnumerateArray().ToList();
        Assert.True(tools.Count >= 6, $"Expected at least 6 tools, got {tools.Count}");

        // Verify ping tool exists
        var pingTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "system_ping");
        Assert.True(pingTool.ValueKind != JsonValueKind.Undefined, "Ping tool not found");
    }

    [Fact]
    public async Task ToolsCall_OverSse_ReturnsMcpFormattedResponse()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "call-sse-1",
            @params = new
            {
                name = "system_ping",
                arguments = new { }
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var dataLine = lines.FirstOrDefault(l => l.StartsWith("data: "));
        
        Assert.NotNull(dataLine);
        var json = dataLine["data: ".Length..];
        var response = JsonDocument.Parse(json).RootElement;

        // Assert MCP format
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("content", out var contentArray));
        
        var firstContent = contentArray.EnumerateArray().First();
        Assert.Equal("text", firstContent.GetProperty("type").GetString());
        Assert.True(firstContent.TryGetProperty("text", out var text));
        
        var toolResult = JsonDocument.Parse(text.GetString()!).RootElement;
        Assert.True(toolResult.TryGetProperty("message", out var message));
        Assert.Equal("Pong", message.GetString());
    }

    [Fact]
    public async Task SseResponse_IncludesDoneEvent()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_ping",
            id = "ping-sse-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read full SSE stream
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);

        // Assert - should contain "event: done"
        Assert.Contains("event: done", content);
    }

    [Fact]
    public async Task SseResponse_HasCorrectHeaders()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "system_ping",
            id = "headers-sse-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/sse", request, fixture.CancellationToken);

        // Assert headers
        Assert.Equal("text/event-stream; charset=utf-8", httpResponse.Content.Headers.ContentType?.ToString());
        
        // Check for SSE-specific headers
        Assert.True(httpResponse.Headers.CacheControl?.NoCache ?? false, "Should have Cache-Control: no-cache");
        
        if (httpResponse.Headers.TryGetValues("Connection", out var connectionValues))
        {
            Assert.Contains("keep-alive", connectionValues, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task SseBatchRequest_ReturnsMultipleEvents()
    {
        // Arrange - batch request with multiple tools
        var batchRequest = new object[]
        {
            new
            {
                jsonrpc = "2.0",
                method = "system_ping",
                id = "batch-1"
            },
            new
            {
                jsonrpc = "2.0",
                method = "system_echo",
                id = "batch-2",
                @params = new { message = "test" }
            }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(batchRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var httpResponse = await fixture.HttpClient.PostAsync("/sse", content, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var responseContent = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var dataLines = responseContent.Split('\n')
            .Where(l => l.StartsWith("data: "))
            .ToList();

        // Assert - should have at least 2 data events (one per request)
        Assert.True(dataLines.Count >= 2, $"Expected at least 2 data events, got {dataLines.Count}");
    }

    [Fact]
    public async Task SseErrorResponse_ReturnsJsonRpcError()
    {
        // Arrange - invalid JSON
        var invalidJson = "{ this is not valid json }";
        var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

        // Act
        var httpResponse = await fixture.HttpClient.PostAsync("/sse", content, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        // Read SSE stream
        var responseContent = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var dataLine = responseContent.Split('\n')
            .FirstOrDefault(l => l.StartsWith("data: "));

        Assert.NotNull(dataLine);
        var json = dataLine["data: ".Length..];
        var response = JsonDocument.Parse(json).RootElement;

        // Assert - should be JSON-RPC error
        Assert.True(response.TryGetProperty("error", out var error));
        Assert.Equal(-32700, error.GetProperty("code").GetInt32()); // Parse error
    }
}
