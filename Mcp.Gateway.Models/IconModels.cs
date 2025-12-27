namespace Mcp.Gateway.Tools;

using System.Text.Json.Serialization;

/// <summary>
/// Represents an MCP icon definition (MCP 2025-11-25).
/// Icons provide visual representations for tools, prompts, and resources in MCP clients.
/// </summary>
/// <param name="Src">Icon URL or data URI (e.g., "https://example.com/icon.png" or "data:image/svg+xml;base64,...")</param>
/// <param name="MimeType">Optional MIME type (e.g., "image/png", "image/svg+xml"). If null, clients infer from URL.</param>
/// <param name="Sizes">Optional array of size hints (e.g., ["48x48", "64x64"]). If null, clients handle sizing.</param>
public sealed record McpIconDefinition(
    [property: JsonPropertyName("src")] string Src,
    [property: JsonPropertyName("mimeType")] string? MimeType = null,
    [property: JsonPropertyName("sizes")] string[]? Sizes = null);
