#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Mcp.Gateway.Tools;
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System.Text.Json.Serialization;

/// <summary>
/// Type of MCP notification (v1.6.0+)
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Tools have been added, removed, or modified.
    /// Clients should re-fetch tools/list.
    /// </summary>
    ToolsChanged,

    /// <summary>
    /// Prompts have been added, removed, or modified.
    /// Clients should re-fetch prompts/list.
    /// </summary>
    PromptsChanged,

    /// <summary>
    /// Resources have been added, removed, or modified.
    /// Clients should re-fetch resources/list.
    /// </summary>
    ResourcesUpdated
}

/// <summary>
/// MCP notification message sent from server to client (v1.6.0+)
/// </summary>
public sealed record NotificationMessage(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] object? Params = null)
{
    /// <summary>
    /// Creates a tools/changed notification
    /// </summary>
    public static NotificationMessage ToolsChanged() =>
        new("2.0", "notifications/tools/changed", new { });

    /// <summary>
    /// Creates a prompts/changed notification
    /// </summary>
    public static NotificationMessage PromptsChanged() =>
        new("2.0", "notifications/prompts/changed", new { });

    /// <summary>
    /// Creates a resources/updated notification
    /// </summary>
    /// <param name="uri">Optional: specific resource URI that changed</param>
    public static NotificationMessage ResourcesUpdated(string? uri = null)
    {
        object @params = uri is not null ? new { uri } : (object)new { };
        return new("2.0", "notifications/resources/updated", @params);
    }
}
