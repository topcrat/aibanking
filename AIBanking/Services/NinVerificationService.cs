using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Services;

/// <summary>
/// Verifies NIN via the NIMC (National Identity Management Commission) provider.
/// Replace the stub logic with the actual NIMC API/SDK call in production.
/// </summary>
public sealed class NinVerificationService(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<NinVerificationService>     logger) : INinVerificationService
{
    public async Task<NinVerification> VerifyAsync(
        Guid   applicationId,
        string ninNumber,
        string? applicantName = null,
        string? applicantDob  = null,
        CancellationToken ct  = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var existing = await context.NinVerifications
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId, ct);

        var record = existing ?? new NinVerification
        {
            Id            = Guid.NewGuid(),
            ApplicationId = applicationId,
            AttemptCount  = 0
        };

        record.NinNumber    = ninNumber.Trim();
        record.AttemptedAt  = DateTime.UtcNow;
        record.AttemptCount++;
        record.Status       = NinVerificationStatus.Pending;

        // ── Provider stub ────────────────────────────────────────────────────
        // Replace this block with actual NIMC API call:
        //   var result = await _nimcClient.VerifyNinAsync(ninNumber, ct);
        var providerResult = SimulateProviderCall(ninNumber, applicantName, applicantDob);
        // ────────────────────────────────────────────────────────────────────

        record.Status        = providerResult.Status;
        record.VerifiedName  = providerResult.FullName;
        record.VerifiedDob   = providerResult.DateOfBirth;
        record.NameMatch     = providerResult.NameMatch;
        record.DobMatch      = providerResult.DobMatch;
        record.FailureReason = providerResult.FailureReason;

        if (record.Status == NinVerificationStatus.Verified)
            record.VerifiedAt = DateTime.UtcNow;

        if (existing is null) context.NinVerifications.Add(record);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("NIN verification for application {Id}: {Status}", applicationId, record.Status);
        return record;
    }

    private static ProviderResponse SimulateProviderCall(
        string ninNumber, string? name, string? dob)
    {
        if (ninNumber.Length != 11 || !ninNumber.All(char.IsDigit))
            return new(NinVerificationStatus.Failed, null, null, false, false,
                "NIN must be exactly 11 digits.");

        if (ninNumber == "00000000000")
            return new(NinVerificationStatus.Failed, null, null, false, false,
                "NIN not found in the NIMC database.");

        var verifiedName = "JOHN DOE";
        var verifiedDob  = "1990-01-15";
        var nameMatch    = string.IsNullOrWhiteSpace(name) ||
                           name.Contains("john", StringComparison.OrdinalIgnoreCase);
        var dobMatch     = string.IsNullOrWhiteSpace(dob) || dob == verifiedDob;

        var status = (!nameMatch || !dobMatch)
            ? NinVerificationStatus.Suspicious
            : NinVerificationStatus.Verified;

        var reason = status == NinVerificationStatus.Suspicious
            ? $"Data mismatch — name match: {nameMatch}, DOB match: {dobMatch}."
            : null;

        return new(status, verifiedName, verifiedDob, nameMatch, dobMatch, reason);
    }

    private record ProviderResponse(
        NinVerificationStatus Status,
        string?               FullName,
        string?               DateOfBirth,
        bool                  NameMatch,
        bool                  DobMatch,
        string?               FailureReason);
}
