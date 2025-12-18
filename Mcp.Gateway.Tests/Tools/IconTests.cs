namespace Mcp.Gateway.Tests.Tools;

using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Icons;  // NEW: For McpIconDefinition
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for icon support in MCP 2025-11-25.
/// Verifies that icons are correctly serialized in tools/list, prompts/list, and resources/list.
/// </summary>
public class IconTests
{
    [Fact]
    public void McpIconDefinition_SerializesCorrectly()
    {
        // Arrange
        var icon = new McpIconDefinition(
            Src: "https://example.com/icon.png",
            MimeType: "image/png",
            Sizes: new[] { "48x48", "64x64" });

        // Act
        var json = JsonSerializer.Serialize(icon, JsonOptions.Default);
        var deserialized = JsonSerializer.Deserialize<McpIconDefinition>(json, JsonOptions.Default);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("https://example.com/icon.png", deserialized.Src);
        Assert.Equal("image/png", deserialized.MimeType);
        Assert.Equal(2, deserialized.Sizes?.Length);
        Assert.Equal("48x48", deserialized.Sizes?[0]);
        Assert.Equal("64x64", deserialized.Sizes?[1]);
    }

    [Fact]
    public void McpIconDefinition_WithNullOptionalFields_SerializesCorrectly()
    {
        // Arrange
        var icon = new McpIconDefinition(
            Src: "https://example.com/icon.png");

        // Act
        var json = JsonSerializer.Serialize(icon, JsonOptions.Default);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert
        Assert.True(root.TryGetProperty("src", out var src));
        Assert.Equal("https://example.com/icon.png", src.GetString());
        
        // Optional fields should be null (not present or null)
        if (root.TryGetProperty("mimeType", out var mimeType))
        {
            Assert.Equal(JsonValueKind.Null, mimeType.ValueKind);
        }
        
        if (root.TryGetProperty("sizes", out var sizes))
        {
            Assert.Equal(JsonValueKind.Null, sizes.ValueKind);
        }
    }

    [Fact]
    public void ToolAttribute_WithIcon_StoresCorrectly()
    {
        // Arrange
        var attr = new McpToolAttribute("test_tool")
        {
            Icon = "https://example.com/icon.png"
        };

        // Assert
        Assert.Equal("https://example.com/icon.png", attr.Icon);
    }

    [Fact]
    public void PromptAttribute_WithIcon_StoresCorrectly()
    {
        // Arrange
        var attr = new McpPromptAttribute("test_prompt")
        {
            Icon = "https://example.com/icon.png"
        };

        // Assert
        Assert.Equal("https://example.com/icon.png", attr.Icon);
    }

    [Fact]
    public void ResourceAttribute_WithIcon_StoresCorrectly()
    {
        // Arrange
        var attr = new McpResourceAttribute("file://test")
        {
            Icon = "https://example.com/icon.png"
        };

        // Assert
        Assert.Equal("https://example.com/icon.png", attr.Icon);
    }

    [Fact]
    public void FunctionDefinition_WithIcon_CreatesCorrectly()
    {
        // Arrange
        var def = new ToolService.FunctionDefinition(
            Name: "test_tool",
            Description: "Test tool",
            InputSchema: "{}",
            Arguments: null,
            Capabilities: ToolCapabilities.Standard,
            Icon: "https://example.com/icon.png");

        // Assert
        Assert.Equal("https://example.com/icon.png", def.Icon);
    }

    [Fact]
    public void ResourceDefinition_WithIcon_CreatesCorrectly()
    {
        // Arrange
        var def = new ResourceDefinition(
            Uri: "file://test",
            Name: "Test",
            Description: "Test resource",
            MimeType: "text/plain",
            Icon: "https://example.com/icon.png");

        // Assert
        Assert.Equal("https://example.com/icon.png", def.Icon);
    }
}
