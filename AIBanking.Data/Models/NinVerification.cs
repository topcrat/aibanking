using AIBanking.Enums;

namespace AIBanking.Models;

public class NinVerification
{
    public Guid                  Id            { get; set; }
    public Guid                  ApplicationId { get; set; }
    public string                NinNumber     { get; set; } = string.Empty;
    public NinVerificationStatus Status        { get; set; }
    public string?               VerifiedName  { get; set; }
    public string?               VerifiedDob   { get; set; }
    public bool                  NameMatch     { get; set; }
    public bool                  DobMatch      { get; set; }
    public string?               FailureReason { get; set; }
    public int                   AttemptCount  { get; set; }
    public DateTime              AttemptedAt   { get; set; }
    public DateTime?             VerifiedAt    { get; set; }
}
