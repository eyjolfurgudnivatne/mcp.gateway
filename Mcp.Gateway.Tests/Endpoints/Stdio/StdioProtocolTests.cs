namespace Mcp.Gateway.Tests.Endpoints.Stdio;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;

[Collection("ServerCollection")]
public class StdioProtocolTests(McpGatewayFixture fixture)
{
    [Fact]
    public async Task Stdio_Initialize_ReturnsProtocolVersion()
    {
        // Arrange - simulate stdio JSON-RPC request
        var request = @"{""jsonrpc"":""2.0"",""method"":""initialize"",""id"":""init-1""}";
        
        // Act - parse and invoke via stdio method
        using var doc = JsonDocument.Parse(request);
        using var scope = fixture.Factory.Services.CreateScope();
        var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
        var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, fixture.CancellationToken);
        
        // Assert
        Assert.NotNull(response);
        var msg = response as JsonRpcMessage;
        Assert.NotNull(msg);
        Assert.NotNull(msg.Result);
        
        var resultJson = JsonSerializer.Serialize(msg.Result, JsonOptions.Default);
        var resultDoc = JsonDocument.Parse(resultJson);
        
        Assert.Equal("2025-11-25", resultDoc.RootElement.GetProperty("protocolVersion").GetString());  // Updated to MCP 2025-11-25 (v1.6.5)
        Assert.True(resultDoc.RootElement.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Stdio_ToolsList_ReturnsAllTools()
    {
        // Arrange
        var request = @"{""jsonrpc"":""2.0"",""method"":""tools/list"",""id"":""list-1""}";
        
        // Act
        using var doc = JsonDocument.Parse(request);
        using var scope = fixture.Factory.Services.CreateScope();
        var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
        var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, fixture.CancellationToken);
        
        // Assert
        Assert.NotNull(response);
        var msg = response as JsonRpcMessage;
        Assert.NotNull(msg);
        Assert.NotNull(msg.Result);
        
        var resultJson = JsonSerializer.Serialize(msg.Result, JsonOptions.Default);
        var resultDoc = JsonDocument.Parse(resultJson);
        
        Assert.True(resultDoc.RootElement.TryGetProperty("tools", out var toolsElement));
        var tools = toolsElement.EnumerateArray().ToList();
        Assert.True(tools.Count >= 6, $"Expected at least 6 tools, got {tools.Count}");
        
        // Verify ping tool is present
        var pingTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "system_ping");
        Assert.True(pingTool.ValueKind != JsonValueKind.Undefined, "Ping tool not found");
    }

    [Fact]
    public async Task Stdio_ToolsCall_ExecutesTool()
    {
        // Arrange
        var request = @"{""jsonrpc"":""2.0"",""method"":""tools/call"",""id"":""call-1"",""params"":{""name"":""system_ping"",""arguments"":{}}}";
        
        // Act
        using var doc = JsonDocument.Parse(request);
        using var scope = fixture.Factory.Services.CreateScope();
        var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
        var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, fixture.CancellationToken);
        
        // Assert
        Assert.NotNull(response);
        var msg = response as JsonRpcMessage;
        Assert.NotNull(msg);
        Assert.NotNull(msg.Result);
        
        var resultJson = JsonSerializer.Serialize(msg.Result, JsonOptions.Default);
        var resultDoc = JsonDocument.Parse(resultJson);
        
        // MCP format: { content: [{ type: "text", text: "..." }] }
        Assert.True(resultDoc.RootElement.TryGetProperty("content", out var content));
        var contentArray = content.EnumerateArray().ToList();
        Assert.Single(contentArray);
        
        var firstContent = contentArray[0];
        Assert.Equal("text", firstContent.GetProperty("type").GetString());
        
        var text = firstContent.GetProperty("text").GetString();
        var toolResult = JsonDocument.Parse(text!);
        
        // Verify ping response
        Assert.True(toolResult.RootElement.TryGetProperty("message", out var message));
        Assert.Equal("Pong", message.GetString());
    }

    [Fact]
    public async Task Stdio_StreamingTool_ReturnsError()
    {
        // Arrange - try to call a streaming tool over stdio
        var request = @"{""jsonrpc"":""2.0"",""method"":""tools/call"",""id"":""call-2"",""params"":{""name"":""system_binary_streams_in"",""arguments"":{}}}";
        
        // Act
        using var doc = JsonDocument.Parse(request);
        using var scope = fixture.Factory.Services.CreateScope();
        var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
        var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, fixture.CancellationToken);
        
        // Assert - should get error (streaming not supported over stdio)
        Assert.NotNull(response);
        var msg = response as JsonRpcMessage;
        Assert.NotNull(msg);
        Assert.NotNull(msg.Error);
        Assert.Equal(-32601, msg.Error.Code);
        Assert.Contains("streaming", msg.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stdio_DirectToolCall_Works()
    {
        // Arrange - direct tool invocation (not via tools/call)
        var request = @"{""jsonrpc"":""2.0"",""method"":""system_echo"",""id"":""echo-1"",""params"":{""message"":""Hello stdio!""}}";
        
        // Act
        using var doc = JsonDocument.Parse(request);
        using var scope = fixture.Factory.Services.CreateScope();
        var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
        var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, fixture.CancellationToken);
        
        // Assert
        Assert.NotNull(response);
        var msg = response as JsonRpcMessage;
        Assert.NotNull(msg);
        Assert.NotNull(msg.Result);
        
        var resultJson = JsonSerializer.Serialize(msg.Result, JsonOptions.Default);
        var resultDoc = JsonDocument.Parse(resultJson);
        
        Assert.True(resultDoc.RootElement.TryGetProperty("message", out var message));
        Assert.Equal("Hello stdio!", message.GetString());
    }

    [Fact]
    public async Task Stdio_Notification_ReturnsNull()
    {
        // Arrange - notification (no id field)
        var request = @"{""jsonrpc"":""2.0"",""method"":""system_notification"",""params"":{""message"":""Test""}}";
        
        // Act
        using var doc = JsonDocument.Parse(request);
        using var scope = fixture.Factory.Services.CreateScope();
        var toolInvoker = scope.ServiceProvider.GetRequiredService<ToolInvoker>();
        var response = await toolInvoker.InvokeSingleStdioAsync(doc.RootElement, fixture.CancellationToken);
        
        // Assert - notifications return null (no response)
        Assert.Null(response);
    }
}
