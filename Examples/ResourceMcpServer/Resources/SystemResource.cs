using Mcp.Gateway.Tools;
using System.Diagnostics;
using System.Text.Json;

namespace ResourceMcpServer.Resources;

/// <summary>
/// Example system resource that returns system status and metrics
/// </summary>
public class SystemResource
{
    [McpResource("system://status",
        Name = "System Status",
        Description = "Current system health metrics and status",
        MimeType = "application/json")]
    [McpIcon("icon.png", "image/png", Sizes = new[] { "16x16", "32x32", "48x48", "any" })]
    [McpIcon("icon-light.png", "image/png", McpIconTheme.Light)]
    [McpIcon("icon-dark.png", "image/png", McpIconTheme.Dark)]
    public JsonRpcMessage SystemStatus(JsonRpcMessage request)
    {
        var process = Process.GetCurrentProcess();
        
        var status = new
        {
            uptime = Environment.TickCount64,
            memoryUsed = GC.GetTotalMemory(false) / (1024 * 1024), // MB
            memoryTotal = process.WorkingSet64 / (1024 * 1024), // MB
            threadCount = ThreadPool.ThreadCount,
            processId = process.Id,
            machineName = Environment.MachineName,
            osVersion = Environment.OSVersion.ToString(),
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(status, JsonOptions.Default);

        var content = new ResourceContent(
            Uri: "system://status",
            MimeType: "application/json",
            Text: json
        );

        return ToolResponse.Success(request.Id, content);
    }

    [McpResource("system://environment",
        Name = "Environment Variables",
        Description = "System environment information (filtered)",
        MimeType = "application/json")]
    public JsonRpcMessage EnvironmentInfo(JsonRpcMessage request)
    {
        // Only expose safe environment variables (not secrets!)
        var safeVars = new
        {
            path = Environment.GetEnvironmentVariable("PATH")?.Split(';').Take(3).ToArray(),
            temp = Environment.GetEnvironmentVariable("TEMP"),
            os = Environment.OSVersion.Platform.ToString(),
            framework = Environment.Version.ToString(),
            is64Bit = Environment.Is64BitOperatingSystem,
            processorCount = Environment.ProcessorCount
        };

        var json = JsonSerializer.Serialize(safeVars, JsonOptions.Default);

        var content = new ResourceContent(
            Uri: "system://environment",
            MimeType: "application/json",
            Text: json
        );

        return ToolResponse.Success(request.Id, content);
    }
}
