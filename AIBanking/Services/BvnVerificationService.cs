using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Services;

/// <summary>
/// Verifies BVN via the NIBSS (or equivalent) provider.
/// Replace the stub logic with the actual provider SDK/HTTP call in production.
/// </summary>
public sealed class BvnVerificationService(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<BvnVerificationService>     logger) : IBvnVerificationService
{
    public async Task<BvnVerification> VerifyAsync(
        Guid   applicationId,
        string bvnNumber,
        string? applicantName = null,
        string? applicantDob  = null,
        CancellationToken ct  = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        // Reuse existing record if present; bump attempt count
        var existing = await context.BvnVerifications
            .FirstOrDefaultAsync(b => b.ApplicationId == applicationId, ct);

        var record = existing ?? new BvnVerification
        {
            Id            = Guid.NewGuid(),
            ApplicationId = applicationId,
            AttemptCount  = 0
        };

        record.BvnNumber    = bvnNumber.Trim();
        record.AttemptedAt  = DateTime.UtcNow;
        record.AttemptCount++;
        record.Status       = BvnVerificationStatus.Pending;

        // ── Provider stub ────────────────────────────────────────────────────
        // Replace this block with actual NIBSS / BVN provider API call:
        //   var result = await _nibssClient.VerifyBvnAsync(bvnNumber, ct);
        var providerResult = SimulateProviderCall(bvnNumber, applicantName, applicantDob);
        // ────────────────────────────────────────────────────────────────────

        record.Status       = providerResult.Status;
        record.VerifiedName = providerResult.FullName;
        record.VerifiedDob  = providerResult.DateOfBirth;
        record.NameMatch    = providerResult.NameMatch;
        record.DobMatch     = providerResult.DobMatch;
        record.FailureReason= providerResult.FailureReason;

        if (record.Status == BvnVerificationStatus.Verified)
            record.VerifiedAt = DateTime.UtcNow;

        if (existing is null) context.BvnVerifications.Add(record);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("BVN verification for application {Id}: {Status}", applicationId, record.Status);
        return record;
    }

    // ── Simulation stub ──────────────────────────────────────────────────────
    private static ProviderResponse SimulateProviderCall(
        string bvnNumber, string? name, string? dob)
    {
        // Format check: BVN must be exactly 11 digits
        if (bvnNumber.Length != 11 || !bvnNumber.All(char.IsDigit))
            return new(BvnVerificationStatus.Failed, null, null, false, false,
                "BVN must be exactly 11 digits.");

        // Simulate a blocked test BVN
        if (bvnNumber == "00000000000")
            return new(BvnVerificationStatus.Failed, null, null, false, false,
                "BVN not found in the national database.");

        // Simulate verified result
        var verifiedName = "JOHN DOE";  // would come from provider
        var verifiedDob  = "1990-01-15";
        var nameMatch    = string.IsNullOrWhiteSpace(name) ||
                           name.Contains("john", StringComparison.OrdinalIgnoreCase);
        var dobMatch     = string.IsNullOrWhiteSpace(dob) || dob == verifiedDob;

        var status = (!nameMatch || !dobMatch)
            ? BvnVerificationStatus.Suspicious
            : BvnVerificationStatus.Verified;

        var reason = status == BvnVerificationStatus.Suspicious
            ? $"Data mismatch — name match: {nameMatch}, DOB match: {dobMatch}."
            : null;

        return new(status, verifiedName, verifiedDob, nameMatch, dobMatch, reason);
    }

    private record ProviderResponse(
        BvnVerificationStatus Status,
        string?               FullName,
        string?               DateOfBirth,
        bool                  NameMatch,
        bool                  DobMatch,
        string?               FailureReason);
}
