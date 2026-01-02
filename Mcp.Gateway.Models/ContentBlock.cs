namespace Mcp.Gateway.Tools;

using System.Collections.Generic;
using System.Text.Json.Serialization;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TextContent), "text")]
[JsonDerivedType(typeof(ImageContent), "image")]
[JsonDerivedType(typeof(ResourceLink), "resource_link")]
[JsonDerivedType(typeof(AudioContent), "audio")]
[JsonDerivedType(typeof(EmbeddedResource), "resource")]
public interface IContentBlock
{
    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// The type of ContentBlock.
    /// </summary>
    string Type { get; set; }
}

/// <summary>
/// Text provided to or from an LLM.
/// </summary>
public class TextContent : IContentBlock
{
    /// <summary>
    /// The text content of the message.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Optional annotations for the client.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Annotation? Annotations { get; set; }

    /// <summary>
    /// The type of ContentBlock.
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = "text";

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }
}

/// <summary>
/// An image provided to or from an LLM.
/// </summary>
public class ImageContent : IContentBlock
{
    /// <summary>
    /// The base64-encoded image data.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the image. Different providers may support different image types.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Optional annotations for the client.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Annotation? Annotations { get; set; }

    /// <summary>
    /// The type of ContentBlock.
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = "image";

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }
}

/// <summary>
/// Audio provided to or from an LLM.
/// </summary>
public class AudioContent : IContentBlock
{
    /// <summary>
    /// The base64-encoded audio data.
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the audio. Different providers may support different audio types.
    /// </summary>
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Optional annotations for the client.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Annotation? Annotations { get; set; }

    /// <summary>
    /// The type of ContentBlock.
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = "audio";

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }
}

/// <summary>
/// A resource that the server is capable of reading, included in a prompt or tool call result.
/// Note: resource links returned by tools are not guaranteed to appear in the results of resources/list requests.
/// </summary>
public class ResourceLink : IContentBlock
{
    /// <summary>
    /// Intended for programmatic or logical use, but used as a display name in past specs or fallback (if title isn’t present).
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A description of what this resource represents.
    /// This can be used by clients to improve the LLM’s understanding of available resources.It can be thought of like a “hint” to the model.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Intended for UI and end-user contexts — optimized to be human-readable and easily understood, even by those unfamiliar with domain-specific terminology.
    /// If not provided, the name should be used for display(except for Tool, where annotations.title should be given precedence over using name, if present).
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// Optional set of sized icons that the client can display in a user interface.
    /// </summary>
    [JsonPropertyName("icons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpIconDefinition>? Icons { get; set; }

    /// <summary>
    /// The MIME type of the image. Different providers may support different image types.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// Optional annotations for the client.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Annotation? Annotations { get; set; }

    /// <summary>
    /// The type of ContentBlock.
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = "resource_link";

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

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
    public string Uri { get; set; } = string.Empty;
}

/// <summary>
/// The contents of a resource, embedded into a prompt or tool call result.
/// It is up to the client how best to render embedded resources for the benefit of the LLM and/or the user.
/// </summary>
public class EmbeddedResource : IContentBlock
{
    /// <summary>
    /// Optional annotations for the client.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Annotation? Annotations { get; set; }

    /// <summary>
    /// The type of ContentBlock.
    /// </summary>
    [JsonIgnore]
    public string Type { get; set; } = "resource";

    /// <summary>
    /// Embedded resource.
    /// </summary>
    [JsonPropertyName("resource")]
    public TextOrBlobResourceContents? Resource { get; set; } = null;

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }
}

public class TextOrBlobResourceContents
{
    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// The MIME type of this resource, if known.
    /// </summary>
    [JsonPropertyName("mimeType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MimeType { get; set; }

    /// <summary>
    /// The text of the item. This must only be set if the item can actually be represented as text (not binary data).
    /// </summary>
    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>
    /// A base64-encoded string representing the binary data of the item.
    /// </summary>
    [JsonPropertyName("blob")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Blob { get; set; }

    /// <summary>
    /// The URI of this resource.
    /// </summary>
    [JsonPropertyName("uri")]
    public string Uri { get; set; } = string.Empty;
}
