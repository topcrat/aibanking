using System.ComponentModel;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Agents.Tools;

/// <summary>
/// Action tools that let the agent make review decisions on workflow items:
/// approve, request rework, or decline.
/// </summary>
internal sealed class WorkflowActionTools(BankingDbContext context)
{
    [Description(
        "Approve a workflow item. Sets its status to Approved and records the reviewer name and optional comments. " +
        "Only items currently in Pending or Rework status can be approved.")]
    public async Task<string> ApproveWorkflowAsync(
        [Description("The workflow item ID (GUID string)")] string workflowId,
        [Description("Name or ID of the person or agent performing the review")] string reviewedBy,
        [Description("Optional approval comments")] string? comments = null)
        => await SetWorkflowStatusAsync(workflowId, WorkflowStatus.Approved, reviewedBy, comments,
            allowedFrom: [WorkflowStatus.Pending, WorkflowStatus.Rework]);

    [Description(
        "Request rework on a workflow item, sending it back for corrections. " +
        "Sets status to Rework. Use comments to describe what needs to be fixed. " +
        "Only items in Pending status can be sent for rework.")]
    public async Task<string> RequestWorkflowReworkAsync(
        [Description("The workflow item ID (GUID string)")] string workflowId,
        [Description("Name or ID of the reviewer requesting rework")] string reviewedBy,
        [Description("Description of what needs to be corrected")] string comments)
        => await SetWorkflowStatusAsync(workflowId, WorkflowStatus.Rework, reviewedBy, comments,
            allowedFrom: [WorkflowStatus.Pending]);

    [Description(
        "Decline a workflow item. Sets its status to Declined and records the reason in comments. " +
        "Only items in Pending or Rework status can be declined.")]
    public async Task<string> DeclineWorkflowAsync(
        [Description("The workflow item ID (GUID string)")] string workflowId,
        [Description("Name or ID of the reviewer declining the item")] string reviewedBy,
        [Description("Reason for declining")] string comments)
        => await SetWorkflowStatusAsync(workflowId, WorkflowStatus.Declined, reviewedBy, comments,
            allowedFrom: [WorkflowStatus.Pending, WorkflowStatus.Rework]);

    // ── Shared implementation ────────────────────────────────────────────────

    private async Task<string> SetWorkflowStatusAsync(
        string          workflowId,
        WorkflowStatus  newStatus,
        string          reviewedBy,
        string?         comments,
        WorkflowStatus[] allowedFrom)
    {
        if (!Guid.TryParse(workflowId, out var id))
            return Err("Invalid workflow ID format.");

        if (string.IsNullOrWhiteSpace(reviewedBy))
            return Err("reviewedBy is required.");

        var item = await context.WorkflowItems.FindAsync(id);
        if (item is null) return Err($"Workflow {workflowId} not found.");

        if (!allowedFrom.Contains(item.Status))
            return Err($"Cannot set status to {newStatus} when current status is {item.Status}. " +
                       $"Allowed from: {string.Join(", ", allowedFrom)}.");

        var previous    = item.Status;
        item.Status     = newStatus;
        item.ReviewedBy = reviewedBy;
        item.Comments   = comments;
        item.UpdatedAt  = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return JsonSerializer.Serialize(new
        {
            workflowId     = item.Id,
            title          = item.Title,
            previousStatus = previous.ToString(),
            newStatus      = item.Status.ToString(),
            reviewedBy     = item.ReviewedBy,
            comments       = item.Comments,
            updatedAt      = item.UpdatedAt
        });
    }

    private static string Err(string message) =>
        JsonSerializer.Serialize(new { error = message });
}
