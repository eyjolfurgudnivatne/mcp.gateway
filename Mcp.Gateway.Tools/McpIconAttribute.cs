namespace Mcp.Gateway.Tools;

using System;

/// <summary>
/// Defines an icon for an MCP tool, resource, or prompt.
/// Can be used multiple times on a method to define multiple icons (e.g. different themes or sizes).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class McpIconAttribute : Attribute
{
    /// <summary>
    /// Creates an MCP icon attribute.
    /// </summary>
    /// <param name="src">Icon URL or data URI.</param>
    public McpIconAttribute(string src)
    {
        Src = src;
    }

    /// <summary>
    /// Creates an MCP icon attribute.
    /// </summary>
    /// <param name="src">Icon URL or data URI.</param>
    /// <param name="mimeType">Optional MIME type (e.g., "image/png").</param>
    public McpIconAttribute(string src, string mimeType)
    {
        Src = src;
        MimeType = mimeType;
    }

    /// <summary>
    /// Creates an MCP icon attribute.
    /// </summary>
    /// <param name="src">Icon URL or data URI.</param>
    /// <param name="mimeType">Optional MIME type (e.g., "image/png").</param>
    /// <param name="theme">Optional theme ("McpIconTheme.Light" or "McpIconTheme.Dark").</param>
    public McpIconAttribute(string src, string mimeType, McpIconTheme theme)
    {
        Src = src;
        MimeType = mimeType;
        Theme = theme;
    }

    /// <summary>
    /// Icon URL or data URI.
    /// </summary>
    public string Src { get; }

    /// <summary>
    /// Optional MIME type (e.g., "image/png").
    /// </summary>
    public string? MimeType { get; set; }

    /// <summary>
    /// Optional size hints (e.g., new[] { "48x48", "64x64" }).
    /// </summary>
    public string[]? Sizes { get; set; }

    /// <summary>
    /// Optional theme ("light" or "dark").
    /// </summary>
    public McpIconTheme? Theme { get; set; }
}

/// <summary>
/// Theme for MCP icons.
/// </summary>
public enum McpIconTheme
{
    Light,
    Dark
}
