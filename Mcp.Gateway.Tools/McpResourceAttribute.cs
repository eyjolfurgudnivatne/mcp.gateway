namespace Mcp.Gateway.Tools;

using System;

/// <summary>
/// Marks a method as an MCP resource.
/// Resources provide data/content that clients can read (readonly in v1.5.0).
/// </summary>
/// <remarks>
/// Example usage:
/// <code>
/// [McpResource("file://logs/app.log",
///     Name = "Application Logs",
///     Description = "Server application logs",
///     MimeType = "text/plain")]
/// public JsonRpcMessage AppLogs(JsonRpcMessage request)
/// {
///     var logs = File.ReadAllText("app.log");
///     return ToolResponse.Success(request.Id, new ResourceContent(...));
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpResourceAttribute : Attribute
{
    /// <summary>
    /// Creates an MCP resource attribute.
    /// </summary>
    /// <param name="uri">
    /// Resource URI. Must follow URI format with a scheme (e.g., "file://", "db://", "http://", "system://").
    /// Examples: "file://logs/app.log", "db://users/123", "system://status"
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if uri is null</exception>
    /// <exception cref="ArgumentException">Thrown if uri is empty or whitespace</exception>
    public McpResourceAttribute(string uri)
    {
        if (uri == null)
            throw new ArgumentNullException(nameof(uri));
        
        if (string.IsNullOrWhiteSpace(uri))
            throw new ArgumentException("Resource URI cannot be empty or whitespace", nameof(uri));
        
        Uri = uri;
    }

    /// <summary>
    /// Resource URI (e.g., "file://logs/app.log", "db://users/123").
    /// Must follow URI format with a scheme.
    /// </summary>
    public string Uri { get; }
    
    /// <summary>
    /// Human-readable name (optional).
    /// If null, will be derived from the URI or method name.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Resource description (optional).
    /// Describes what data/content this resource provides.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// MIME type (e.g., "text/plain", "application/json").
    /// Optional, can be auto-detected or set explicitly.
    /// </summary>
    public string? MimeType { get; set; }
}
