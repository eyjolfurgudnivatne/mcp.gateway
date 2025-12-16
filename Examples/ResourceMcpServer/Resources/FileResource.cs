using Mcp.Gateway.Tools;
using System.Text.Json;

namespace ResourceMcpServer.Resources;

/// <summary>
/// Example file-based resource that returns application logs
/// </summary>
public class FileResource
{
    [McpResource("file://logs/app.log",
        Name = "Application Logs",
        Description = "Recent application log entries (last 100 lines)",
        MimeType = "text/plain")]
    public JsonRpcMessage AppLogs(JsonRpcMessage request)
    {
        // Simulate log content (in real app, read from actual log file)
        var logEntries = new[]
        {
            $"[{DateTime.UtcNow.AddMinutes(-5):yyyy-MM-dd HH:mm:ss}] INFO: Application started",
            $"[{DateTime.UtcNow.AddMinutes(-4):yyyy-MM-dd HH:mm:ss}] DEBUG: Initializing MCP Gateway",
            $"[{DateTime.UtcNow.AddMinutes(-3):yyyy-MM-dd HH:mm:ss}] INFO: Resources registered: 3",
            $"[{DateTime.UtcNow.AddMinutes(-2):yyyy-MM-dd HH:mm:ss}] DEBUG: HTTP endpoint listening on /rpc",
            $"[{DateTime.UtcNow.AddMinutes(-1):yyyy-MM-dd HH:mm:ss}] INFO: Ready to accept connections"
        };

        var logContent = string.Join("\n", logEntries);

        // Return as ResourceContent
        var content = new ResourceContent(
            Uri: "file://logs/app.log",
            MimeType: "text/plain",
            Text: logContent
        );

        return ToolResponse.Success(request.Id, content);
    }

    [McpResource("file://config/settings.json",
        Name = "Application Settings",
        Description = "Current application configuration",
        MimeType = "application/json")]
    public JsonRpcMessage AppSettings(JsonRpcMessage request)
    {
        // Simulate configuration (in real app, read from appsettings.json)
        var settings = new
        {
            environment = "Development",
            logging = new
            {
                logLevel = new
                {
                    Default = "Information",
                    Microsoft = "Warning"
                }
            },
            features = new
            {
                resourcesEnabled = true,
                toolsEnabled = true,
                promptsEnabled = true
            }
        };

        var json = JsonSerializer.Serialize(settings, JsonOptions.Default);

        var content = new ResourceContent(
            Uri: "file://config/settings.json",
            MimeType: "application/json",
            Text: json
        );

        return ToolResponse.Success(request.Id, content);
    }
}
