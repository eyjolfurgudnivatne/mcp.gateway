namespace Mcp.Gateway.Tools.Notifications;

using Mcp.Gateway.Tools;

/// <summary>
/// Interface for sending notifications to clients (v1.6.0+)
/// </summary>
public interface INotificationSender
{
    /// <summary>
    /// Sends a notification to all subscribed clients
    /// </summary>
    Task SendNotificationAsync(NotificationMessage notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active subscribers
    /// </summary>
    int SubscriberCount { get; }
}
