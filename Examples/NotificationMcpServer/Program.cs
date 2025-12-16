using Mcp.Gateway.Tools;
using Mcp.Gateway.Tools.Notifications;

var builder = WebApplication.CreateBuilder(args);

// Detect stdio mode
var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Register ToolService + ToolInvoker + NotificationService + Dynamic tools
builder.AddToolsService();
builder.Services.AddSingleton<NotificationMcpServer.Tools.DynamicToolRegistry>();

var app = builder.Build();

// stdio mode for GitHub Copilot
if (isStdioMode)
{
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NotificationMcpServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// WebSockets for streaming and notifications
app.UseWebSockets();

// MCP endpoints
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");

// API endpoint for triggering notifications (for demo purposes)
app.MapPost("/api/notify/tools", async (INotificationSender notificationSender) =>
{
    await notificationSender.SendNotificationAsync(NotificationMessage.ToolsChanged());
    return Results.Ok(new { message = "tools/changed notification sent" });
});

app.MapPost("/api/notify/prompts", async (INotificationSender notificationSender) =>
{
    await notificationSender.SendNotificationAsync(NotificationMessage.PromptsChanged());
    return Results.Ok(new { message = "prompts/changed notification sent" });
});

app.MapPost("/api/notify/resources", async (INotificationSender notificationSender, string? uri = null) =>
{
    await notificationSender.SendNotificationAsync(NotificationMessage.ResourcesUpdated(uri));
    return Results.Ok(new { message = $"resources/updated notification sent for {uri ?? "all"}" });
});

app.Run();

