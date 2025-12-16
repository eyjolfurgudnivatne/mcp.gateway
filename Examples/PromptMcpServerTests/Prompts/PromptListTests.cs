namespace PromptMcpServerTests.Prompts;

using Mcp.Gateway.Tools;
using Microsoft.Extensions.DependencyInjection;
using PromptMcpServer.Prompts;
using System;

public class PromptListTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ToolService _toolService;

    public PromptListTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SimplePrompt>();
        _serviceProvider = services.BuildServiceProvider();
        _toolService = new ToolService(_serviceProvider);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetPrompts_Websocket_IncludesAllPrompts()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Prompt, "ws");
        var prompts = result.Items.ToList();

        // Assert
        Assert.NotEmpty(prompts);

        // Should NOT contain letter_to_santa
        Assert.DoesNotContain(prompts, t => t.Name == "letter_to_santa");

        // Should contain santa_report_prompt
        Assert.Contains(prompts, t => t.Name == "santa_report_prompt");
    }

    [Fact]
    public void GetPrompts_Http_IncludesAllPrompts()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Prompt, "http");
        var prompts = result.Items.ToList();

        // Assert
        Assert.NotEmpty(prompts);

        // Should NOT contain letter_to_santa
        Assert.DoesNotContain(prompts, t => t.Name == "letter_to_santa");

        // Should contain santa_report_prompt
        Assert.Contains(prompts, t => t.Name == "santa_report_prompt");
    }

    [Fact]
    public void GetTools_Http_IncludesAllTools()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "http");
        var tools = result.Items.ToList();

        // Assert
        Assert.NotEmpty(tools);

        // Should contain letter_to_santa
        Assert.Contains(tools, t => t.Name == "letter_to_santa");

        // Should NOT contain santa_report_prompt
        Assert.DoesNotContain(tools, t => t.Name == "santa_report_prompt");
    }

    [Fact]
    public void GetTools_Websocket_IncludesAllTools()
    {
        // Arrange & Act
        var result = _toolService.GetFunctionsForTransport(ToolService.FunctionTypeEnum.Tool, "ws");
        var tools = result.Items.ToList();

        // Assert
        Assert.NotEmpty(tools);

        // Should contain letter_to_santa
        Assert.Contains(tools, t => t.Name == "letter_to_santa");

        // Should NOT contain santa_report_prompt
        Assert.DoesNotContain(tools, t => t.Name == "santa_report_prompt");
    }
}
