namespace Mcp.Gateway.Tools;

using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpToolAttribute(string name) : Attribute
{
    public string Name { get; } = name;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? InputSchema { get; set; }
}
