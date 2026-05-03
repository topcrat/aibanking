using AIBanking.Enums;
using AIBanking.Models;

namespace AIBanking.Services;

public interface IDigitalEnrollmentService
{
    /// <summary>
    /// Enroll a customer in a digital service (Mobile or Internet Banking).
    /// Generates a username and temporary password, and sends credentials via SMS.
    /// </summary>
    Task<DigitalEnrollment> EnrollAsync(
        Guid              customerId,
        Guid              accountId,
        DigitalServiceType serviceType,
        string            fullName,
        string            accountNumber,
        CancellationToken ct = default);
}
