#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// Represents the set of capabilities supported by the server, including features such as completions, logging,
/// prompts, resources, tasks, tools, and any experimental extensions.
/// </summary>
/// <remarks>This record is typically used to communicate the server's supported features to clients during
/// capability negotiation. The exact structure of each capability property is determined by the server implementation
/// and protocol version. Properties that are null indicate that the corresponding capability is not supported or not
/// advertised by the server.</remarks>
/// <param name="Completions">An object describing the server's support for completion features. The structure and content depend on the server's
/// implementation. May be null if completions are not supported.</param>
/// <param name="Experimental">A dictionary containing experimental capabilities or extensions not covered by the standard specification. Keys are
/// capability names, and values are their corresponding definitions. May be null if no experimental features are
/// provided.</param>
/// <param name="Logging">An object specifying the server's logging capabilities or configuration. The format is server-defined. May be null
/// if logging is not supported or not configurable.</param>
/// <param name="Prompts">An object describing the server's support for prompt-related features. The structure is determined by the server.
/// May be null if prompts are not supported.</param>
/// <param name="Resources">An object representing the server's resource management capabilities. The content is server-specific. May be null if
/// resource management is not supported.</param>
/// <param name="Tasks">An object indicating the server's support for task-related features. The structure is defined by the server. May be
/// null if tasks are not supported.</param>
/// <param name="Tools">An object describing the server's support for tool integration or related features. The format is server-specific.
/// May be null if tools are not supported.</param>
public sealed record ServerCapabilities(
    [property: JsonPropertyName("completions")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Completions,
    [property: JsonPropertyName("experimental")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Dictionary<string, object>? Experimental = null,
    [property: JsonPropertyName("logging")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Logging = null,
    [property: JsonPropertyName("prompts")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Prompts = null,
    [property: JsonPropertyName("resources")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Resources = null,
    [property: JsonPropertyName("tasks")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Tasks = null,
    [property: JsonPropertyName("tools")][property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Tools = null)
{
}
