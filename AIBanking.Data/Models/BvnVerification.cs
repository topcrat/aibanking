using AIBanking.Enums;

namespace AIBanking.Models;

public class BvnVerification
{
    public Guid                  Id            { get; set; }
    public Guid                  ApplicationId { get; set; }

    public string                BvnNumber     { get; set; } = string.Empty;
    public BvnVerificationStatus Status        { get; set; } = BvnVerificationStatus.Pending;

    /// <summary>Full name returned by the BVN provider (NIBSS or equivalent).</summary>
    public string? VerifiedName    { get; set; }
    public string? VerifiedDob     { get; set; }
    public bool    NameMatch       { get; set; }
    public bool    DobMatch        { get; set; }

    /// <summary>Detail from the provider when verification fails or is suspicious.</summary>
    public string? FailureReason   { get; set; }

    public int     AttemptCount    { get; set; } = 1;
    public DateTime AttemptedAt   { get; set; }
    public DateTime? VerifiedAt   { get; set; }
}
