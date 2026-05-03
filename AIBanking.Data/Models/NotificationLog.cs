using AIBanking.Enums;

namespace AIBanking.Models;

public class NotificationLog
{
    public Guid               Id           { get; set; }
    public Guid               CustomerId   { get; set; }   // FK → Customer
    public NotificationType   Type         { get; set; }
    public NotificationStatus Status       { get; set; }

    /// <summary>SMS number, email address, or device token used for this send.</summary>
    public string  Recipient    { get; set; } = string.Empty;
    public string  Subject      { get; set; } = string.Empty;
    public string  Message      { get; set; } = string.Empty;

    /// <summary>Error detail when Status = Failed.</summary>
    public string? ErrorMessage { get; set; }

    public DateTime SentAt      { get; set; }
}
