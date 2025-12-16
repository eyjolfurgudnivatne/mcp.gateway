namespace NotificationMcpServerTests.Notifications;

using NotificationMcpServerTests.Fixture;
using System.Net.Http.Json;
using System.Text.Json;

[Collection("ServerCollection")]
public class NotificationCapabilitiesTests(NotificationMcpServerFixture fixture)
{
    [Fact]
    public async Task Initialize_IncludesNotificationCapabilities()
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

        // Assert - Response structure
        Assert.True(response.TryGetProperty("result", out var result));
        Assert.Equal("2025-06-18", result.GetProperty("protocolVersion").GetString());

        // Should have serverInfo
        Assert.True(result.TryGetProperty("serverInfo", out var serverInfo));
        Assert.Equal("mcp-gateway", serverInfo.GetProperty("name").GetString());

        // Should have capabilities
        Assert.True(result.TryGetProperty("capabilities", out var capabilities));

        // Should have tools capability
        Assert.True(capabilities.TryGetProperty("tools", out _));

        // Should have notifications capability (v1.6.0+)
        Assert.True(capabilities.TryGetProperty("notifications", out var notifications));

        // Notifications should include tools (at minimum)
        Assert.True(notifications.TryGetProperty("tools", out _));
        
        // Note: NotificationMcpServer only has tools registered.
        // Therefore, prompts and resources notifications should NOT be present.
        Assert.False(notifications.TryGetProperty("prompts", out _), 
            "prompts notification should NOT be present when no prompts are registered");
        Assert.False(notifications.TryGetProperty("resources", out _), 
            "resources notification should NOT be present when no resources are registered");
    }

    [Fact]
    public async Task ToolsList_ReturnsStaticTools()
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
        Assert.True(tools.Count >= 3, "Expected at least 3 static tools (ping, echo, get_time)");

        // Verify ping tool exists
        var pingTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "ping");
        Assert.True(pingTool.ValueKind != JsonValueKind.Undefined, "ping tool not found");

        // Verify echo tool exists
        var echoTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "echo");
        Assert.True(echoTool.ValueKind != JsonValueKind.Undefined, "echo tool not found");

        // Verify get_time tool exists
        var getTimeTool = tools.FirstOrDefault(t => t.GetProperty("name").GetString() == "get_time");
        Assert.True(getTimeTool.ValueKind != JsonValueKind.Undefined, "get_time tool not found");
    }

    [Fact]
    public async Task TriggerNotification_ToolsChanged_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await fixture.HttpClient.PostAsync("/api/notify/tools", null, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var json = JsonDocument.Parse(content).RootElement;

        Assert.True(json.TryGetProperty("message", out var message));
        Assert.Equal("tools/changed notification sent", message.GetString());
    }

    [Fact]
    public async Task TriggerNotification_PromptsChanged_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await fixture.HttpClient.PostAsync("/api/notify/prompts", null, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var json = JsonDocument.Parse(content).RootElement;

        Assert.True(json.TryGetProperty("message", out var message));
        Assert.Equal("prompts/changed notification sent", message.GetString());
    }

    [Fact]
    public async Task TriggerNotification_ResourcesUpdated_ReturnsSuccess()
    {
        // Arrange & Act
        var response = await fixture.HttpClient.PostAsync("/api/notify/resources", null, fixture.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(fixture.CancellationToken);
        var json = JsonDocument.Parse(content).RootElement;

        Assert.True(json.TryGetProperty("message", out var message));
        Assert.Contains("resources/updated notification sent", message.GetString());
    }

    [Fact]
    public async Task NotificationCapabilities_OnlyIncludeRegisteredTypes()
    {
        // This test verifies that notification capabilities are filtered
        // based on what's actually registered in the server.
        
        // Arrange
        var request = new
        {
            jsonrpc = "2.0",
            method = "initialize",
            id = "init-capabilities-1"
        };

        // Act
        var httpResponse = await fixture.HttpClient.PostAsJsonAsync("/rpc", request, fixture.CancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var content = await httpResponse.Content.ReadAsStringAsync(fixture.CancellationToken);
        var response = JsonDocument.Parse(content).RootElement;

        // Assert
        var capabilities = response.GetProperty("result").GetProperty("capabilities");
        var notifications = capabilities.GetProperty("notifications");

        // Count notification types
        int notificationCount = 0;
        if (notifications.TryGetProperty("tools", out _)) notificationCount++;
        if (notifications.TryGetProperty("prompts", out _)) notificationCount++;
        if (notifications.TryGetProperty("resources", out _)) notificationCount++;

        // Should only have 1 notification type (tools)
        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public async Task NotificationService_CanSendNotificationsForUnregisteredTypes()
    {
        // This test verifies that the notification infrastructure can send
        // notifications even for types that aren't registered (prompts/resources).
        // This is useful for dynamic registration scenarios.

        // Act - Try to send prompts/changed notification (even though no prompts registered)
        var promptsResponse = await fixture.HttpClient.PostAsync("/api/notify/prompts", null, fixture.CancellationToken);
        
        // Assert - Should succeed (infrastructure allows it)
        promptsResponse.EnsureSuccessStatusCode();

        // Act - Try to send resources/updated notification (even though no resources registered)
        var resourcesResponse = await fixture.HttpClient.PostAsync("/api/notify/resources", null, fixture.CancellationToken);
        
        // Assert - Should succeed (infrastructure allows it)
        resourcesResponse.EnsureSuccessStatusCode();
    }
}
