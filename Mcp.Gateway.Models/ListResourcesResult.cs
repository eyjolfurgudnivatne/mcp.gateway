#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// The server’s response to a resources/list request from the client.
/// </summary>
public class ListResourcesResult
{
    /// <summary>
    /// List of known resources that the server is capable of reading.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<ResourceDefinition> Resources { get; set; } = [];

    /// <summary>
    /// An opaque token representing the pagination position after the last returned result. If present, there may be more results available.
    /// </summary>
    [JsonPropertyName("nextCursor")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextCursor { get; set; }

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Gets or sets a collection of additional data that is not mapped to known properties during JSON serialization or
    /// deserialization.
    /// </summary>
    /// <remarks>This property stores any extra JSON properties encountered during deserialization that do not
    /// have corresponding members in the class. When serializing, any key-value pairs in this dictionary will be
    /// included as additional JSON properties. This enables forward compatibility and extensibility for handling
    /// unknown or dynamic data.</remarks>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// A known resource that the server is capable of reading.
/// </summary>
public class ResourceDefinition
{
    /// <summary>
    /// Intended for programmatic or logical use, but used as a display name in
    /// past specs or fallback (if title isn’t present).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Intended for UI and end-user contexts — optimized to be human-readable and easily understood,
    /// even by those unfamiliar with domain-specific terminology.
    /// If not provided, the name should be used for display
    /// (except for Tool, where annotations.title should be given precedence over using name, if present).
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// A human-readable description of the tool.
    /// This can be used by clients to improve the LLM’s understanding
    /// of available resources.It can be thought of like a “hint” to the model.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// The MIME type of this resource, if known.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// Optional set of sized icons that the client can display in a user interface.
    /// </summary>
    [JsonPropertyName("icons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpIconDefinition>? Icons { get; set; }

    /// <summary>
    /// The size of the raw resource content, in bytes (i.e., before base64 encoding or any tokenization), if known.
    /// This can be used by Hosts to display file sizes and estimate context window usage.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Size { get; set; }

    /// <summary>
    /// The URI of this resource.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Uri { get; set; }

    /// <summary>
    /// Optional annotations for the client.
    /// The client can use annotations to inform how objects are used or displayed.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Annotation? Annotations { get; set; }
}

/// <summary>
/// Optional annotations for the client. The client can use annotations to inform how objects are used or displayed
/// </summary>
public class Annotation
{
    /// <summary>
    /// Describes who the intended audience of this object or data is.
    /// It can include multiple entries to indicate content useful
    /// for multiple audiences (e.g., [“user”, “assistant”]).
    /// </summary>
    [JsonPropertyName("audience")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Audience { get; set; }

    /// <summary>
    /// The moment the resource was last modified, as an ISO 8601 formatted string.
    /// Should be an ISO 8601 formatted string (e.g., “2025-01-12T15:00:58Z”).
    /// Examples: last activity timestamp in an open file, timestamp when the resource was attached, etc.
    /// </summary>
    [JsonPropertyName("lastModified")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LastModified { get; set; }

    /// <summary>
    /// Describes how important this data is for operating the server.
    /// A value of 1 means “most important,” and indicates that the data is effectively required,
    /// while 0 means “least important,” and indicates that the data is entirely optional.
    /// </summary>
    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Priority { get; set; }
}