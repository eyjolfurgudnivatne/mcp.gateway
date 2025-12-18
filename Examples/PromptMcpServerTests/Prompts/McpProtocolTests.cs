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
        Assert.Equal("2025-11-25", result.GetProperty("protocolVersion").GetString());  // Updated to MCP 2025-11-25 (v1.6.5)

        Assert.True(result.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());

        Assert.True(result.TryGetProperty("capabilities", out var capabilities));

        Assert.True(capabilities.TryGetProperty("tools", out _));
        Assert.True(capabilities.TryGetProperty("prompts", out _));
    }

    [Fact]
    public async Task PromptsList_ReturnsAllPrompts()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "prompts/list",
            id = "prompts-list-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert - Response structure
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("prompts", out var promptsElement));

        var prompts = promptsElement.EnumerateArray().ToList();
        Assert.True(prompts.Count >= 1, $"Expected at least 1 prompt, got {prompts.Count}");

        // Find santa_report_prompt
        var santaPrompt = prompts.FirstOrDefault(p => 
            p.GetProperty("name").GetString() == "santa_report_prompt");
        Assert.True(santaPrompt.ValueKind != JsonValueKind.Undefined, 
            "santa_report_prompt not found in prompts/list");

        // Assert - Prompt metadata
        Assert.True(santaPrompt.TryGetProperty("name", out var name));
        Assert.Equal("santa_report_prompt", name.GetString());

        Assert.True(santaPrompt.TryGetProperty("description", out var description));
        Assert.Equal("Report to Santa Claus", description.GetString());

        // Assert - Arguments array (NOT inputSchema!)
        Assert.True(santaPrompt.TryGetProperty("arguments", out var argumentsElement));
        Assert.Equal(JsonValueKind.Array, argumentsElement.ValueKind); // Must be array!

        var arguments = argumentsElement.EnumerateArray().ToList();
        Assert.Equal(2, arguments.Count); // name and behavior

        // Assert - First argument (name)
        var nameArg = arguments.FirstOrDefault(a => 
            a.GetProperty("name").GetString() == "name");
        Assert.True(nameArg.ValueKind != JsonValueKind.Undefined, "name argument not found");
        Assert.True(nameArg.TryGetProperty("description", out var nameDesc));
        Assert.False(string.IsNullOrEmpty(nameDesc.GetString()));
        Assert.True(nameArg.TryGetProperty("required", out var nameRequired));
        Assert.True(nameRequired.GetBoolean());

        // Assert - Second argument (behavior)
        var behaviorArg = arguments.FirstOrDefault(a => 
            a.GetProperty("name").GetString() == "behavior");
        Assert.True(behaviorArg.ValueKind != JsonValueKind.Undefined, "behavior argument not found");
        Assert.True(behaviorArg.TryGetProperty("description", out var behaviorDesc));
        Assert.False(string.IsNullOrEmpty(behaviorDesc.GetString()));
        Assert.True(behaviorArg.TryGetProperty("required", out var behaviorRequired));
        Assert.True(behaviorRequired.GetBoolean());
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
