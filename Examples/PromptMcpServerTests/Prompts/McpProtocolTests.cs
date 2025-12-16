namespace PromptMcpServerTests.Prompts;

using PromptMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class McpProtocolTests(PromptMcpServerFixture fixture)
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
        Assert.Equal("2025-06-18", result.GetProperty("protocolVersion").GetString());

        Assert.True(result.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());

        Assert.True(result.TryGetProperty("capabilities", out var capabilities));

        Assert.True(capabilities.TryGetProperty("tools", out _));
        Assert.True(capabilities.TryGetProperty("prompts", out _));
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
        Assert.True(tools.Count == 1, $"Expected 1 standard tools, got {tools.Count}");

        // Check that LetterToSanta tool is present with correct schema
        var letterToSantaTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "letter_to_santa");
        Assert.True(letterToSantaTool.ValueKind != JsonValueKind.Undefined, "LetterToSanta tool not found");

        Assert.True(letterToSantaTool.TryGetProperty("description", out var desc));
        Assert.False(string.IsNullOrEmpty(desc.GetString()));

        Assert.True(letterToSantaTool.TryGetProperty("inputSchema", out var schema));
        Assert.Equal("object", schema.GetProperty("type").GetString());

        Assert.True(schema.TryGetProperty("properties", out var properties));

        Assert.True(properties.TryGetProperty("name", out var nameProp));
        Assert.Equal("string", nameProp.GetProperty("type").GetString());
        Assert.True(properties.TryGetProperty("behavior", out var behaviorProp));
        Assert.Equal("string", behaviorProp.GetProperty("type").GetString());
        Assert.True(properties.TryGetProperty("santaEmailAddress", out var santaEmailProp));
        Assert.Equal("string", santaEmailProp.GetProperty("type").GetString());

        Assert.True(behaviorProp.TryGetProperty("enum", out var enumProp));
        var enumValues = enumProp.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Good", enumValues);
        Assert.Contains("Naughty", enumValues);

        Assert.True(schema.TryGetProperty("required", out var requiredProp));
        var requiredValues = requiredProp.EnumerateArray().Select(r => r.GetString()).ToList();
        Assert.Contains("behavior", requiredValues);
        Assert.Contains("name", requiredValues);
        Assert.DoesNotContain("santaEmailAddress", requiredValues);
    }
}
