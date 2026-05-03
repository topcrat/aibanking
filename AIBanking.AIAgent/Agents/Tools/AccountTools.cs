using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.AIAgent.Agents.Tools;

/// <summary>Kernel tools for account opening application queries.</summary>
internal sealed class AccountTools(BankingDbContext context)
{
    [Description("List account opening applications. Optionally filter by status: Draft, PendingDocuments, UnderReview, Approved, Active, or Rejected.")]
    public async Task<string> GetApplicationsAsync(
        [Description("Optional status filter (Draft, PendingDocuments, UnderReview, Approved, Active, Rejected)")] string? status = null)
    {
        var query = context.AccountApplications
            .Include(a => a.Processes)
            .AsNoTracking()
            .AsQueryable();

        if (status is not null && Enum.TryParse<AccountStatus>(status, ignoreCase: true, out var parsed))
            query = query.Where(a => a.Status == parsed);

        var apps = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

        var result = apps.Select(a => new
        {
            id         = a.Id,
            status     = a.Status.ToString(),
            createdAt  = a.CreatedAt,
            updatedAt  = a.UpdatedAt,
            processes  = a.Processes.Select(p => new { name = p.Name.ToString(), status = p.Status.ToString(), completedAt = p.CompletedAt })
        });

        return JsonSerializer.Serialize(result);
    }

    [Description("Get full details, document list, process checklist, and extracted person information for a specific account application.")]
    public async Task<string> GetApplicationStatusAsync(
        [Description("The application ID (GUID string)")] string applicationId)
    {
        if (!Guid.TryParse(applicationId, out var id))
            return JsonSerializer.Serialize(new { error = "Invalid application ID format." });

        var app = await context.AccountApplications
            .Include(a => a.Documents)
            .Include(a => a.Processes)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app is null)
            return JsonSerializer.Serialize(new { error = $"Application {applicationId} not found." });

        return JsonSerializer.Serialize(new
        {
            id            = app.Id,
            status        = app.Status.ToString(),
            extractedInfo = app.ExtractedInfo,
            documents     = app.Documents.Select(d => new { type = d.Type.ToString(), d.FileName, d.UploadedAt }),
            processes     = app.Processes.Select(p => new { name = p.Name.ToString(), status = p.Status.ToString(), p.ResultId, p.CompletedAt }),
            createdAt     = app.CreatedAt,
            updatedAt     = app.UpdatedAt
        });
    }
}
