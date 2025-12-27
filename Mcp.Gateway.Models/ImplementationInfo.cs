namespace Mcp.Gateway.Tools;

using System;
using System.Runtime.Intrinsics.X86;
using System.Text.Json.Serialization;

/// <summary>
/// Describes the MCP implementation.
/// </summary>
/// <param name="Name">Intended for programmatic or logical use, but used as a display name in past specs or fallback (if title isn’t present).</param>
/// <param name="Version">The version of the implementation.</param>
/// <param name="Description">A brief description of the implementation.</param>
/// <param name="Title">A human-readable title for the implementation.</param>
/// <param name="Icons">A list of icons representing the implementation.</param>
/// <param name="WebsiteUrl">A URL to the implementation's website.</param>
public sealed record ImplementationInfo(
    /// <summary>
    /// Intended for programmatic or logical use, but used as a display name in past specs or fallback (if title isn’t present).
    /// </summary>
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    /// <summary>
    /// An optional human-readable description of what this implementation does.
    /// This can be used by clients or servers to provide context about their purpose and capabilities. For example, a server might describe the types of resources or tools it provides, while a client might describe its intended use case.
    /// </summary>
    [property: JsonPropertyName("description")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Description,
    /// <summary>
    /// Intended for UI and end-user contexts — optimized to be human-readable and easily understood, even by those unfamiliar with domain-specific terminology.
    /// If not provided, the name should be used for display (except for Tool, where annotations.title should be given precedence over using name, if present).
    /// </summary>
    [property: JsonPropertyName("title")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Title,
    /// <summary>
    /// Optional set of sized icons that the client can display in a user interface.
    /// </summary>
    [property: JsonPropertyName("icons")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] McpIconDefinition[]? Icons,
    /// <summary>
    /// An optional URL of the website for this implementation.
    /// </summary>
    [property: JsonPropertyName("websiteUrl")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? WebsiteUrl
    )
{
}
