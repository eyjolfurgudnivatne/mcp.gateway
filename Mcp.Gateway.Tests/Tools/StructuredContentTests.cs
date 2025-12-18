namespace Mcp.Gateway.Tests.Tools;

using Mcp.Gateway.Tools;
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for structured content and output schema support in MCP 2025-11-25.
/// Verifies that tools can define outputSchema and return structuredContent.
/// </summary>
public class StructuredContentTests
{
    [Fact]
    public void OutputSchema_InAttribute_StoresCorrectly()
    {
        // Arrange
        var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""number""}}}";
        var attr = new McpToolAttribute("test_tool")
        {
            OutputSchema = schema
        };

        // Assert
        Assert.Equal(schema, attr.OutputSchema);
    }

    [Fact]
    public void FunctionDefinition_WithOutputSchema_CreatesCorrectly()
    {
        // Arrange
        var schema = @"{""type"":""object"",""properties"":{""result"":{""type"":""number""}}}";
        var def = new ToolService.FunctionDefinition(
            Name: "test_tool",
            Description: "Test tool",
            InputSchema: "{}",
            Arguments: null,
            Capabilities: ToolCapabilities.Standard,
            Icon: null,
            OutputSchema: schema);

        // Assert
        Assert.Equal(schema, def.OutputSchema);
    }

    [Fact]
    public void ToolResponse_SuccessWithStructured_CreatesBothContentAndStructured()
    {
        // Arrange
        var textContent = "Result: 42";
        var structuredContent = new { result = 42, operation = "test" };

        // Act
        var response = ToolResponse.SuccessWithStructured("test-id", textContent, structuredContent);

        // Assert
        Assert.NotNull(response.Result);
        var json = JsonSerializer.Serialize(response.Result, JsonOptions.Default);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify content array
        Assert.True(root.TryGetProperty("content", out var content));
        Assert.Equal(JsonValueKind.Array, content.ValueKind);
        var firstContent = content.EnumerateArray().First();
        Assert.Equal("text", firstContent.GetProperty("type").GetString());
        Assert.Equal("Result: 42", firstContent.GetProperty("text").GetString());

        // Verify structuredContent
        Assert.True(root.TryGetProperty("structuredContent", out var structured));
        Assert.Equal(42, structured.GetProperty("result").GetInt32());
        Assert.Equal("test", structured.GetProperty("operation").GetString());
    }

    [Fact]
    public void ToolResponse_SuccessWithStructured_CustomContent_UsesProvidedArray()
    {
        // Arrange
        var content = new[]
        {
            new { type = "text", text = "Line 1" },
            new { type = "text", text = "Line 2" }
        };
        var structuredContent = new { result = 42 };

        // Act
        var response = ToolResponse.SuccessWithStructured("test-id", content, structuredContent);

        // Assert
        Assert.NotNull(response.Result);
        var json = JsonSerializer.Serialize(response.Result, JsonOptions.Default);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify content array has 2 items
        Assert.True(root.TryGetProperty("content", out var contentArray));
        Assert.Equal(2, contentArray.GetArrayLength());

        // Verify structuredContent
        Assert.True(root.TryGetProperty("structuredContent", out var structured));
        Assert.Equal(42, structured.GetProperty("result").GetInt32());
    }
}
