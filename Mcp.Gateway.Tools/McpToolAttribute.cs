namespace Mcp.Gateway.Tools;

using System;

/// <summary>
/// Marks a method as an MCP tool.
/// Tool name can be specified explicitly or auto-generated from method name.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpToolAttribute : Attribute
{
    /// <summary>
    /// Creates an MCP tool attribute with optional name.
    /// If name is null, it will be auto-generated from the method name.
    /// </summary>
    /// <param name="name">
    /// Tool name (optional). Must match pattern ^[a-zA-Z0-9_-]{1,128}$.
    /// If null, auto-generated from method name (e.g., "AddNumbersTool" → "add_numbers_tool").
    /// </param>
    public McpToolAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Tool name. If null, will be auto-generated from method name.
    /// </summary>
    public string? Name { get; }
    
    /// <summary>
    /// Tool title (optional). If null, humanized from tool name.
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// Tool description (optional).
    /// This is the description shown in tools/list and to MCP clients.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// JSON Schema for input parameters (optional).
    /// If null, will be auto-generated from method parameters (future feature).
    /// </summary>
    public string? InputSchema { get; set; }
}
