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
// Add Tool service to the container.
builder.AddToolsService();

var app = builder.Build();

// stdio mode
if (isStdioMode)
{
    var logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClientTestMcpServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// Regular HTTP/WebSocket mode
// Enable WebSockets (must be before mapping)
app.UseWebSockets();

// MCP 2025-11-25 Streamable HTTP
app.UseProtocolVersionValidation();  // Protocol version validation
app.MapStreamableHttpEndpoint("/mcp");  // Unified endpoint (POST + GET + DELETE)
app.MapWsRpcEndpoint("/ws");     // WebSocket

app.Run();
