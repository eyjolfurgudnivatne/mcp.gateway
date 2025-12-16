namespace Mcp.Gateway.Tools;

using Mcp.Gateway.Tools.Notifications;
using Microsoft.Extensions.Logging;

/// <summary>
/// ToolInvoker partial class - Notifications support (v1.6.0+)
/// </summary>
public partial class ToolInvoker
{
    /// <summary>
    /// Sends a tools/changed notification to all subscribers
    /// </summary>
    public async Task NotifyToolsChangedAsync(CancellationToken cancellationToken = default)
    {
        if (_notificationSender is null)
        {
            _logger.LogWarning("NotificationSender not configured - cannot send tools/changed notification");
            return;
        }

        try
        {
            var notification = NotificationMessage.ToolsChanged();
            await _notificationSender.SendNotificationAsync(notification, cancellationToken);
            _logger.LogInformation("Sent tools/changed notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send tools/changed notification");
        }
    }

    /// <summary>
    /// Sends a prompts/changed notification to all subscribers
    /// </summary>
    public async Task NotifyPromptsChangedAsync(CancellationToken cancellationToken = default)
    {
        if (_notificationSender is null)
        {
            _logger.LogWarning("NotificationSender not configured - cannot send prompts/changed notification");
            return;
        }

        try
        {
            var notification = NotificationMessage.PromptsChanged();
            await _notificationSender.SendNotificationAsync(notification, cancellationToken);
            _logger.LogInformation("Sent prompts/changed notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send prompts/changed notification");
        }
    }

    /// <summary>
    /// Sends a resources/updated notification to all subscribers
    /// </summary>
    /// <param name="uri">Optional: specific resource URI that changed</param>
    public async Task NotifyResourcesUpdatedAsync(string? uri = null, CancellationToken cancellationToken = default)
    {
        if (_notificationSender is null)
        {
            _logger.LogWarning("NotificationSender not configured - cannot send resources/updated notification");
            return;
        }

        try
        {
            var notification = NotificationMessage.ResourcesUpdated(uri);
            await _notificationSender.SendNotificationAsync(notification, cancellationToken);
            _logger.LogInformation("Sent resources/updated notification for URI: {Uri}", uri ?? "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send resources/updated notification");
        }
    }
}
