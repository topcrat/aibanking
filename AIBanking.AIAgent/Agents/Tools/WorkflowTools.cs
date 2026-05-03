using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.AIAgent.Agents.Tools;

/// <summary>Kernel tools for workflow approval queries.</summary>
internal sealed class WorkflowTools(BankingDbContext context)
{
    [Description("List workflow approval items. Optionally filter by status: Pending, Approved, Rework, or Declined.")]
    public async Task<string> GetWorkflowsAsync(
        [Description("Optional status filter (Pending, Approved, Rework, Declined)")] string? status = null)
    {
        var query = context.WorkflowItems.AsNoTracking().AsQueryable();

        if (status is not null && Enum.TryParse<WorkflowStatus>(status, ignoreCase: true, out var parsed))
            query = query.Where(w => w.Status == parsed);

        var items = await query.OrderByDescending(w => w.CreatedAt).ToListAsync();

        var result = items.Select(w => new
        {
            id          = w.Id,
            title       = w.Title,
            submittedBy = w.SubmittedBy,
            status      = w.Status.ToString(),
            reviewedBy  = w.ReviewedBy,
            createdAt   = w.CreatedAt,
            updatedAt   = w.UpdatedAt
        });

        return JsonSerializer.Serialize(result);
    }

    [Description("Get full details of a specific workflow approval item, including comments and reviewer information.")]
    public async Task<string> GetWorkflowStatusAsync(
        [Description("The workflow ID (GUID string)")] string workflowId)
    {
        if (!Guid.TryParse(workflowId, out var id))
            return JsonSerializer.Serialize(new { error = "Invalid workflow ID format." });

        var item = await context.WorkflowItems.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id);

        if (item is null)
            return JsonSerializer.Serialize(new { error = $"Workflow {workflowId} not found." });

        return JsonSerializer.Serialize(new
        {
            id          = item.Id,
            title       = item.Title,
            description = item.Description,
            submittedBy = item.SubmittedBy,
            status      = item.Status.ToString(),
            reviewedBy  = item.ReviewedBy,
            comments    = item.Comments,
            createdAt   = item.CreatedAt,
            updatedAt   = item.UpdatedAt
        });
    }

    [Description("Count workflow items grouped by status to give a summary overview.")]
    public async Task<string> GetWorkflowSummaryAsync()
    {
        var counts = await context.WorkflowItems
            .AsNoTracking()
            .GroupBy(w => w.Status)
            .Select(g => new { status = g.Key.ToString(), count = g.Count() })
            .ToListAsync();

        return JsonSerializer.Serialize(counts);
    }
}
