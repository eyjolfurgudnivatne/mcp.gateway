namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware for validating MCP-Protocol-Version header on MCP endpoints (v1.7.0).
/// Implements MCP 2025-11-25 protocol version validation.
/// </summary>
public sealed class McpProtocolVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpProtocolVersionMiddleware> _logger;
    
    /// <summary>
    /// Supported MCP protocol versions.
    /// </summary>
    private static readonly string[] SupportedVersions = ["2025-11-25", "2025-06-18", "2025-03-26"];
    
    /// <summary>
    /// Default protocol version assumed when header is missing (backward compatibility).
    /// </summary>
    private const string DefaultVersion = "2025-03-26";

    public McpProtocolVersionMiddleware(
        RequestDelegate next,
        ILogger<McpProtocolVersionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Validates MCP-Protocol-Version header on /mcp endpoints.
    /// Returns 400 Bad Request if version is unsupported.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate MCP endpoints
        if (!context.Request.Path.StartsWithSegments("/mcp"))
        {
            await _next(context);
            return;
        }

        // Extract protocol version header
        var protocolVersion = context.Request.Headers["MCP-Protocol-Version"].ToString();

        // Backward compatibility: If missing, assume default version
        if (string.IsNullOrEmpty(protocolVersion))
        {
            protocolVersion = DefaultVersion;
            _logger.LogWarning(
                "Missing MCP-Protocol-Version header on {Path}, assuming {Version}",
                context.Request.Path,
                protocolVersion);
        }

        // Validate version
        if (!SupportedVersions.Contains(protocolVersion))
        {
            _logger.LogWarning(
                "Unsupported MCP protocol version: {Version} (supported: {Supported})",
                protocolVersion,
                string.Join(", ", SupportedVersions));

            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var error = new
            {
                error = new
                {
                    code = -32600,
                    message = "Unsupported protocol version",
                    data = new
                    {
                        provided = protocolVersion,
                        supported = SupportedVersions
                    }
                }
            };

            await context.Response.WriteAsJsonAsync(error);
            return;
        }

        // Store version in HttpContext.Items for later use
        context.Items["MCP-Protocol-Version"] = protocolVersion;

        _logger.LogDebug(
            "Validated MCP protocol version: {Version} for {Path}",
            protocolVersion,
            context.Request.Path);

        await _next(context);
    }
}
