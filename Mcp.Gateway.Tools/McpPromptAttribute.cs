namespace Mcp.Gateway.Tools;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// Marks a method as an MCP prompt.
/// Prompt name can be specified explicitly or auto-generated from method name.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpPromptAttribute : Attribute
{
    /// <summary>
    /// Creates an MCP prompt attribute with optional name.
    /// If name is null, it will be auto-generated from the method name.
    /// </summary>
    /// <param name="name">
    /// Prompt name (optional). Must match pattern ^[a-zA-Z0-9_-]{1,128}$.
    /// If null, auto-generated from method name (e.g., "AddNumbersPrompt" → "add_numbers_prompt").
    /// </param>
    public McpPromptAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Prompt name. If null, will be auto-generated from method name.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Prompt title (optional). If null, humanized from prompt name.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Prompt description (optional).
    /// This is the description shown in prompts/list and to MCP clients.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// JSON Schema for input parameters (optional).
    /// If null, will be auto-generated from method parameters (future feature).
    /// </summary>
    public string? InputSchema { get; set; }
    
    /// <summary>
    /// Optional icon URL for this prompt (MCP 2025-11-25).
    /// Provides a visual representation in MCP clients.
    /// </summary>
    /// <example>
    /// "https://example.com/prompt-icon.png"
    /// </example>
    public string? Icon { get; set; }
}

public sealed record PromptResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("messages")] IReadOnlyList<PromptMessage> Messages,
    [property: JsonPropertyName("arguments")] object Arguments);

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
    public string Content { get; init; } = "";

    public PromptMessage() { }

    public PromptMessage(PromptRole role, string content)
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