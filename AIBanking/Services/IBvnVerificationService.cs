using AIBanking.Models;

namespace AIBanking.Services;

public interface IBvnVerificationService
{
    /// <summary>
    /// Verify a BVN for an account application.
    /// Returns the BvnVerification record with the provider response.
    /// </summary>
    Task<BvnVerification> VerifyAsync(Guid applicationId, string bvnNumber,
        string? applicantName = null, string? applicantDob = null,
        CancellationToken ct = default);
}
