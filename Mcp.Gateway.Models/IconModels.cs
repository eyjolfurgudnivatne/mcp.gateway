#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json.Serialization;

/// <summary>
/// Represents an MCP icon definition (MCP 2025-11-25).
/// Icons provide visual representations for tools, prompts, and resources in MCP clients.
/// </summary>
/// <param name="Src">Icon URL or data URI (e.g., "https://example.com/icon.png" or "data:image/svg+xml;base64,...")</param>
/// <param name="MimeType">Optional MIME type (e.g., "image/png", "image/svg+xml"). If null, clients infer from URL.</param>
/// <param name="Sizes">Optional array of size hints (e.g., ["48x48", "64x64"]). If null, clients handle sizing.</param>
/// <param name="Theme">Optional specifier for the theme this icon is designed for.
/// light indicates the icon is designed to be used with a light background,
/// and dark indicates the icon is designed to be used with a dark background.
/// If not provided, the client should assume the icon can be used with any theme.</param>
public sealed record McpIconDefinition(
    [property: JsonPropertyName("src")] string Src,
    [property: JsonPropertyName("mimeType")] string? MimeType = null,
    [property: JsonPropertyName("sizes")] string[]? Sizes = null,
    [property: JsonPropertyName("theme")] string? Theme = null);
