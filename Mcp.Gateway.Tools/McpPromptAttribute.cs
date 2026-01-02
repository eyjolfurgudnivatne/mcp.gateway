namespace Mcp.Gateway.Tools;

using System;

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
    /// If null, will be auto-generated from method parameters.
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
