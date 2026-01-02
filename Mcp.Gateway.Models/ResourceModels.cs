#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

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
