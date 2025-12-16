namespace PromptMcpServerTests.Prompts;

using Mcp.Gateway.Tools;
using PromptMcpServerTests.Fixture;
using System;
using System.Net.Http.Json;
using static PromptMcpServer.Prompts.SimplePrompt;

[Collection("ServerCollection")]
public class SimplePromptTests(PromptMcpServerFixture fixture)
{
    [Fact]
    public async Task GetPrompt_SantaReport_ReturnsPrompt()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "prompts/get",
            Guid.NewGuid().ToString("D"),
            new { name = "santa_report_prompt" });

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to get prompt");

        var result = content.GetResult<PromptResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Messages.First().Content);
        Assert.Equal("santa_report_prompt", result.Name);
    }

    [Fact]
    public async Task GetPrompt_SantaReport_JustPromptName_ReturnsPrompt()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "santa_report_prompt",
            Guid.NewGuid().ToString("D"));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to get prompt");

        var result = content.GetResult<PromptResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.Messages.First().Content);
        Assert.Equal("santa_report_prompt", result.Name);
    }

    [Fact]
    public async Task CallTool_LetterToSanta_ReturnsSuccess()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "tools/call",
            Guid.NewGuid().ToString("D"),
            new
            {
                name = "letter_to_santa",
                arguments = new
                {
                    name = "Good Kid",
                    behavior = "Good"
                }
            });

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to call tool");

        var result = content.GetToolsCallResult<LetterToSantaResponse>();
        Assert.NotNull(result);
        Assert.True(result.Sent);
    }

    [Fact]
    public async Task CallTool_LetterToSanta_JustToolName_ReturnsSuccess()
    {
        // Arrange
        var request = JsonRpcMessage.CreateRequest(
            "letter_to_santa",
            Guid.NewGuid().ToString("D"),
            new LetterToSantaRequest("Good Kid", BehaviorEnum.Good, null));

        // Act
        var response = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<JsonRpcMessage>(fixture.CancellationToken);
        Assert.NotNull(content);
        Assert.True(content.IsSuccessResponse, $"Failed to call tool");

        var result = content.GetResult<LetterToSantaResponse>();
        Assert.NotNull(result);
        Assert.True(result.Sent);
    }
}
