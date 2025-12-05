namespace Mcp.Gateway.Tests.Endpoints.Rpc;

using Mcp.Gateway.Tests.Fixtures.CollectionFixtures;
using Mcp.Gateway.Tools;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

[Collection("ServerCollection")]
public class CalculatorTests(McpGatewayFixture fixture)
{
    public sealed record NumbersRequest(
        [property: JsonPropertyName("number1")] double Number1,
        [property: JsonPropertyName("number2")] double Number2);

    public sealed record NumbersResponse(
        [property: JsonPropertyName("result")] double Result);

    private readonly string ToolPath = "add_numbers";

    [Fact]
    public async Task Add_Numbers_ReturnsNumber()
    {
        // Arrange
        var testData = new NumbersRequest(1, 2);
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            id = "test-add-numbers-1",
            @params = testData
        };

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("result", out var result), $"No 'result' in response: {content}");
        Assert.True(result.TryGetProperty("result", out var message));
        Assert.Equal(testData.Number1 + testData.Number2, message.GetDouble());
    }

    [Fact]
    public async Task Add_Numbers_ReturnsNumber_part2()
    {
        // Arrange
        var testData = new NumbersRequest(1, 2);

        var request = JsonRpcMessage.CreateRequest(
            Method: ToolPath,
            Id: "test-add-numbers-1",
            Params: testData);

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        Assert.True(root.TryGetProperty("result", out var result), $"No 'result' in response: {content}");
        Assert.True(result.TryGetProperty("result", out var message));
        Assert.Equal(testData.Number1 + testData.Number2, message.GetDouble());
    }

    [Fact]
    public async Task Add_Numbers_WithMissingParams_ReturnsInvalidParamsError()
    {
        // Arrange - send empty params object (missing required number1 and number2)
        var request = new
        {
            jsonrpc = "2.0",
            method = ToolPath,
            id = "test-add-numbers-invalid-1",
            @params = new { } // Empty params - missing required fields
        };

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode(); // JSON-RPC errors still return 200 OK

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var jsonDoc = JsonDocument.Parse(content);
        var root = jsonDoc.RootElement;

        // Verify this is an error response
        Assert.True(root.TryGetProperty("error", out var error), $"No 'error' in response: {content}");
        
        // Verify error code is -32602 (Invalid params)
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(-32602, code.GetInt32());
        
        // Verify error message
        Assert.True(error.TryGetProperty("message", out var message));
        Assert.Equal("Invalid params", message.GetString());
        
        // Verify error data contains detail
        Assert.True(error.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("detail", out var detail));
        Assert.Contains("number1", detail.GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("number2", detail.GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Add_Numbers_WithNullParams_ReturnsInvalidParamsError()
    {
        // Arrange - send request without params property at all
        var requestJson = """
        {
            "jsonrpc": "2.0",
            "method": "add_numbers",
            "id": "test-add-numbers-invalid-2"
        }
        """;
        
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await fixture.HttpClient.PostAsync("/rpc", content, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        // Verify this is an error response with -32602
        Assert.True(root.TryGetProperty("error", out var error), $"No 'error' in response: {responseContent}");
        Assert.True(error.TryGetProperty("code", out var code));
        Assert.Equal(-32602, code.GetInt32());
    }
}
