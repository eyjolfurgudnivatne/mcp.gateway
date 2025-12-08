namespace Mcp.Gateway.Tests.Formatters;

using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using Mcp.Gateway.Server;
using Mcp.Gateway.Tools;

/// <summary>
/// Integration tests for formatted tool lists via JSON-RPC methods.
/// Tests tools/list/{format} endpoints without requiring external dependencies (e.g., Ollama).
/// </summary>
public class FormattedToolsListIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public FormattedToolsListIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ToolsList_Ollama_ReturnsOllamaFormat()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list/ollama",
            id = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/rpc", request);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2.0", json.GetProperty("jsonrpc").GetString());
        Assert.Equal(1, json.GetProperty("id").GetInt32());
        
        var result = json.GetProperty("result");
        Assert.True(result.TryGetProperty("tools", out var tools));
        Assert.Equal(JsonValueKind.Array, tools.ValueKind);
        
        // Verify Ollama format structure
        if (tools.GetArrayLength() > 0)
        {
            var firstTool = tools[0];
            Assert.Equal("function", firstTool.GetProperty("type").GetString());
            
            var function = firstTool.GetProperty("function");
            Assert.True(function.TryGetProperty("name", out _));
            Assert.True(function.TryGetProperty("description", out _));
            Assert.True(function.TryGetProperty("parameters", out var parameters));
            
            Assert.Equal("object", parameters.GetProperty("type").GetString());
            Assert.True(parameters.TryGetProperty("properties", out _));
            Assert.True(parameters.TryGetProperty("required", out _));
        }
    }

    [Fact]
    public async Task ToolsList_MicrosoftAI_ReturnsMicrosoftAIFormat()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list/microsoft-ai",
            id = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/rpc", request);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2.0", json.GetProperty("jsonrpc").GetString());
        Assert.Equal(2, json.GetProperty("id").GetInt32());
        
        var result = json.GetProperty("result");
        Assert.True(result.TryGetProperty("tools", out var tools));
        Assert.Equal(JsonValueKind.Array, tools.ValueKind);
        
        // Verify Microsoft.Extensions.AI format structure
        if (tools.GetArrayLength() > 0)
        {
            var firstTool = tools[0];
            Assert.True(firstTool.TryGetProperty("name", out _));
            Assert.True(firstTool.TryGetProperty("description", out _));
            Assert.True(firstTool.TryGetProperty("parameters", out var parameters));
            
            // Parameters should be an object with properties
            Assert.Equal(JsonValueKind.Object, parameters.ValueKind);
        }
    }

    [Fact]
    public async Task ToolsList_MCP_ReturnsStandardMCPFormat()
    {
        // Arrange - test explicit mcp format
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list/mcp",
            id = 3
        };

        // Act
        var response = await _client.PostAsJsonAsync("/rpc", request);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json.GetProperty("result");
        Assert.True(result.TryGetProperty("tools", out var tools));
        
        // Verify standard MCP format structure
        if (tools.GetArrayLength() > 0)
        {
            var firstTool = tools[0];
            Assert.True(firstTool.TryGetProperty("name", out _));
            Assert.True(firstTool.TryGetProperty("description", out _));
            Assert.True(firstTool.TryGetProperty("inputSchema", out var inputSchema));
            
            // inputSchema should be an object
            Assert.Equal(JsonValueKind.Object, inputSchema.ValueKind);
        }
    }

    [Fact]
    public async Task ToolsList_UnknownFormat_ReturnsError()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list/unknown-format",
            id = 4
        };

        // Act
        var response = await _client.PostAsJsonAsync("/rpc", request);

        // Assert
        response.EnsureSuccessStatusCode(); // Still 200 OK, but with JSON-RPC error
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("2.0", json.GetProperty("jsonrpc").GetString());
        Assert.Equal(4, json.GetProperty("id").GetInt32());
        
        // Should have error, not result
        Assert.True(json.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32());
        Assert.Equal("Unknown format", error.GetProperty("message").GetString());
        
        // Should include supported formats in error data
        var errorData = error.GetProperty("data");
        Assert.True(errorData.TryGetProperty("supportedFormats", out var formats));
        Assert.Equal(JsonValueKind.Array, formats.ValueKind);
    }

    [Fact]
    public async Task ToolsList_Standard_StillWorks()
    {
        // Arrange - verify standard tools/list still works
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/rpc", request);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json.GetProperty("result");
        Assert.True(result.TryGetProperty("tools", out var tools));
        Assert.Equal(JsonValueKind.Array, tools.ValueKind);
        
        // Should return standard MCP format
        if (tools.GetArrayLength() > 0)
        {
            var firstTool = tools[0];
            Assert.True(firstTool.TryGetProperty("name", out _));
            Assert.True(firstTool.TryGetProperty("description", out _));
            Assert.True(firstTool.TryGetProperty("inputSchema", out _));
        }
    }

    [Fact]
    public async Task ToolsList_TransportFiltering_AppliesCorrectly()
    {
        // Arrange - get tools for HTTP transport
        var ollamaRequest = new
        {
            jsonrpc = "2.0",
            method = "tools/list/ollama",
            id = 6
        };

        // Act
        var response = await _client.PostAsJsonAsync("/rpc", ollamaRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var result = json.GetProperty("result");
        var tools = result.GetProperty("tools");
        
        // Verify that binary streaming tools are filtered out for HTTP transport
        // (This assumes we have both standard and binary streaming tools registered)
        foreach (var tool in tools.EnumerateArray())
        {
            var toolName = tool.GetProperty("function").GetProperty("name").GetString();
            
            // Binary streaming tools should be filtered out for HTTP
            Assert.DoesNotContain("binary_streams", toolName ?? "");
        }
    }

    [Fact]
    public async Task ToolsList_Ollama_ReturnsConsistentToolCount()
    {
        // Arrange - call both standard and ollama format
        var standardRequest = new { jsonrpc = "2.0", method = "tools/list", id = 7 };
        var ollamaRequest = new { jsonrpc = "2.0", method = "tools/list/ollama", id = 8 };

        // Act
        var standardResponse = await _client.PostAsJsonAsync("/rpc", standardRequest);
        var ollamaResponse = await _client.PostAsJsonAsync("/rpc", ollamaRequest);

        // Assert - both should return same number of tools (just different format)
        var standardJson = await standardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var ollamaJson = await ollamaResponse.Content.ReadFromJsonAsync<JsonElement>();
        
        var standardTools = standardJson.GetProperty("result").GetProperty("tools");
        var ollamaTools = ollamaJson.GetProperty("result").GetProperty("tools");
        
        Assert.Equal(standardTools.GetArrayLength(), ollamaTools.GetArrayLength());
    }
}
