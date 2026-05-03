using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using AIBanking.Services;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Agents.Tools;

/// <summary>
/// Compliance action tools: BVN verification, fraud assessment, digital enrollment,
/// fraud outcome recording, and onboarding KPI tracking.
/// </summary>
internal sealed class ComplianceActionTools(
    BankingDbContext           context,
    IBvnVerificationService    bvnService,
    INinVerificationService    ninService,
    IFraudDetectionService     fraudService,
    IDigitalEnrollmentService  enrollmentService)
{
    [Description(
        "Verify the BVN (Bank Verification Number) for an application against the national database (NIBSS). " +
        "Checks that the BVN is valid, exists in the database, and that the name and date of birth match " +
        "the extracted applicant information. " +
        "Run this early in the review process before approving an application.")]
    public async Task<string> VerifyBvnAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("The 11-digit BVN number provided by the applicant")] string bvnNumber,
        [Description("Applicant full name for cross-matching (from extracted info). Optional but recommended.")] string? applicantName = null,
        [Description("Applicant date of birth for cross-matching (YYYY-MM-DD). Optional but recommended.")] string? applicantDob = null,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        if (string.IsNullOrWhiteSpace(bvnNumber))
            return Err("BVN number is required.");

        // Persist BVN on the application record
        var app = await context.AccountApplications.FindAsync([id], ct);
        if (app is null) return Err($"Application {applicationId} not found.");

        app.BvnNumber = bvnNumber.Trim();
        app.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        var result = await bvnService.VerifyAsync(id, bvnNumber, applicantName, applicantDob, ct);

        return JsonSerializer.Serialize(new
        {
            applicationId,
            bvnNumber     = result.BvnNumber,
            status        = result.Status.ToString(),
            verifiedName  = result.VerifiedName,
            verifiedDob   = result.VerifiedDob,
            nameMatch     = result.NameMatch,
            dobMatch      = result.DobMatch,
            failureReason = result.FailureReason,
            attemptCount  = result.AttemptCount,
            recommendation = result.Status switch
            {
                BvnVerificationStatus.Verified   => "BVN verified successfully. Proceed with application review.",
                BvnVerificationStatus.Suspicious => "BVN data mismatch detected. Flag for rework and request the applicant to confirm their details.",
                BvnVerificationStatus.Failed     => "BVN verification failed. Flag the application for rework — applicant must provide a valid BVN.",
                _                                => "BVN verification pending. Retry or contact the applicant."
            }
        });
    }

    [Description(
        "Verify the NIN (National Identification Number) for an application against the NIMC database. " +
        "Checks that the NIN is valid, exists in the database, and that the name and date of birth match " +
        "the extracted applicant information. " +
        "Run this alongside BVN verification before approving an application.")]
    public async Task<string> VerifyNinAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("The 11-digit NIN number provided by the applicant")] string ninNumber,
        [Description("Applicant full name for cross-matching (from extracted info). Optional but recommended.")] string? applicantName = null,
        [Description("Applicant date of birth for cross-matching (YYYY-MM-DD). Optional but recommended.")] string? applicantDob = null,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        if (string.IsNullOrWhiteSpace(ninNumber))
            return Err("NIN number is required.");

        var app = await context.AccountApplications.FindAsync([id], ct);
        if (app is null) return Err($"Application {applicationId} not found.");

        app.NinNumber = ninNumber.Trim();
        app.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        var result = await ninService.VerifyAsync(id, ninNumber, applicantName, applicantDob, ct);

        return JsonSerializer.Serialize(new
        {
            applicationId,
            ninNumber     = result.NinNumber,
            status        = result.Status.ToString(),
            verifiedName  = result.VerifiedName,
            verifiedDob   = result.VerifiedDob,
            nameMatch     = result.NameMatch,
            dobMatch      = result.DobMatch,
            failureReason = result.FailureReason,
            attemptCount  = result.AttemptCount,
            recommendation = result.Status switch
            {
                NinVerificationStatus.Verified   => "NIN verified successfully. Proceed with application review.",
                NinVerificationStatus.Suspicious => "NIN data mismatch detected. Flag for rework and request the applicant to confirm their details.",
                NinVerificationStatus.Failed     => "NIN verification failed. Flag the application for rework — applicant must provide a valid NIN.",
                _                                => "NIN verification pending. Retry or contact the applicant."
            }
        });
    }

    [Description(
        "Run a fraud risk assessment on an application. " +
        "Evaluates multiple risk signals: BVN status, duplicate BVN/phone across recent applications, " +
        "missing documents, extraction failures, and excessive retry attempts. " +
        "Returns a risk score (0–100) and level (Low/Medium/High/Critical). " +
        "Run this after BVN verification and before approving any application.")]
    public async Task<string> RunFraudAssessmentAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var assessment = await fraudService.AssessAsync(id, ct);

        return JsonSerializer.Serialize(new
        {
            applicationId,
            assessmentId = assessment.Id,
            riskScore    = assessment.RiskScore,
            riskLevel    = assessment.RiskLevel.ToString(),
            flags        = assessment.Flags,
            assessedAt   = assessment.AssessedAt,
            recommendation = assessment.RiskLevel switch
            {
                FraudRiskLevel.Low      => "Low risk — safe to proceed with approval.",
                FraudRiskLevel.Medium   => "Medium risk — review the triggered flags before approving. Human review recommended.",
                FraudRiskLevel.High     => "High risk — escalate to compliance team before approving.",
                FraudRiskLevel.Critical => "Critical risk — do not approve. Escalate immediately to fraud team.",
                _                       => "Unknown risk level."
            }
        });
    }

    [Description(
        "Enroll a customer in Mobile Banking and/or Internet Banking. " +
        "Automatically generates a username and temporary password, then delivers credentials via SMS. " +
        "The customer must have an active account before enrollment. " +
        "This is called automatically during account opening, but can be called manually if needed.")]
    public async Task<string> EnrollDigitalServicesAsync(
        [Description("The customer ID (GUID string)")] string customerId,
        [Description("The account ID (GUID string)")] string accountId,
        [Description("Customer full name (used to generate username)")] string fullName,
        [Description("The account number (used to generate username)")] string accountNumber,
        [Description("Enroll in Mobile Banking? (true/false)")] bool enrollMobile = true,
        [Description("Enroll in Internet Banking? (true/false)")] bool enrollInternet = true,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(customerId, out var cid))
            return Err("Invalid customer ID format.");
        if (!Guid.TryParse(accountId, out var aid))
            return Err("Invalid account ID format.");

        if (!enrollMobile && !enrollInternet)
            return Err("At least one service (mobile or internet) must be selected.");

        var results = new List<object>();

        if (enrollMobile)
        {
            var mobile = await enrollmentService.EnrollAsync(
                cid, aid, DigitalServiceType.MobileBanking, fullName, accountNumber, ct);
            results.Add(new
            {
                service    = "MobileBanking",
                enrollmentId = mobile.Id,
                username   = mobile.Username,
                status     = mobile.Status.ToString(),
                enrolledAt = mobile.EnrolledAt,
                credentialsSentViaSms = true
            });
        }

        if (enrollInternet)
        {
            var internet = await enrollmentService.EnrollAsync(
                cid, aid, DigitalServiceType.InternetBanking, fullName, accountNumber, ct);
            results.Add(new
            {
                service    = "InternetBanking",
                enrollmentId = internet.Id,
                username   = internet.Username,
                status     = internet.Status.ToString(),
                enrolledAt = internet.EnrolledAt,
                credentialsSentViaSms = true
            });
        }

        return JsonSerializer.Serialize(new
        {
            customerId,
            accountId,
            enrollments = results,
            message = "Digital banking credentials have been sent to the customer via SMS. Customer must change password on first login."
        });
    }

    [Description(
        "Record the outcome of a manual fraud assessment review. " +
        "Used when a compliance officer or fraud analyst reviews a High/Critical risk application. " +
        "Valid outcomes: Cleared (approved despite flags), Confirmed (fraud confirmed — reject application), Escalated (referred to senior team).")]
    public async Task<string> RecordFraudReviewOutcomeAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("Outcome: Cleared, Confirmed, or Escalated")] string outcome,
        [Description("Name of the reviewer recording the decision")] string reviewedBy,
        [Description("Notes explaining the review decision")] string reviewNotes,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        if (string.IsNullOrWhiteSpace(outcome))
            return Err("Outcome is required.");

        var validOutcomes = new[] { "Cleared", "Confirmed", "Escalated" };
        if (!validOutcomes.Contains(outcome, StringComparer.OrdinalIgnoreCase))
            return Err($"Invalid outcome '{outcome}'. Valid values: {string.Join(", ", validOutcomes)}");

        var assessment = await context.FraudAssessments
            .FirstOrDefaultAsync(f => f.ApplicationId == id, ct);

        if (assessment is null)
            return Err($"No fraud assessment found for application {applicationId}. Run fraud assessment first.");

        assessment.Outcome    = outcome;
        assessment.ReviewedBy = reviewedBy;
        assessment.ReviewNotes= reviewNotes;
        assessment.ReviewedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        return JsonSerializer.Serialize(new
        {
            applicationId,
            assessmentId  = assessment.Id,
            riskScore     = assessment.RiskScore,
            riskLevel     = assessment.RiskLevel.ToString(),
            outcome       = assessment.Outcome,
            reviewedBy    = assessment.ReviewedBy,
            reviewNotes   = assessment.ReviewNotes,
            reviewedAt    = assessment.ReviewedAt,
            nextStep = outcome.Equals("Cleared", StringComparison.OrdinalIgnoreCase)
                ? "Application cleared by fraud review — safe to proceed with approval."
                : outcome.Equals("Confirmed", StringComparison.OrdinalIgnoreCase)
                    ? "Fraud confirmed — reject the application immediately."
                    : "Escalated to senior compliance team — await their decision."
        });
    }

    [Description(
        "Record or update the onboarding KPI metric for an application. " +
        "Tracks start time, completion time, duration, last stage reached, and success/failure outcome. " +
        "The target KPI is account creation within 3 minutes (180 seconds). " +
        "Call this when onboarding completes (success or failure) to record the timing.")]
    public async Task<string> RecordOnboardingMetricAsync(
        [Description("The application ID (GUID string)")] string applicationId,
        [Description("The last stage reached (e.g. BvnVerification, FraudAssessment, CustomerCreated, AccountCreated, DigitalEnrollment)")] string lastStage,
        [Description("Was the onboarding completed successfully? (true/false)")] bool isSuccess,
        [Description("Outcome description (e.g. 'AccountOpened', 'FraudFlagged', 'ReworkRequired')")] string outcome,
        [Description("Reason for failure if isSuccess is false. Leave empty for successful onboardings.")] string? failureReason = null,
        CancellationToken ct = default)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var existing = await context.OnboardingMetrics
            .FirstOrDefaultAsync(m => m.ApplicationId == id, ct);

        var metric = existing ?? new OnboardingMetric
        {
            Id            = Guid.NewGuid(),
            ApplicationId = id,
            StartedAt     = DateTime.UtcNow
        };

        var completedAt = DateTime.UtcNow;
        metric.CompletedAt     = completedAt;
        metric.DurationSeconds = (int)(completedAt - metric.StartedAt).TotalSeconds;
        metric.LastStage       = lastStage;
        metric.IsSuccess       = isSuccess;
        metric.Outcome         = outcome;
        metric.FailureReason   = failureReason;

        if (existing is null) context.OnboardingMetrics.Add(metric);
        await context.SaveChangesAsync(ct);

        var metKpi = metric.DurationSeconds <= 180;

        return JsonSerializer.Serialize(new
        {
            applicationId,
            metricId         = metric.Id,
            startedAt        = metric.StartedAt,
            completedAt      = metric.CompletedAt,
            durationSeconds  = metric.DurationSeconds,
            metIn3Minutes    = metKpi,
            lastStage        = metric.LastStage,
            isSuccess        = metric.IsSuccess,
            outcome          = metric.Outcome,
            failureReason    = metric.FailureReason,
            kpiMessage       = metKpi
                ? $"KPI MET — onboarding completed in {metric.DurationSeconds} seconds (target: ≤180s)."
                : $"KPI MISSED — onboarding took {metric.DurationSeconds} seconds (target: ≤180s)."
        });
    }

    private static string Err(string message) =>
        JsonSerializer.Serialize(new { error = message });
}
