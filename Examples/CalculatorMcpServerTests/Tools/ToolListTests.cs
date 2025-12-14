namespace CalculatorMcpServerTests.Tools;

using CalculatorMcpServerTests.Fixture;
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
}
