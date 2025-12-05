namespace Mcp.Gateway.Server.Endpoints.Health;

using System.Reflection;
using System.Text.Json;

internal static class Health
{
    /// <summary>
    /// Maps the /health endpoint.
    /// </summary>
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder group)
    {
        // GET /health
        group.MapGet("", GetHealthAsync)
            .WithTags("System")
            .WithName("Health")
            .Produces<string>(StatusCodes.Status200OK)
            .Produces<string>(StatusCodes.Status503ServiceUnavailable);
        
        return group;
    }


    /// <summary>
    /// Returns a lightweight health status.
    /// Negotiates between text/plain and application/json based on Accept header.
    /// Always sets no-cache headers.
    /// </summary>
    private static async Task<IResult> GetHealthAsync(HttpRequest request)
    {
        // Simulate async (kept to match original signature; no awaited work currently)
        await Task.CompletedTask;

        var responseHeaders = request.HttpContext.Response.Headers;

        // Explicit anti-caching
        responseHeaders.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        responseHeaders.Pragma = "no-cache";
        responseHeaders.Expires = "0";

        // Basic metadata
        var version = typeof(Health).Assembly.GetName().Version?.ToString() ?? "0.0.0";

        var machine = Environment.MachineName;
        var timestamp = DateTimeOffset.UtcNow;

        // Check if client prefers JSON
        var wantsJson = request.Headers.Accept.Any(h =>
        {
            if (h is null) return false;
            return h.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        });

        if (wantsJson)
        {
            var payload = new
            {
                status = "Healthy",
                timestamp,
                version,
                machine
            };

            responseHeaders.ContentType = "application/json; charset=utf-8";
            string debudThis = JsonSerializer.Serialize(payload);
            return Results.Json(payload);
        }

        // Plain text fallback
        responseHeaders.ContentType = "text/plain; charset=utf-8";
        // Optional extra metadata as headers (keep minimal)
        responseHeaders["X-App-Version"] = version;
        responseHeaders["X-Machine"] = machine;
        responseHeaders["X-Timestamp"] = timestamp.ToString("O");

        return Results.Text("Healthy");
    }
}
