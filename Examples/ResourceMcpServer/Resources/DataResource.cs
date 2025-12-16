using Mcp.Gateway.Tools;
using System.Text.Json;

namespace ResourceMcpServer.Resources;

/// <summary>
/// Example data resource that returns mock database data
/// </summary>
public class DataResource
{
    [McpResource("db://users/example",
        Name = "Example User",
        Description = "Example user profile data",
        MimeType = "application/json")]
    public JsonRpcMessage ExampleUser(JsonRpcMessage request)
    {
        // Simulate database query result
        var user = new
        {
            id = "example",
            name = "John Doe",
            email = "john.doe@example.com",
            role = "Administrator",
            createdAt = DateTime.UtcNow.AddDays(-365),
            lastLogin = DateTime.UtcNow.AddHours(-2),
            preferences = new
            {
                theme = "dark",
                language = "en",
                notifications = true
            }
        };

        var json = JsonSerializer.Serialize(user, JsonOptions.Default);

        var content = new ResourceContent(
            Uri: "db://users/example",
            MimeType: "application/json",
            Text: json
        );

        return ToolResponse.Success(request.Id, content);
    }

    [McpResource("db://stats/summary",
        Name = "Statistics Summary",
        Description = "Application usage statistics",
        MimeType = "application/json")]
    public JsonRpcMessage StatsSummary(JsonRpcMessage request)
    {
        // Simulate statistics data
        var stats = new
        {
            totalUsers = 1234,
            activeUsers = 567,
            totalRequests = 98765,
            averageResponseTime = 125.5, // ms
            uptime = 0.9987, // 99.87%
            lastUpdated = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(stats, JsonOptions.Default);

        var content = new ResourceContent(
            Uri: "db://stats/summary",
            MimeType: "application/json",
            Text: json
        );

        return ToolResponse.Success(request.Id, content);
    }
}
