using AIBanking.Enums;
using AIBanking.Models;

namespace AIBanking.Services;

public interface INotificationService
{
    /// <summary>
    /// Send a notification to a customer via all their enabled channels.
    /// SMS is always attempted (mandatory). Email and push are sent only when enabled.
    /// All attempts are persisted to NotificationLog.
    /// </summary>
    Task SendAsync(Guid customerId, string subject, string message, CancellationToken ct = default);

    /// <summary>Send via a specific channel only.</summary>
    Task SendAsync(Guid customerId, NotificationType type, string subject, string message, CancellationToken ct = default);
}
