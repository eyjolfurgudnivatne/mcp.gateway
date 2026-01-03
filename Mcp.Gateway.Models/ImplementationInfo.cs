#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json.Serialization;

/// <summary>
/// Describes the MCP implementation.
/// </summary>
public class ImplementationInfo
{
    /// <summary>
    /// Intended for programmatic or logical use, but used as a display name in past specs or fallback (if title isn’t present).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The version of the implementation.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// An optional human-readable description of what this implementation does.
    /// This can be used by clients or servers to provide context about their purpose and capabilities. For example, a server might describe the types of resources or tools it provides, while a client might describe its intended use case.
    /// </summary>
    [JsonPropertyName("description")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; } = null;

    /// <summary>
    /// Intended for UI and end-user contexts — optimized to be human-readable and easily understood, even by those unfamiliar with domain-specific terminology.
    /// If not provided, the name should be used for display (except for Tool, where annotations.title should be given precedence over using name, if present).
    /// </summary>
    [JsonPropertyName("title")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; } = null;

    /// <summary>
    /// Optional set of sized icons that the client can display in a user interface.
    /// </summary>
    [JsonPropertyName("icons")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpIconDefinition[]? Icons { get; set; } = null;

    /// <summary>
    /// An optional URL of the website for this implementation.
    /// </summary>
    [JsonPropertyName("websiteUrl")][JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WebsiteUrl { get; set; } = null;
}
