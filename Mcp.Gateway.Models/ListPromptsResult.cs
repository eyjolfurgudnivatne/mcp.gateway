#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// The server’s response to a prompts/list request from the client.
/// </summary>
public class ListPromptsResult
{
    /// <summary>
    /// List of known prompts that the server is capable of reading.
    /// </summary>
    [JsonPropertyName("prompts")]
    public List<PromptDefinition> Prompts { get; set; } = [];

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
public class PromptDefinition
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
    /// An optional description of what this prompt provides.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// A list of arguments to use for templating the prompt.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PromptArgument>? Arguments { get; set; }

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
}

/// <summary>
/// Describes an argument that a prompt can accept.
/// </summary>
public class PromptArgument
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
    /// If not provided, the name should be used for display(except for Tool,
    /// where annotations.title should be given precedence over using name, if present).
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// A human-readable description of the argument.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this argument must be provided.
    /// </summary>
    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Required { get; set; }
}

/// <summary>
/// Parameters for a prompts/get request from the client to the server.
/// </summary>
public class PromptRequest {
    /// <summary>
    /// The name of the prompt to retrieve.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional arguments to customize the prompt.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Arguments { get; set; }

}

/// <summary>
/// Represents a request to retrieve a named prompt, optionally supplying arguments to customize its behavior.
/// </summary>
/// <typeparam name="TArguments">The type of the arguments used to customize the prompt. This can be any type that represents the data required by
/// the prompt, or null if no arguments are needed.</typeparam>
public class PromptRequest<TArguments>
{
    /// <summary>
    /// The name of the prompt to retrieve.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional arguments to customize the prompt.
    /// </summary>
    [JsonPropertyName("arguments")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TArguments? Arguments { get; set; }
}

/// <summary>
/// The server’s response to a prompts/get request from the client.
/// </summary>
public class PromptResponse
{
    /// <summary>
    /// An optional description for the prompt.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// Messages returned.
    /// </summary>
    [JsonPropertyName("messages")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PromptMessage> Messages { get; set; } = [];

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
/// Describes a message returned as part of a prompt.
/// </summary>
public sealed record PromptMessage
{
    /// <summary>
    /// The role of the message sender (system, user, assistant, or tool).
    /// </summary>
    [JsonIgnore]
    public PromptRole Role { get; init; }

    /// <summary>
    /// Gets or sets the role as a string value for serialization purposes.
    /// </summary>
    /// <remarks>This property is intended for use with JSON serialization and deserialization. It represents
    /// the role in a format suitable for wire transfer and may differ from the internal representation. Setting this
    /// property updates the underlying role accordingly.</remarks>
    [JsonPropertyName("role")]
    public string RoleString
    {
        get => ToWireRole(Role);
        init => Role = FromWireRole(value);
    }

    /// <summary>
    /// The content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public IContentBlock? Content { get; init; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptMessage"/> class.
    /// </summary>
    public PromptMessage() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="PromptMessage"/> class with the specified role and content.
    /// </summary>
    /// <param name="role">The role of the message sender.</param>
    /// <param name="content">The content of the message.</param>
    public PromptMessage(PromptRole role, IContentBlock content)
    {
        Role = role;
        Content = content;
    }

    private static PromptRole FromWireRole(string role) => role switch
    {
        "system" => PromptRole.System,
        "user" => PromptRole.User,
        "assistant" => PromptRole.Assistant,
        "tool" => PromptRole.Tool,
        _ => PromptRole.User
    };

    private static string ToWireRole(PromptRole role) => role switch
    {
        PromptRole.System => "system",
        PromptRole.User => "user",
        PromptRole.Assistant => "assistant",
        PromptRole.Tool => "tool",
        _ => "user"
    };
}

/// <summary>
/// Specifies the role of a message or participant in an AI prompt exchange.
/// </summary>
/// <remarks>Use this enumeration to indicate whether a message originates from the system, a user, an AI
/// assistant, or an external tool. This distinction is important for processing and interpreting conversational context
/// in AI-driven applications.</remarks>
public enum PromptRole
{
    System,
    User,
    Assistant,
    Tool
}
