namespace Mcp.Gateway.Tools;

/// <summary>
/// Represents an MCP resource definition.
/// Used in resources/list responses to describe available resources.
/// </summary>
/// <param name="Uri">Resource URI (e.g., "file://logs/app.log", "db://users/123")</param>
/// <param name="Name">Human-readable name of the resource</param>
/// <param name="Description">Optional description of what the resource provides</param>
/// <param name="MimeType">MIME type of the resource content (e.g., "text/plain", "application/json")</param>
/// <param name="Icon">Optional icon URL for MCP 2025-11-25 (e.g., "https://example.com/icon.png")</param>
public sealed record ResourceDefinition(
    string Uri,
    string Name,
    string? Description,
    string? MimeType,
    string? Icon = null);  // NEW: MCP 2025-11-25 icon URL

/// <summary>
/// Represents the content of a resource (from resources/read).
/// Contains the actual data/content that the resource provides.
/// </summary>
/// <param name="Uri">Resource URI that was read</param>
/// <param name="MimeType">MIME type of the content</param>
/// <param name="Text">Text content (for text-based resources). Mutually exclusive with Blob.</param>
/// <param name="Blob">Binary content (for binary resources). Mutually exclusive with Text. Reserved for v1.6+.</param>
public sealed record ResourceContent(
    string Uri,
    string? MimeType,
    string? Text = null,
    byte[]? Blob = null);
