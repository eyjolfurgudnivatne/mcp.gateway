namespace Mcp.Gateway.Tools;

using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for MCP middleware (v1.7.0).
/// </summary>
public static class McpMiddlewareExtensions
{
    /// <summary>
    /// Adds MCP protocol version validation middleware to the pipeline.
    /// Must be called before MapStreamableHttpEndpoint or other MCP endpoints.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The application builder for chaining</returns>
    /// <remarks>
    /// This middleware validates the MCP-Protocol-Version header on all /mcp endpoints.
    /// Supported versions: 2025-11-25, 2025-06-18.
    /// Missing header defaults to 2025-03-26 for backward compatibility.
    /// Returns 400 Bad Request if version is unsupported.
    /// </remarks>
    public static IApplicationBuilder UseProtocolVersionValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<McpProtocolVersionMiddleware>();
    }
}
