namespace PaginationMcpServerTests.Pagination;

using PaginationMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class ToolsPaginationTests(PaginationMcpServerFixture fixture)
{
    [Fact]
    public async Task ToolsList_NoCursor_ReturnsFirst100Tools()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-1"
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
        Assert.Equal(100, tools.Count); // Default page size

        // Verify first tool is mock_tool_001
        Assert.Equal("mock_tool_001", tools.First().GetProperty("name").GetString());

        // Verify last tool in page is mock_tool_100
        Assert.Equal("mock_tool_100", tools.Last().GetProperty("name").GetString());

        // Should have nextCursor for remaining 20 tools
        Assert.True(result.TryGetProperty("nextCursor", out var nextCursor));
        Assert.False(string.IsNullOrEmpty(nextCursor.GetString()));
    }

    [Fact]
    public async Task ToolsList_WithCursor_ReturnsRemainingTools()
    {
        // Arrange - First request to get cursor
        var request1 = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-1"
        };

        var response1 = await fixture.HttpClient.PostAsJsonAsync("/rpc", request1, fixture.CancellationToken);
        var content1 = await response1.Content.ReadAsStringAsync(fixture.CancellationToken);
        var doc1 = JsonDocument.Parse(content1).RootElement;
        var cursor = doc1.GetProperty("result").GetProperty("nextCursor").GetString();

        // Act - Second request with cursor
        var request2 = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-2",
            @params = new { cursor }
        };

        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request2, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var toolsElement));

        var tools = toolsElement.EnumerateArray().ToList();
        Assert.Equal(20, tools.Count); // Remaining tools

        // Verify first tool in second page is mock_tool_101
        Assert.Equal("mock_tool_101", tools.First().GetProperty("name").GetString());

        // Verify last tool is mock_tool_120
        Assert.Equal("mock_tool_120", tools.Last().GetProperty("name").GetString());

        // Should NOT have nextCursor (last page)
        Assert.False(result.TryGetProperty("nextCursor", out _));
    }

    [Fact]
    public async Task ToolsList_WithCustomPageSize_ReturnCorrectCount()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-1",
            @params = new { pageSize = 10 }
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
        Assert.Equal(10, tools.Count); // Custom page size

        // Verify first tool is mock_tool_001
        Assert.Equal("mock_tool_001", tools.First().GetProperty("name").GetString());

        // Verify last tool in page is mock_tool_010
        Assert.Equal("mock_tool_010", tools.Last().GetProperty("name").GetString());

        // Should have nextCursor for remaining 110 tools
        Assert.True(result.TryGetProperty("nextCursor", out var nextCursor));
        Assert.False(string.IsNullOrEmpty(nextCursor.GetString()));
    }

    [Fact]
    public async Task ToolsList_InvalidCursor_ReturnsFirstPage()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-1",
            @params = new { cursor = "invalid_cursor_12345" }
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert - Should return first page (graceful fallback)
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var toolsElement));

        var tools = toolsElement.EnumerateArray().ToList();
        Assert.True(tools.Count > 0); // Should return some tools

        // Verify first tool is mock_tool_001 (start from beginning)
        Assert.Equal("mock_tool_001", tools.First().GetProperty("name").GetString());
    }

    [Fact]
    public async Task ToolsList_PageSizeLargerThanTotal_ReturnsAllToolsWithoutCursor()
    {
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "tools/list",
            id = "test-1",
            @params = new { pageSize = 200 } // Larger than 120 total tools
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
        Assert.Equal(120, tools.Count); // All tools

        // Should NOT have nextCursor (all tools returned)
        Assert.False(result.TryGetProperty("nextCursor", out _));
    }
}
