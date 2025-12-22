using Mcp.Gateway.Tools;
using MEAIIntegration;
using Microsoft.Extensions.AI;
using OllamaSharp;

const string ollamaUrl = "https://ollama-proxy.multicom.internal";
const string model = "llama3.2";

var builder = WebApplication.CreateBuilder(args);

// Register HttpClient for remote MCP Gateway
builder.Services.AddHttpClient("MCP", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["MEAIIntegration:RemoteUrl"] ?? "http://localhost:62080");
    client.DefaultRequestHeaders.Add("MCP-Protocol-Version", "2025-11-25");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Choose invoker implementation based on configuration:
var mode = builder.Configuration["MEAIIntegration:Mode"] ?? "Local";

if (mode == "Local")
{
    // Option 1: Local (direct ToolInvoker call, faster)
    builder.Services.AddScoped<IMEAIInvoker, MEAILocalInvoker>();
}
else if (mode == "Remote")
{
    // Option 2: Remote (via HttpClient, for distributed scenarios)
    builder.Services.AddScoped<IMEAIInvoker, MEAIRemoteInvoker>();
}
else
{
    throw new InvalidOperationException($"Invalid MEAIIntegration:Mode: {mode}. Must be 'Local' or 'Remote'.");
}

builder.Services.AddSingleton<IChatClient>(sp =>
{
    var baseClient = new OllamaApiClient(ollamaUrl, model);
    return new ChatClientBuilder(baseClient)
        .UseFunctionInvocation()
        .Build();
});

// Add Tool service to the container.
builder.AddToolsService();

var app = builder.Build();

// Regular HTTP/WebSocket mode
// Enable WebSockets (must be before mapping)
app.UseWebSockets();

// MCP 2025-11-25 Streamable HTTP (v1.7.0 - RECOMMENDED)
app.UseProtocolVersionValidation();  // Protocol version validation
app.MapStreamableHttpEndpoint("/mcp");  // Unified endpoint (POST + GET + DELETE)

// Legacy endpoints (still work, deprecated)
app.MapHttpRpcEndpoint("/rpc");  // HTTP POST only (deprecated)
app.MapWsRpcEndpoint("/ws");     // WebSocket (keep for binary streaming)
app.MapSseRpcEndpoint("/sse");   // SSE only (deprecated, use /mcp GET instead)

app.Run();
