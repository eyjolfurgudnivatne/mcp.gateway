namespace Mcp.Gateway.Tests.Formatters;

using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Formatters;
using System.Text.Json;
using Xunit;

/// <summary>
/// Tests for MicrosoftAIToolListFormatter - validates conversion from MCP to Microsoft.Extensions.AI format
/// </summary>
public class MicrosoftAIToolListFormatterTests
{
    [Fact]
    public void FormatToolList_BasicTool_ReturnsMicrosoftAIFormat()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();
        var tools = new List<ToolService.FunctionDefinition>
        {
            new(
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
        Assert.Equal(1, toolsArray.GetArrayLength());

        var tool = toolsArray[0];
        Assert.Equal("add_numbers", tool.GetProperty("name").GetString());
        Assert.Equal("Adds two numbers", tool.GetProperty("description").GetString());
        
        var parameters = tool.GetProperty("parameters");
        Assert.True(parameters.TryGetProperty("a", out var paramA));
        Assert.Equal("integer", paramA.GetProperty("type").GetString());
        Assert.Equal("First number", paramA.GetProperty("description").GetString());
        Assert.True(paramA.GetProperty("required").GetBoolean());
        
        Assert.True(parameters.TryGetProperty("b", out var paramB));
        Assert.Equal("integer", paramB.GetProperty("type").GetString());
        Assert.True(paramB.GetProperty("required").GetBoolean());
    }

    [Fact]
    public void FormatToolList_NoRequiredFields_MarksAsNotRequired()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();
        var tools = new List<ToolService.FunctionDefinition>
        {
            new(
                Name: "greet",
                Description: "Greets someone",
                InputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string", "description": "Name to greet" }
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
        var parameters = tool.GetProperty("parameters");
        var nameParam = parameters.GetProperty("name");
        
        Assert.False(nameParam.GetProperty("required").GetBoolean());
    }

    [Fact]
    public void FormatToolList_MixedRequiredFields_CorrectlyMarks()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();
        var tools = new List<ToolService.FunctionDefinition>
        {
            new(
                Name: "create_user",
                Description: "Creates a user",
                InputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "name": { "type": "string", "description": "User name" },
                        "email": { "type": "string", "description": "User email" },
                        "age": { "type": "integer", "description": "User age" }
                    },
                    "required": ["name", "email"]
                }
                """
            )
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        var parameters = parsed.RootElement.GetProperty("tools")[0].GetProperty("parameters");
        
        Assert.True(parameters.GetProperty("name").GetProperty("required").GetBoolean());
        Assert.True(parameters.GetProperty("email").GetProperty("required").GetBoolean());
        Assert.False(parameters.GetProperty("age").GetProperty("required").GetBoolean());
    }

    [Fact]
    public void FormatToolList_NoDescription_HandlesGracefully()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();
        var tools = new List<ToolService.FunctionDefinition>
        {
            new(
                Name: "simple_tool",
                Description: "Simple tool",
                InputSchema: """
                {
                    "type": "object",
                    "properties": {
                        "value": { "type": "string" }
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
        var parameters = parsed.RootElement.GetProperty("tools")[0].GetProperty("parameters");
        var valueParam = parameters.GetProperty("value");
        
        // Description should be null if not provided
        var descriptionExists = valueParam.TryGetProperty("description", out var desc);
        if (descriptionExists)
        {
            Assert.Equal(JsonValueKind.Null, desc.ValueKind);
        }
    }

    [Fact]
    public void FormatToolList_MultipleTools_ReturnsAllFormatted()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();
        var tools = new List<ToolService.FunctionDefinition>
        {
            new("tool1", "First", """{"type":"object","properties":{}}"""),
            new("tool2", "Second", """{"type":"object","properties":{}}"""),
            new("tool3", "Third", """{"type":"object","properties":{}}""")
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        var toolsArray = parsed.RootElement.GetProperty("tools");
        Assert.Equal(3, toolsArray.GetArrayLength());
        
        Assert.Equal("tool1", toolsArray[0].GetProperty("name").GetString());
        Assert.Equal("tool2", toolsArray[1].GetProperty("name").GetString());
        Assert.Equal("tool3", toolsArray[2].GetProperty("name").GetString());
    }

    [Fact]
    public void FormatName_ReturnsCorrectValue()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();

        // Act & Assert
        Assert.Equal("microsoft-ai", formatter.FormatName);
    }

    [Fact]
    public void FormatToolList_EmptyProperties_ReturnsEmptyParametersObject()
    {
        // Arrange
        var formatter = new MicrosoftAIToolListFormatter();
        var tools = new List<ToolService.FunctionDefinition>
        {
            new(
                Name: "no_params",
                Description: "Tool with no parameters",
                InputSchema: """{"type":"object","properties":{}}"""
            )
        };

        // Act
        var result = formatter.FormatToolList(tools);
        var json = JsonSerializer.Serialize(result, JsonOptions.Default);
        var parsed = JsonDocument.Parse(json);

        // Assert
        var parameters = parsed.RootElement.GetProperty("tools")[0].GetProperty("parameters");
        Assert.Equal(JsonValueKind.Object, parameters.ValueKind);
        Assert.Empty(parameters.EnumerateObject());
    }
}
