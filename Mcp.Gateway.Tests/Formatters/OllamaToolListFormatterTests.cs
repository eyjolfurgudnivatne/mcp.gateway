namespace Mcp.Gateway.Tests.Formatters;

using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Formatters;
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for OllamaToolListFormatter - validates conversion from MCP to Ollama format
/// </summary>
public class OllamaToolListFormatterTests
{
    [Fact]
    public void FormatToolList_BasicTool_ReturnsOllamaFormat()
    {
        // Arrange
        var formatter = new OllamaToolListFormatter();
        var tools = new List<ToolService.ToolDefinition>
        {
            new ToolService.ToolDefinition(
                Name: "add_numbers",
                Description: "Adds two numbers",
                InputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "a": { "type": "integer", "description": "First number" },
                        "b": { "type": "integer", "description": "Second number" }
                    },
                    "required": ["a", "b"]
                }
                """
            )
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        Assert.True(parsed.RootElement.TryGetProperty("tools", out var toolsArray));
        Assert.Equal(JsonValueKind.Array, toolsArray.ValueKind);
        Assert.Equal(1, toolsArray.GetArrayLength());

        var tool = toolsArray[0];
        Assert.Equal("function", tool.GetProperty("type").GetString());
        
        var function = tool.GetProperty("function");
        Assert.Equal("add_numbers", function.GetProperty("name").GetString());
        Assert.Equal("Adds two numbers", function.GetProperty("description").GetString());
        
        var parameters = function.GetProperty("parameters");
        Assert.Equal("object", parameters.GetProperty("type").GetString());
        
        var properties = parameters.GetProperty("properties");
        Assert.True(properties.TryGetProperty("a", out _));
        Assert.True(properties.TryGetProperty("b", out _));
        
        var required = parameters.GetProperty("required");
        Assert.Equal(2, required.GetArrayLength());
    }

    [Fact]
    public void FormatToolList_NoRequiredFields_ReturnsEmptyRequiredArray()
    {
        // Arrange
        var formatter = new OllamaToolListFormatter();
        var tools = new List<ToolService.ToolDefinition>
        {
            new ToolService.ToolDefinition(
                Name: "greet",
                Description: "Greets someone",
                InputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string" }
                    }
                }
                """
            )
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        var tool = parsed.RootElement.GetProperty("tools")[0];
        var required = tool.GetProperty("function").GetProperty("parameters").GetProperty("required");
        Assert.Equal(0, required.GetArrayLength());
    }

    [Fact]
    public void FormatToolList_ComplexSchema_PreservesStructure()
    {
        // Arrange
        var formatter = new OllamaToolListFormatter();
        var tools = new List<ToolService.ToolDefinition>
        {
            new ToolService.ToolDefinition(
                Name: "complex_tool",
                Description: "Complex tool with nested properties",
                InputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "user": {
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" },
                                "age": { "type": "integer" }
                            }
                        },
                        "tags": {
                            "type": "array",
                            "items": { "type": "string" }
                        }
                    },
                    "required": ["user"]
                }
                """
            )
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        var tool = parsed.RootElement.GetProperty("tools")[0];
        var parameters = tool.GetProperty("function").GetProperty("parameters");
        var properties = parameters.GetProperty("properties");
        
        Assert.True(properties.TryGetProperty("user", out var user));
        Assert.Equal("object", user.GetProperty("type").GetString());
        
        Assert.True(properties.TryGetProperty("tags", out var tags));
        Assert.Equal("array", tags.GetProperty("type").GetString());
    }

    [Fact]
    public void FormatToolList_MultipleTools_ReturnsAllFormatted()
    {
        // Arrange
        var formatter = new OllamaToolListFormatter();
        var tools = new List<ToolService.ToolDefinition>
        {
            new ToolService.ToolDefinition("tool1", "First tool", """{"type":"object","properties":{}}"""),
            new ToolService.ToolDefinition("tool2", "Second tool", """{"type":"object","properties":{}}"""),
            new ToolService.ToolDefinition("tool3", "Third tool", """{"type":"object","properties":{}}""")
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        var toolsArray = parsed.RootElement.GetProperty("tools");
        Assert.Equal(3, toolsArray.GetArrayLength());
        
        Assert.Equal("tool1", toolsArray[0].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("tool2", toolsArray[1].GetProperty("function").GetProperty("name").GetString());
        Assert.Equal("tool3", toolsArray[2].GetProperty("function").GetProperty("name").GetString());
    }

    [Fact]
    public void FormatName_ReturnsCorrectValue()
    {
        // Arrange
        var formatter = new OllamaToolListFormatter();

        // Act & Assert
        Assert.Equal("ollama", formatter.FormatName);
    }
}
