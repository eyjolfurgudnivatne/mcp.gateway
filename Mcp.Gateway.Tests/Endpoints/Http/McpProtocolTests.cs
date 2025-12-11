namespace Mcp.Gateway.Tests.Endpoints.Http;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class McpProtocolTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task Initialize_ReturnsProtocolVersion()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.Equal("2025-06-18", result.GetProperty("protocolVersion").GetString());  // Updated to match current protocol version
        Assert.True(result.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());
        Assert.True(result.TryGetProperty("capabilities", out var capabilities));
        Assert.True(capabilities.TryGetProperty("tools", out _));
    }

    [Fact]
    public async Task ToolsList_ReturnsAllTools()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var toolsElement));
        
        var tools = toolsElement.EnumerateArray().ToList();
        Assert.True(tools.Count >= 3, $"Expected at least 3 standard tools, got {tools.Count}"); // Ping, Echo, Notification (streaming excluded on HTTP)

        // Check that ping tool is present with correct schema
        var pingTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "system_ping");
        Assert.True(pingTool.ValueKind != JsonValueKind.Undefined, "Ping tool not found");
        Assert.True(pingTool.TryGetProperty("description", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));
        Assert.True(pingTool.TryGetProperty("inputSchema", out var schema));
        Assert.Equal("object", schema.GetProperty("type").GetString());
    }

    [Fact]
    public async Task ToolsList_ViaHttp_ExcludesBinaryStreamingTools()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-filtered"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var toolsElement));
        
        var tools = toolsElement.EnumerateArray().ToList();

        // Verify binary streaming tools are NOT included (HTTP transport)
        var hasBinaryStreamIn = tools.Any(t => t.GetProperty("name").GetString() == "system_binary_streams_in");
        Assert.False(hasBinaryStreamIn, "Binary streaming tools should NOT be visible via HTTP transport");

        var hasBinaryStreamOut = tools.Any(t => t.GetProperty("name").GetString() == "system_binary_streams_out");
        Assert.False(hasBinaryStreamOut, "Binary streaming tools should NOT be visible via HTTP transport");

        var hasBinaryStreamDuplex = tools.Any(t => t.GetProperty("name").GetString() == "system_binary_streams_duplex");
        Assert.False(hasBinaryStreamDuplex, "Binary streaming tools should NOT be visible via HTTP transport");

        // Verify standard tools ARE still included
        var hasPing = tools.Any(t => t.GetProperty("name").GetString() == "system_ping");
        Assert.True(hasPing, "Standard tools like system_ping should be visible via HTTP transport");
    }

    [Fact]
    public async Task ToolsCall_ReturnsMcpFormattedResponse()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "call-1",
            @params = new
            {
                name = "system_ping",
                arguments = new { }
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        
        // Debug output
        Console.WriteLine($"tools/call response: {content}");
        
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result), $"No 'result' in response: {content}");
        
        // MCP format: { content: [{ type: "text", text: "..." }] }
        Console.WriteLine($"Result: {result.GetRawText()}");
        Assert.True(result.TryGetProperty("content", out var contentArray), $"No 'content' in result: {result.GetRawText()}");
        var contents = contentArray.EnumerateArray().ToList();
        Assert.Single(contents);
        
        var firstContent = contents[0];
        Assert.Equal("text", firstContent.GetProperty("type").GetString());
        Assert.True(firstContent.TryGetProperty("text", out var text));
        
        // Parse the text to verify it contains the tool result
        var toolResult = JsonDocument.Parse(text.GetString()!).RootElement;
        
        // The text contains the direct result, not wrapped in another "result" property
        Assert.True(toolResult.TryGetProperty("message", out var message));
        Assert.Equal("Pong", message.GetString());
    }

    [Fact]
    public async Task ToolsCall_WithEchoTool_ReturnsEchoedData()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "call-2",
            @params = new
            {
                name = "system_echo",
                arguments = new
                {
                    message = "Hello MCP!"
                }
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("content", out var contentArray));
        
        var firstContent = contentArray.EnumerateArray().First();
        var text = firstContent.GetProperty("text").GetString();
        var toolResult = JsonDocument.Parse(text!).RootElement;
        
        // Echo returns the params directly (not wrapped in another "result" property)
        Assert.True(toolResult.TryGetProperty("message", out var echoedMessage));
        Assert.Equal("Hello MCP!", echoedMessage.GetString());
    }

    [Fact]
    public async Task ToolsCall_WithStreamingTool_ReturnsError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "call-3",
            @params = new
            {
                name = "system_binary_streams_in",
                arguments = new { }
            }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();
        
        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32());
        var errorMessage = error.GetProperty("message").GetString();
        Assert.Contains("streaming", errorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
