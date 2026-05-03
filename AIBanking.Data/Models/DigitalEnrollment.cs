using AIBanking.Enums;

namespace AIBanking.Models;

public class DigitalEnrollment
{
    public Guid                   Id            { get; set; }
    public Guid                   CustomerId    { get; set; }
    public Guid                   AccountId     { get; set; }

    public DigitalServiceType     ServiceType   { get; set; }
    public DigitalEnrollmentStatus Status       { get; set; } = DigitalEnrollmentStatus.Active;

    /// <summary>Auto-generated username (e.g. first3OfName + last6OfAccountNumber).</summary>
    public string Username         { get; set; } = string.Empty;

    /// <summary>Temporary password hash — customer must change on first login.</summary>
    public string PasswordHash     { get; set; } = string.Empty;

    public bool   MustChangePassword { get; set; } = true;

    public DateTime  EnrolledAt    { get; set; }
    public DateTime? LastLoginAt   { get; set; }
    public DateTime? SuspendedAt   { get; set; }
    public string?   SuspendReason { get; set; }
}
