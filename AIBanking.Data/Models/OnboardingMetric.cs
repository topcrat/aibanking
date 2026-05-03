namespace AIBanking.Models;

/// <summary>
/// Tracks the full lifecycle of a digital onboarding attempt for KPI reporting.
/// One record per AccountApplication from creation through to Active or terminal failure.
/// </summary>
public class OnboardingMetric
{
    public Guid     Id            { get; set; }
    public Guid     ApplicationId { get; set; }

    public DateTime StartedAt     { get; set; }
    public DateTime? CompletedAt  { get; set; }

    /// <summary>Wall-clock seconds from StartedAt to CompletedAt.</summary>
    public int?     DurationSeconds { get; set; }

    /// <summary>Last stage reached: BvnVerification, FraudCheck, DocumentReview, CustomerCreation, AccountCreation, Completed.</summary>
    public string   LastStage     { get; set; } = "BvnVerification";

    /// <summary>Success, FailedBvn, FailedFraud, FailedKyc, Rejected, Abandoned.</summary>
    public string   Outcome       { get; set; } = "InProgress";

    public string?  FailureReason { get; set; }

    /// <summary>True when the account is live and all digital services are active.</summary>
    public bool     IsSuccess     { get; set; }
}
