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
        "ResourceMcpServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// Regular HTTP/WebSocket mode
// Enable WebSockets (must be before mapping)
app.UseWebSockets();

// HTTP endpoint (easy testing with curl/Postman)
app.MapHttpRpcEndpoint("/rpc");

// WebSocket endpoint (full-duplex, streaming support)
app.MapWsRpcEndpoint("/ws");

// SSE endpoint (remote MCP clients, e.g. Claude Desktop)
app.MapSseRpcEndpoint("/sse");

app.Run();

// Make Program accessible for testing
public partial class Program { }
