#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// The server’s response to a tools/list request from the client.
/// </summary>
public class ListToolsResult
{
    /// <summary>
    /// List of definitions for tools the client can call.
    /// </summary>
    [JsonPropertyName("tools")]
    public List<ToolItem> Tools { get; set; } = [];

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
/// Definition for a tool the client can call.
/// </summary>
public class ToolItem
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
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>
    /// A human-readable description of the tool.
    /// This can be used by clients to improve the LLM’s understanding
    /// of available tools.It can be thought of like a “hint” to the model.
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// The _meta property/parameter is reserved by MCP to allow clients and servers to attach additional metadata to their interactions.
    /// </summary>
    [JsonPropertyName("_meta")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>
    /// A JSON Schema object defining the expected parameters for the tool.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public object InputSchema { get; set; } = new { type = "object", properties = new { } };

    /// <summary>
    /// An optional JSON Schema object defining the structure of the tool’s
    /// output returned in the structuredContent field of a CallToolResult.
    /// </summary>
    [JsonPropertyName("outputSchema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? OutputSchema { get; set; }

    /// <summary>
    /// Optional set of sized icons that the client can display in a user interface.
    /// </summary>
    [JsonPropertyName("icons")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<McpIconDefinition>? Icons { get; set; }

    /// <summary>
    /// Optional additional tool information.
    /// Display name precedence order is: title, annotations.title, then name.
    /// </summary>
    [JsonPropertyName("annotations")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolAnnotation? Annotations { get; set; }

    /// <summary>
    /// Execution-related properties for this tool.
    /// </summary>
    [JsonPropertyName("execution")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ToolExecution? Execution { get; set; }
}

/// <summary>
/// Additional properties describing a Tool to clients.
/// NOTE: all properties in ToolAnnotations are hints.
/// They are not guaranteed to provide a faithful description of tool
/// behavior (including descriptive properties like title).
/// Clients should never make tool use decisions based on ToolAnnotations received from untrusted servers.
/// </summary>
public class ToolAnnotation
{
    /// <summary>
    /// If true, the tool may perform destructive updates to its environment.
    /// If false, the tool performs only additive updates.
    /// (This property is meaningful only when readOnlyHint == false)
    /// Default: true
    /// </summary>
    [JsonPropertyName("destructiveHint")]
    public bool DestructiveHint { get; set; } = true;

    /// <summary>
    /// If true, calling the tool repeatedly with the same arguments will have no
    /// additional effect on its environment.
    /// (This property is meaningful only when readOnlyHint == false)
    /// Default: false
    /// </summary>
    [JsonPropertyName("idempotentHint")]
    public bool IdempotentHint { get; set; } = false;

    /// <summary>
    /// If true, this tool may interact with an “open world” of external entities.
    /// If false, the tool’s domain of interaction is closed.
    /// For example, the world of a web search tool is open, whereas that of a memory tool is not.
    /// Default: true
    /// </summary>
    [JsonPropertyName("openWorldHint")]
    public bool OpenWorldHint { get; set; } = true;

    /// <summary>
    /// If true, the tool does not modify its environment.
    /// Default: false
    /// </summary>
    [JsonPropertyName("readOnlyHint")]
    public bool ReadOnlyHint { get; set; } = false;

    /// <summary>
    /// A human-readable title for the tool.
    /// </summary>
    [JsonPropertyName("title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }
}

/// <summary>
/// Execution-related properties for a tool.
/// </summary>
public class ToolExecution
{
    /// <summary>
    /// Indicates whether this tool supports task-augmented execution.
    /// This allows clients to handle long-running operations through polling the task system.
    /// - “forbidden”: Tool does not support task-augmented execution(default when absent)
    /// - “optional”: Tool may support task-augmented execution
    /// - “required”: Tool requires task-augmented execution
    /// Default: “forbidden”
    /// </summary>
    [JsonPropertyName("taskSupport")]
    public string TaskSupport { get; set; } = "forbidden";
}