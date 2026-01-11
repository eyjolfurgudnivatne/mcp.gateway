namespace ClientTestMcpServer.EpResources;

using Mcp.Gateway.Tools;
using System.Diagnostics;
using System.Text.Json;

public class SystemResource
{
    [McpResource("system://status",
        Name = "System Status",
        Description = "Current system health metrics and status",
        MimeType = "application/json")]
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

        var result = new ReadResourceResult
        {
            Meta = new Dictionary<string, object> {
                { "tools.gateway.mcp/status", "Hello World" }
            },
            Contents = [
                new ResourceContent(
                    Uri: "system://status",
                    MimeType: "application/json",
                    Text: json)]
        };

        return ToolResponse.Success(request.Id, result);
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
