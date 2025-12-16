using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Detect stdio mode
var isStdioMode = args.Contains("--stdio");

if (isStdioMode)
{
    builder.Logging.ClearProviders();
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Register ToolService + ToolInvoker + Mock tools
builder.AddToolsService();
builder.Services.AddSingleton<PaginationMcpServer.Tools.MockTools>();
builder.Services.AddSingleton<PaginationMcpServer.Prompts.MockPrompts>();
builder.Services.AddSingleton<PaginationMcpServer.Resources.MockResources>();

var app = builder.Build();

// stdio mode for GitHub Copilot
if (isStdioMode)
{
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PaginationMcpServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// WebSockets for streaming
app.UseWebSockets();

// MCP endpoints
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");
app.MapSseRpcEndpoint("/sse");

app.Run();
