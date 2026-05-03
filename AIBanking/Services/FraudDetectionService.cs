using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Services;

/// <summary>
/// Rule-based fraud detection. Each triggered rule contributes points to a risk score.
/// Replace or augment with an AI/ML model call in production.
/// </summary>
public sealed class FraudDetectionService(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<FraudDetectionService>      logger) : IFraudDetectionService
{
    public async Task<FraudAssessment> AssessAsync(Guid applicationId, CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var app = await context.AccountApplications
            .Include(a => a.Documents)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new InvalidOperationException($"Application {applicationId} not found.");

        var flags  = new List<FraudFlag>();
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // ── Rule 1: BVN not verified ─────────────────────────────────────────
        var bvn = await context.BvnVerifications.AsNoTracking()
            .FirstOrDefaultAsync(b => b.ApplicationId == applicationId, ct);

        if (bvn is null || bvn.Status == BvnVerificationStatus.Pending)
            flags.Add(new("BvnNotVerified", 25, "BVN verification has not been completed."));
        else if (bvn.Status == BvnVerificationStatus.Failed)
            flags.Add(new("BvnFailed", 40, "BVN verification failed — number not found."));
        else if (bvn.Status == BvnVerificationStatus.Suspicious)
            flags.Add(new("BvnSuspicious", 30, "BVN data mismatch: name or DOB does not match."));

        // ── Rule 2: Same BVN on multiple recent applications ─────────────────
        if (bvn is not null && !string.IsNullOrWhiteSpace(bvn.BvnNumber))
        {
            var duplicateBvnCount = await context.BvnVerifications.AsNoTracking()
                .CountAsync(b => b.BvnNumber == bvn.BvnNumber &&
                                 b.ApplicationId != applicationId &&
                                 b.AttemptedAt   >= cutoff, ct);
            if (duplicateBvnCount > 0)
                flags.Add(new("DuplicateBvn", 40,
                    $"BVN used in {duplicateBvnCount} other application(s) in the last 30 days."));
        }

        // ── Rule 3: Same phone number on multiple recent applications ─────────
        var phone = app.ExtractedInfo?.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var dupPhoneApps = await context.AccountApplications.AsNoTracking()
                .CountAsync(a => a.Id != applicationId &&
                                 a.ExtractedInfo != null &&
                                 a.ExtractedInfo.PhoneNumber == phone &&
                                 a.CreatedAt >= cutoff, ct);
            if (dupPhoneApps > 0)
                flags.Add(new("DuplicatePhone", 20,
                    $"Phone number appears on {dupPhoneApps} other application(s) in 30 days."));
        }

        // ── Rule 4: Missing required documents ───────────────────────────────
        var docTypes = app.Documents.Select(d => d.Type).ToHashSet();
        if (!docTypes.Contains(DocumentType.AccountOpeningForm) ||
            !docTypes.Contains(DocumentType.IdentityCard))
            flags.Add(new("MissingDocuments", 15, "One or more required documents not uploaded."));

        // ── Rule 5: No usable extracted identity info ─────────────────────────
        if (app.ExtractedInfo is null || string.IsNullOrWhiteSpace(app.ExtractedInfo.FullName))
            flags.Add(new("ExtractionFailed", 15, "Full name could not be extracted from documents."));

        // ── Rule 6: Multiple failed BVN attempts ──────────────────────────────
        if (bvn is { AttemptCount: > 2 })
            flags.Add(new("ExcessiveBvnAttempts", 20,
                $"BVN verification attempted {bvn.AttemptCount} times."));

        // ── Rule 7: NIN not verified ──────────────────────────────────────────
        var nin = await context.NinVerifications.AsNoTracking()
            .FirstOrDefaultAsync(n => n.ApplicationId == applicationId, ct);

        if (nin is null || nin.Status == NinVerificationStatus.Pending)
            flags.Add(new("NinNotVerified", 20, "NIN verification has not been completed."));
        else if (nin.Status == NinVerificationStatus.Failed)
            flags.Add(new("NinFailed", 35, "NIN verification failed — number not found in NIMC database."));
        else if (nin.Status == NinVerificationStatus.Suspicious)
            flags.Add(new("NinSuspicious", 25, "NIN data mismatch: name or DOB does not match."));

        // ── Rule 8: No consent captured (NDPA requirement) ───────────────────
        if (!app.ConsentGiven)
            flags.Add(new("NoConsent", 10, "Applicant has not given explicit data-usage consent (NDPA)."));

        // ── Rule 9: PEP / Sanctions screening (stub) ─────────────────────────
        // Replace with live watchlist API (e.g. Dow Jones, World-Check, NFIU) in production.
        var fullName = app.ExtractedInfo?.FullName ?? string.Empty;
        var pepHit   = CheckPepList(fullName);
        if (pepHit is not null)
            flags.Add(new("PepMatch", 50, $"Name '{fullName}' matched PEP/sanctions watchlist: {pepHit}."));

        // ── Rule 10: Consent given but NIN missing from application ──────────
        if (string.IsNullOrWhiteSpace(app.NinNumber))
            flags.Add(new("NinMissing", 15, "NIN not provided on the application."));

        // ── Score → Level ─────────────────────────────────────────────────────
        var totalScore = Math.Min(flags.Sum(f => f.Score), 100);
        var level = totalScore switch
        {
            <= 25 => FraudRiskLevel.Low,
            <= 50 => FraudRiskLevel.Medium,
            <= 75 => FraudRiskLevel.High,
            _     => FraudRiskLevel.Critical
        };

        // Upsert assessment (replace previous if exists)
        var existing = await context.FraudAssessments
            .FirstOrDefaultAsync(f => f.ApplicationId == applicationId, ct);

        var assessment = existing ?? new FraudAssessment
        {
            Id            = Guid.NewGuid(),
            ApplicationId = applicationId
        };

        assessment.RiskScore  = totalScore;
        assessment.RiskLevel  = level;
        assessment.Flags      = JsonSerializer.Serialize(flags);
        assessment.AssessedAt = DateTime.UtcNow;
        assessment.Outcome    = null;  // awaiting review

        if (existing is null) context.FraudAssessments.Add(assessment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Fraud assessment for application {Id}: score={Score}, level={Level}, flags={Count}",
            applicationId, totalScore, level, flags.Count);

        return assessment;
    }

    /// <summary>
    /// Stub PEP/sanctions check. Replace with a live watchlist API in production.
    /// Returns a non-null string describing the match category if a hit is found.
    /// </summary>
    private static string? CheckPepList(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return null;

        // Example stub entries — replace with real watchlist integration
        var pepNames = new[]
        {
            "SANCTIONED PERSON",
            "BLOCKED ENTITY"
        };

        foreach (var entry in pepNames)
        {
            if (fullName.Contains(entry, StringComparison.OrdinalIgnoreCase))
                return "Global Sanctions List";
        }

        return null;
    }

    private record FraudFlag(string Rule, int Score, string Detail);
}
