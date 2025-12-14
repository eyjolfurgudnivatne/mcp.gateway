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
    /// Prompt description (optional).
    /// This is the description shown in prompts/list and to MCP clients.
    /// </summary>
    public string? Description { get; set; }
}
