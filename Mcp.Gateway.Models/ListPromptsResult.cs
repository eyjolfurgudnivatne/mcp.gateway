namespace Mcp.Gateway.Tools;

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

    // Fanger opp ekstra properties ([key: string]: unknown)
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

    // Fanger opp ekstra properties ([key: string]: unknown)
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Describes a message returned as part of a prompt.
/// </summary>
public sealed record PromptMessage
{
    [JsonIgnore]
    public PromptRole Role { get; init; }

    [JsonPropertyName("role")]
    public string RoleString
    {
        get => ToWireRole(Role);
        init => Role = FromWireRole(value);
    }

    [JsonPropertyName("content")]
    public IContentBlock? Content { get; init; } = null;

    public PromptMessage() { }

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

public enum PromptRole
{
    System,
    User,
    Assistant,
    Tool
}
