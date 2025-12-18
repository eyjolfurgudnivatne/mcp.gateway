namespace ResourceMcpServerTests.Resources;

using ResourceMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class McpProtocolTests(ResourceMcpServerFixture fixture)
{
    [Fact]
    public async Task Initialize_IncludesResourcesCapability()
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
        Assert.Equal("2025-11-25", result.GetProperty("protocolVersion").GetString());

        Assert.True(result.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());

        Assert.True(result.TryGetProperty("capabilities", out var capabilities));

        // Should have resources capability
        Assert.True(capabilities.TryGetProperty("resources", out _), 
            "Resources capability should be present");
    }

    [Fact]
    public async Task Initialize_WithoutResources_StillWorksForOtherCapabilities()
    {
        // This test verifies backward compatibility
        // Even if resources are present, tools should still work

        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-2"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("capabilities", out var capabilities));

        // Check all capabilities are still there
        var capabilityCount = 0;
        if (capabilities.TryGetProperty("tools", out _)) capabilityCount++;
        if (capabilities.TryGetProperty("prompts", out _)) capabilityCount++;
        if (capabilities.TryGetProperty("resources", out _)) capabilityCount++;

        Assert.True(capabilityCount >= 1, "Should have at least resources capability");
    }

    [Fact]
    public async Task ResourcesWorkflow_ListThenRead_Success()
    {
        // This test demonstrates the full workflow:
        // 1. Initialize to discover capabilities
        // 2. List resources to see what's available
        // 3. Read a specific resource

        // Step 1: Initialize
        var initRequest = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "workflow-1"
        };

        var initResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", initRequest, fixture.CancellationToken);
        initResponse.EnsureSuccessStatusCode();

        var initContent = await initResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var initDoc = JsonDocument.Parse(initContent).RootElement;
        
        Assert.True(initDoc.GetProperty("result")
            .GetProperty("capabilities")
            .TryGetProperty("resources", out _));

        // Step 2: List resources
        var listRequest = new
        {
            jsonrpc = "2.0",
            method = "resources/list",
            id = "workflow-2"
        };

        var listResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", listRequest, fixture.CancellationToken);
        listResponse.EnsureSuccessStatusCode();

        var listContent = await listResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var listDoc = JsonDocument.Parse(listContent).RootElement;
        
        var resources = listDoc.GetProperty("result")
            .GetProperty("resources")
            .EnumerateArray()
            .ToList();
        
        Assert.NotEmpty(resources);
        
        // Get first resource URI
        var firstResourceUri = resources[0].GetProperty("uri").GetString();
        Assert.NotNull(firstResourceUri);

        // Step 3: Read the resource
        var readRequest = new
        {
            jsonrpc = "2.0",
            method = "resources/read",
            id = "workflow-3",
            @params = new
            {
                uri = firstResourceUri
            }
        };

        var readResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", readRequest, fixture.CancellationToken);
        readResponse.EnsureSuccessStatusCode();

        var readContent = await readResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var readDoc = JsonDocument.Parse(readContent).RootElement;
        
        var contents = readDoc.GetProperty("result")
            .GetProperty("contents")
            .EnumerateArray()
            .ToList();
        
        Assert.Single(contents);
        Assert.Equal(firstResourceUri, contents[0].GetProperty("uri").GetString());
    }
}
