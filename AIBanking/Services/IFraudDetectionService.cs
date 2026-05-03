using AIBanking.Models;

namespace AIBanking.Services;

public interface IFraudDetectionService
{
    /// <summary>
    /// Run rule-based fraud assessment for an application.
    /// Returns the FraudAssessment record with risk score, level, and triggered flags.
    /// </summary>
    Task<FraudAssessment> AssessAsync(Guid applicationId, CancellationToken ct = default);
}
