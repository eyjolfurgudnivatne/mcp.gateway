namespace CalculatorMcpServerTests.Tools;

using CalculatorMcpServerTests.Fixture;
using Mcp.Gateway.Tools;  // NEW: For JsonOptions
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class ToolListTests(CalculatorMcpServerFixture fixture)
{
    record AddNumbersToolInfo(string Name, string? Type, string? Description);

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
        Assert.True(tools.Count >= 2, $"Expected at least 2 tools, got {tools.Count}");

        // Check that add_numbers_typed tool is present with correct schema
        var addNumbersTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "add_numbers");
        Assert.True(addNumbersTool.ValueKind != JsonValueKind.Undefined, "Add Numbers (typed) tool not found");
        Assert.True(addNumbersTool.TryGetProperty("description", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));

        // NEW: Check that icon is present (v1.6.5)
        Assert.True(addNumbersTool.TryGetProperty("icons", out var icons));
        var iconsList = icons.EnumerateArray().ToList();
        Assert.Single(iconsList);
        var firstIcon = iconsList[0];
        Assert.Equal("https://example.com/icons/calculator.png", firstIcon.GetProperty("src").GetString());

        Assert.True(addNumbersTool.TryGetProperty("inputSchema", out var schema));
        Assert.Equal("object", schema.GetProperty("type").GetString());

        // check properties, property types and description
        Assert.True(schema.TryGetProperty("properties", out var properties));
        var propertyNames = new HashSet<AddNumbersToolInfo>();

        foreach (var prop in properties.EnumerateObject())
        {
            propertyNames.Add(new AddNumbersToolInfo(prop.Name, prop.Value.GetProperty("type").GetString(), prop.Value.GetProperty("description").GetString()));
        }
        Assert.Contains(new AddNumbersToolInfo("number1", "number", "First number to add"), propertyNames);
        Assert.Contains(new AddNumbersToolInfo("number2", "number", "Second number to add"), propertyNames);

        // check required fields
        Assert.True(schema.TryGetProperty("required", out var required));
        var requiredFields = new HashSet<string>(required.EnumerateArray().Select(r => r.GetString()!));
        Assert.Contains("number1", requiredFields);
        Assert.Contains("number2", requiredFields);
    }

    [Fact]
    public async Task ToolsList_CheckStringEnum()
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
        Assert.True(tools.Count >= 2, $"Expected at least 2 tools, got {tools.Count}");

        // Check that generate_secret tool is present with correct schema
        var generateSecretTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "generate_secret");
        Assert.True(generateSecretTool.ValueKind != JsonValueKind.Undefined, "Generate Secret tool not found");
        Assert.True(generateSecretTool.TryGetProperty("description", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));

        Assert.True(generateSecretTool.TryGetProperty("inputSchema", out var schema));
        Assert.Equal("object", schema.GetProperty("type").GetString());

        // check properties, property types and description
        Assert.True(schema.TryGetProperty("properties", out var properties));

        var prop = properties.GetProperty("format");

        Assert.Equal("string", prop.GetProperty("type").GetString());
        Assert.Equal("Format of the secret.", prop.GetProperty("description").GetString());

        var enumList = prop.GetProperty("enum").GetArrayLength() > 0
            ? prop.GetProperty("enum").EnumerateArray().Select(e => e.GetString()!).ToArray()
            : null;

        Assert.True(enumList != null, "Enum list is null");
        Assert.Equal(3, enumList.Length);
        Assert.Contains("Guid", enumList);
        Assert.Contains("Hex", enumList);
        Assert.Contains("Base64", enumList);

        // check required fields
        Assert.True(schema.TryGetProperty("required", out var required));
        var requiredFields = new HashSet<string>(required.EnumerateArray().Select(r => r.GetString()!));
        Assert.Contains("format", requiredFields);
    }

    [Fact]
    public async Task ToolsList_ToolWithIcon_IncludesIconsField()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-icons"
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

        // Check add_numbers tool has icons field
        var addNumbersTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "add_numbers");
        Assert.True(addNumbersTool.ValueKind != JsonValueKind.Undefined, "add_numbers tool not found");
        Assert.True(addNumbersTool.TryGetProperty("icons", out var icons), "icons field missing");
        
        var iconsList = icons.EnumerateArray().ToList();
        Assert.Single(iconsList);
        Assert.Equal("https://example.com/icons/calculator.png", iconsList[0].GetProperty("src").GetString());
    }

    [Fact]
    public async Task ToolsList_ToolWithOutputSchema_IncludesOutputSchemaField()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-output-schema"
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

        // Check add_numbers tool has outputSchema field
        var addNumbersTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "add_numbers");
        Assert.True(addNumbersTool.ValueKind != JsonValueKind.Undefined, "add_numbers tool not found");
        Assert.True(addNumbersTool.TryGetProperty("outputSchema", out var outputSchema), "outputSchema field missing");
        
        // Verify outputSchema structure
        Assert.Equal("object", outputSchema.GetProperty("type").GetString());
        Assert.True(outputSchema.TryGetProperty("properties", out var properties));
        Assert.True(properties.TryGetProperty("result", out var resultProp));
        Assert.Equal("number", resultProp.GetProperty("type").GetString());
    }

    [Fact]
    public async Task ToolsCall_WithStructuredContent_ReturnsBothContentAndStructured()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/call",
            id = "call-structured",
            @params = new
            {
                name = "add_numbers",
                arguments = new
                {
                    number1 = 5.0,
                    number2 = 3.0
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
        
        // Debug: Print the actual response
        System.Diagnostics.Debug.WriteLine($"Response: {JsonSerializer.Serialize(result, JsonOptions.Default)}");
        
        // Verify content array
        Assert.True(result.TryGetProperty("content", out var contentArray));
        var firstContent = contentArray.EnumerateArray().First();
        Assert.Equal("text", firstContent.GetProperty("type").GetString());
        Assert.Contains("Result: 8", firstContent.GetProperty("text").GetString());
        
        // Verify structuredContent (NEW: v1.6.5)
        Assert.True(result.TryGetProperty("structuredContent", out var structured), 
            $"structuredContent not found in response. Actual response: {JsonSerializer.Serialize(result, JsonOptions.Default)}");
        Assert.Equal(8.0, structured.GetProperty("result").GetDouble());
        Assert.Equal("addition", structured.GetProperty("operation").GetString());
    }

    [Fact]
    public async Task ToolsList_ToolWithoutIcon_OmitsIconsField()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "list-no-icons"
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

        // Check multiply_numbers tool does NOT have icons field
        var multiplyTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "multiply_numbers");
        Assert.True(multiplyTool.ValueKind != JsonValueKind.Undefined, "multiply_numbers tool not found");
        Assert.False(multiplyTool.TryGetProperty("icons", out _), "icons field should be omitted when not set");
    }
}
