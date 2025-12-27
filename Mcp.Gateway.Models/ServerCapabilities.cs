namespace Mcp.Gateway.Tools;

using System.Collections.Generic;
using System.Text.Json.Serialization;

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
