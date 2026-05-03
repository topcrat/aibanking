using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.AIAgent.Agents.Tools;

/// <summary>
/// Read-only compliance query tools: BVN verification status, fraud assessments,
/// digital enrollment status, and onboarding KPI metrics.
/// </summary>
internal sealed class ComplianceTools(BankingDbContext context)
{
    [Description(
        "Get the BVN (Bank Verification Number) verification status for an application. " +
        "Returns the verification status, name/DOB match results, attempt count, and any failure reason.")]
    public async Task<string> GetBvnVerificationAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var bvn = await context.BvnVerifications
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ApplicationId == id);

        if (bvn is null)
            return JsonSerializer.Serialize(new { applicationId, status = "NotStarted", message = "BVN verification has not been initiated for this application." });

        return JsonSerializer.Serialize(new
        {
            applicationId,
            bvnVerificationId = bvn.Id,
            bvnNumber         = bvn.BvnNumber,
            status            = bvn.Status.ToString(),
            verifiedName      = bvn.VerifiedName,
            verifiedDob       = bvn.VerifiedDob,
            nameMatch         = bvn.NameMatch,
            dobMatch          = bvn.DobMatch,
            failureReason     = bvn.FailureReason,
            attemptCount      = bvn.AttemptCount,
            attemptedAt       = bvn.AttemptedAt,
            verifiedAt        = bvn.VerifiedAt
        });
    }

    [Description(
        "Get the NIN (National Identification Number) verification status for an application. " +
        "Returns the verification status, name/DOB match results, attempt count, and any failure reason.")]
    public async Task<string> GetNinVerificationAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var nin = await context.NinVerifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.ApplicationId == id);

        if (nin is null)
            return JsonSerializer.Serialize(new { applicationId, status = "NotStarted", message = "NIN verification has not been initiated for this application." });

        return JsonSerializer.Serialize(new
        {
            applicationId,
            ninVerificationId = nin.Id,
            ninNumber         = nin.NinNumber,
            status            = nin.Status.ToString(),
            verifiedName      = nin.VerifiedName,
            verifiedDob       = nin.VerifiedDob,
            nameMatch         = nin.NameMatch,
            dobMatch          = nin.DobMatch,
            failureReason     = nin.FailureReason,
            attemptCount      = nin.AttemptCount,
            attemptedAt       = nin.AttemptedAt,
            verifiedAt        = nin.VerifiedAt
        });
    }

    [Description(
        "Get the fraud risk assessment for an application. " +
        "Returns risk score (0-100), risk level (Low/Medium/High/Critical), all triggered fraud flags, " +
        "and the review outcome if one has been recorded.")]
    public async Task<string> GetFraudAssessmentAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var assessment = await context.FraudAssessments
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.ApplicationId == id);

        if (assessment is null)
            return JsonSerializer.Serialize(new { applicationId, status = "NotAssessed", message = "No fraud assessment has been run for this application." });

        return JsonSerializer.Serialize(new
        {
            applicationId,
            assessmentId = assessment.Id,
            riskScore    = assessment.RiskScore,
            riskLevel    = assessment.RiskLevel.ToString(),
            flags        = assessment.Flags,
            outcome      = assessment.Outcome,
            reviewedBy   = assessment.ReviewedBy,
            reviewNotes  = assessment.ReviewNotes,
            assessedAt   = assessment.AssessedAt,
            reviewedAt   = assessment.ReviewedAt
        });
    }

    [Description(
        "Get the digital banking enrollment status for a customer. " +
        "Returns enrollment details for Mobile Banking and Internet Banking, including usernames " +
        "(never passwords), enrollment dates, and current status.")]
    public async Task<string> GetDigitalEnrollmentStatusAsync(
        [Description("The customer ID (GUID string)")] string customerId)
    {
        if (!Guid.TryParse(customerId, out var id))
            return Err("Invalid customer ID format.");

        var enrollments = await context.DigitalEnrollments
            .AsNoTracking()
            .Where(e => e.CustomerId == id)
            .ToListAsync();

        if (enrollments.Count == 0)
            return JsonSerializer.Serialize(new { customerId, message = "No digital enrollments found for this customer." });

        return JsonSerializer.Serialize(new
        {
            customerId,
            enrollments = enrollments.Select(e => new
            {
                enrollmentId        = e.Id,
                serviceType         = e.ServiceType.ToString(),
                status              = e.Status.ToString(),
                username            = e.Username,
                mustChangePassword  = e.MustChangePassword,
                enrolledAt          = e.EnrolledAt,
                lastLoginAt         = e.LastLoginAt,
                suspendedAt         = e.SuspendedAt,
                suspendReason       = e.SuspendReason
            })
        });
    }

    [Description(
        "Get onboarding KPI metrics for an application. " +
        "Returns duration in seconds, whether the 3-minute target was met, last stage reached, and outcome. " +
        "Use this to track whether digital onboarding meets the ≤3-minute KPI.")]
    public async Task<string> GetOnboardingMetricAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return Err("Invalid application ID format.");

        var metric = await context.OnboardingMetrics
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ApplicationId == id);

        if (metric is null)
            return JsonSerializer.Serialize(new { applicationId, message = "No onboarding metric recorded for this application." });

        return JsonSerializer.Serialize(new
        {
            applicationId,
            metricId         = metric.Id,
            startedAt        = metric.StartedAt,
            completedAt      = metric.CompletedAt,
            durationSeconds  = metric.DurationSeconds,
            metIn3Minutes    = metric.DurationSeconds.HasValue && metric.DurationSeconds.Value <= 180,
            lastStage        = metric.LastStage,
            outcome          = metric.Outcome,
            isSuccess        = metric.IsSuccess,
            failureReason    = metric.FailureReason
        });
    }

    [Description(
        "Get a summary of onboarding KPIs across all applications. " +
        "Returns counts of successful/failed onboardings, average duration, and percentage meeting the 3-minute target.")]
    public async Task<string> GetOnboardingKpiSummaryAsync()
    {
        var metrics = await context.OnboardingMetrics
            .AsNoTracking()
            .ToListAsync();

        if (metrics.Count == 0)
            return JsonSerializer.Serialize(new { message = "No onboarding metrics recorded yet." });

        var completed  = metrics.Where(m => m.DurationSeconds.HasValue).ToList();
        var successes  = metrics.Count(m => m.IsSuccess);
        var met3Min    = completed.Count(m => m.DurationSeconds!.Value <= 180);
        var avgSeconds = completed.Count > 0 ? completed.Average(m => m.DurationSeconds!.Value) : 0;

        return JsonSerializer.Serialize(new
        {
            totalApplications    = metrics.Count,
            successfulOnboardings= successes,
            failedOnboardings    = metrics.Count - successes,
            successRate          = $"{(metrics.Count > 0 ? (double)successes / metrics.Count * 100 : 0):F1}%",
            completedWithTiming  = completed.Count,
            metIn3Minutes        = met3Min,
            percentageMetKpi     = $"{(completed.Count > 0 ? (double)met3Min / completed.Count * 100 : 0):F1}%",
            averageDurationSeconds = Math.Round(avgSeconds, 1),
            averageDurationMinutes = Math.Round(avgSeconds / 60, 2)
        });
    }

    private static string Err(string message) =>
        JsonSerializer.Serialize(new { error = message });
}
