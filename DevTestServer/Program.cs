using DevTestServer.Endpoints;
using DevTestServer.MyServices;
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Detect stdio mode EARLY (before building)
var isStdioMode = args.Contains("--stdio");

// Setup logging path for stdio mode
string? logPath = null;
if (isStdioMode)
{
    // Clear default console logging (stdout must be JSON-only!)
    builder.Logging.ClearProviders();
    
    // Add Debug provider (won't pollute stdout/stderr)
    builder.Logging.AddDebug();
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    
    // Create log file path
    logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DevTestServer",
        $"stdio-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    
    Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
    
    // DON'T write to stderr - it can confuse MCP clients!
    // Log path will be in the log file itself
}

// Add Tool service to the container.
builder.AddToolsService();

// Test service registration
builder.Services.AddScoped<CalculatorService>();


var app = builder.Build();

// Check for stdio mode
if (isStdioMode)
{
    await StdioMode.RunAsync(app.Services, logPath);
    return;
}

// Regular HTTP/WebSocket mode
// Enable WebSockets (must be before mapping)
app.UseWebSockets();

// Configure the HTTP request pipeline.
app.MapEndpoints();

app.Run();
