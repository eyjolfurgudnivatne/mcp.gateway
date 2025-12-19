using AuthorizationMcpServer.Authorization;
using AuthorizationMcpServer.Services;
using Mcp.Gateway.Tools;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<ITokenValidationService, SimpleTokenValidationService>();
builder.Services.AddHttpContextAccessor();  // Required for AuthorizationHook

// Register ToolService + ToolInvoker
builder.AddToolsService();

// Register authorization hook (v1.8.0)
builder.AddToolLifecycleHook<AuthorizationHook>();

var app = builder.Build();

// Authorization middleware (validates token and stores user info)
app.Use(async (context, next) =>
{
    // Skip auth for health check
    if (context.Request.Path == "/health")
    {
        await next();
        return;
    }
    
    // Check Authorization header
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsJsonAsync(new
        {
            jsonrpc = "2.0",
            error = new
            {
                code = -32000,
                message = "Unauthorized",
                data = new
                {
                    detail = "Missing Authorization header",
                    hint = "Include 'Authorization: Bearer <token>' header"
                }
            },
            id = (object?)null
        });
        return;
    }
    
    // Extract token
    var token = authHeader.ToString();
    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        token = token.Substring("Bearer ".Length).Trim();
    }
    
    // Validate token
    var tokenService = context.RequestServices.GetRequiredService<ITokenValidationService>();
    var validationResult = await tokenService.ValidateTokenAsync(token);
    
    if (!validationResult.IsValid)
    {
        var errorCode = validationResult.IsExpired ? -32001 : -32002;
        var message = validationResult.IsExpired ? "Session expired" : "Forbidden";
        
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        
        await context.Response.WriteAsJsonAsync(new
        {
            jsonrpc = "2.0",
            error = new
            {
                code = errorCode,
                message = message,
                data = new
                {
                    detail = validationResult.ErrorMessage,
                    hint = "Request a new token or check your credentials"
                }
            },
            id = (object?)null
        });
        return;
    }
    
    // Store user info in HttpContext.Items for AuthorizationHook
    context.Items["UserId"] = validationResult.UserId;
    context.Items["UserRoles"] = validationResult.Roles;
    
    await next();
});

// WebSockets for streaming
app.UseWebSockets();

// MCP endpoint
app.MapStreamableHttpEndpoint("/mcp");

// Legacy endpoints
app.MapHttpRpcEndpoint("/rpc");
app.MapWsRpcEndpoint("/ws");

// Health check (no auth required)
app.MapGet("/health", () => Results.Json(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    features = new[] { "authorization", "lifecycle-hooks", "mcp-2025-11-25" }
}));

app.Run();
