namespace AIBanking.Models;

public class NotificationPreference
{
    public Guid    Id         { get; set; }
    public Guid    CustomerId { get; set; }   // unique FK → Customer

    // SMS — mandatory; always enabled
    public bool    SmsEnabled  { get; set; } = true;
    public string  PhoneNumber { get; set; } = string.Empty;

    // Email — optional
    public bool    EmailEnabled { get; set; }
    public string? Email        { get; set; }

    // Push notification — optional (mobile app device token)
    public bool    PushEnabled  { get; set; }
    public string? DeviceToken  { get; set; }

    public DateTime CreatedAt  { get; set; }
    public DateTime UpdatedAt  { get; set; }
}
