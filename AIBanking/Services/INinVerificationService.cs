using AIBanking.Models;

namespace AIBanking.Services;

public interface INinVerificationService
{
    Task<NinVerification> VerifyAsync(
        Guid   applicationId,
        string ninNumber,
        string? applicantName = null,
        string? applicantDob  = null,
        CancellationToken ct  = default);
}
