namespace Mcp.Gateway.Tests;

using Mcp.Gateway.Tools;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Tests for ToolCapabilities filtering functionality.
/// Verifies that tools are correctly filtered based on transport capabilities.
/// </summary>
public class ToolCapabilitiesTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ToolService _toolService;

    public ToolCapabilitiesTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ToolCapabilitiesTestTools>();
        _serviceProvider = services.BuildServiceProvider();
        _toolService = new ToolService(_serviceProvider);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetToolsForTransport_Stdio_ExcludesStreamingTools()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "stdio");
        var tools = result.Items.ToList();

        // Assert
        Assert.NotEmpty(tools);
        
        // Should NOT contain binary streaming tools
        Assert.DoesNotContain(tools, t => 
            t.Name.Contains("binary", StringComparison.OrdinalIgnoreCase) &&
            t.Name.Contains("stream", StringComparison.OrdinalIgnoreCase));
        
        // Should contain standard tools
        Assert.Contains(tools, t => t.Name == "test_standard_tool");
    }

    [Fact]
    public void GetToolsForTransport_Http_ExcludesStreamingTools()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "http");
        var tools = result.Items.ToList();

        // Assert
        Assert.NotEmpty(tools);
        
        // Should NOT contain binary streaming tools
        Assert.DoesNotContain(tools, t => 
            t.Name.Contains("binary", StringComparison.OrdinalIgnoreCase) &&
            t.Name.Contains("stream", StringComparison.OrdinalIgnoreCase));
        
        // Should contain standard tools
        Assert.Contains(tools, t => t.Name == "test_standard_tool");
    }

    [Fact]
    public void GetToolsForTransport_WebSocket_IncludesAllTools()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "ws");
        var tools = result.Items.ToList();

        // Assert
        Assert.NotEmpty(tools);
        
        // Should contain binary streaming tools
        Assert.Contains(tools, t => t.Name == "test_binary_stream_tool");
        
        // Should contain standard tools
        Assert.Contains(tools, t => t.Name == "test_standard_tool");
        
        // Should contain text streaming tools (if any)
        Assert.Contains(tools, t => t.Name == "test_text_stream_tool");
    }

    [Fact]
    public void GetToolsForTransport_Sse_IncludesTextStreamingTools()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "sse");
        var tools = result.Items.ToList();

        // Assert
        Assert.NotEmpty(tools);
        
        // Should NOT contain binary streaming tools (WebSocket-only)
        Assert.DoesNotContain(tools, t => t.Name == "test_binary_stream_tool");
        
        // Should contain standard tools
        Assert.Contains(tools, t => t.Name == "test_standard_tool");
        
        // Should contain text streaming tools
        Assert.Contains(tools, t => t.Name == "test_text_stream_tool");
    }

    [Fact]
    public void ToolCapabilities_FlagsEnumWorks()
    {
        // Test that ToolCapabilities enum works as [Flags]
        var combined = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket;
        
        Assert.True((combined & ToolCapabilities.BinaryStreaming) != 0);
        Assert.True((combined & ToolCapabilities.RequiresWebSocket) != 0);
        Assert.False((combined & ToolCapabilities.TextStreaming) != 0);
    }

    [Fact]
    public void ToolDefinition_DefaultCapabilities_IsStandard()
    {
        // Arrange
        var tool = new ToolService.FunctionDefinition(
            Name: "test",
            Description: "Test tool",
            InputSchema: "{}"
        );

        // Assert
        Assert.Equal(ToolCapabilities.Standard, tool.Capabilities);
    }

    [Fact]
    public void McpToolAttribute_DefaultCapabilities_IsStandard()
    {
        // Arrange
        var attr = new McpToolAttribute("test_tool");

        // Assert
        Assert.Equal(ToolCapabilities.Standard, attr.Capabilities);
    }

    [Fact]
    public void GetAllToolDefinitions_IncludesCapabilities()
    {
        // Arrange & Act
        var tools = _toolService.GetAllFunctionDefinitions(ToolService.FunctionTypeEnum.Tool).ToList();

        // Assert
        Assert.NotEmpty(tools);
        
        // Find binary streaming tool
        var binaryTool = tools.FirstOrDefault(t => t.Name == "test_binary_stream_tool");
        Assert.NotNull(binaryTool);
        Assert.Equal(
            ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket,
            binaryTool.Capabilities);
        
        // Find standard tool
        var standardTool = tools.FirstOrDefault(t => t.Name == "test_standard_tool");
        Assert.NotNull(standardTool);
        Assert.Equal(ToolCapabilities.Standard, standardTool.Capabilities);
    }
}

/// <summary>
/// Test tools for capability filtering tests.
/// Includes tools with different capabilities.
/// </summary>
public class ToolCapabilitiesTestTools
{
    [McpTool("test_standard_tool",
        Description = "Standard tool (works on all transports)",
        InputSchema = @"{""type"":""object"",""properties"":{}}",
        Capabilities = ToolCapabilities.Standard)]
    public static Task<JsonRpcMessage> StandardTool(JsonRpcMessage request)
    {
        return Task.FromResult(ToolResponse.Success(request.Id, new { message = "standard" }));
    }

    [McpTool("test_binary_stream_tool",
        Description = "Binary streaming tool (WebSocket only)",
        InputSchema = @"{""type"":""object"",""properties"":{}}",
        Capabilities = ToolCapabilities.BinaryStreaming | ToolCapabilities.RequiresWebSocket)]
    public static async Task BinaryStreamTool(ToolConnector connector)
    {
        var meta = new StreamMessageMeta(
            Method: "result.data",
            Binary: true);
        
        using var handle = (ToolConnector.BinaryStreamHandle)connector.OpenWrite(meta);
        var data = System.Text.Encoding.UTF8.GetBytes("test");
        await handle.WriteAsync(data);
        await handle.CompleteAsync(new { bytes = data.Length });
    }

    [McpTool("test_text_stream_tool",
        Description = "Text streaming tool (WebSocket or SSE)",
        InputSchema = @"{""type"":""object"",""properties"":{}}",
        Capabilities = ToolCapabilities.TextStreaming)]
    public static async Task TextStreamTool(ToolConnector connector)
    {
        var meta = new StreamMessageMeta(
            Method: "result.text",
            Binary: false);
        
        var handle = (ToolConnector.TextStreamHandle)connector.OpenWrite(meta);
        await handle.WriteChunkAsync(new { message = "test" });
        await handle.CompleteAsync(new { chunks = 1 });
    }
}
