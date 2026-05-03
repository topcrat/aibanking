using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AIBanking.Data;
using AIBanking.DTOs;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkflowController(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<WorkflowController>         logger) : ControllerBase
{
    // ── Pipeline definitions (Admin) ──────────────────────────────────────────

    /// <summary>List all workflow definitions.</summary>
    [HttpGet("definitions")]
    public async Task<IActionResult> ListDefinitions(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var defs = await db.WorkflowDefinitions
            .AsNoTracking()
            .Include(d => d.Stages.OrderBy(s => s.StageOrder))
            .OrderBy(d => d.Name)
            .ToListAsync(ct);
        return Ok(defs);
    }

    /// <summary>Create a new workflow definition. Admin only.</summary>
    [HttpPost("definitions")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CreateDefinition(
        [FromBody] CreateWorkflowDefinitionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "Name is required." });
        if (request.Stages is not { Count: > 0 })
            return BadRequest(new { Message = "At least one stage is required." });

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var def = new WorkflowDefinition
        {
            Id          = Guid.NewGuid(),
            Name        = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive    = true,
            Stages      = request.Stages
                .OrderBy(s => s.StageOrder)
                .Select((s, i) => new WorkflowStageDefinition
                {
                    Id           = Guid.NewGuid(),
                    StageOrder   = i + 1,
                    StageName    = s.StageName.Trim(),
                    RequiredRole = s.RequiredRole.Trim(),
                })
                .ToList()
        };

        db.WorkflowDefinitions.Add(def);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Workflow definition '{Name}' created with {Count} stages.", def.Name, def.Stages.Count);
        return CreatedAtAction(nameof(ListDefinitions), new { }, def);
    }

    // ── Submit ────────────────────────────────────────────────────────────────

    /// <summary>Submit a new workflow item against a definition.</summary>
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitWorkflowRequest request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var definition = await db.WorkflowDefinitions
            .AsNoTracking()
            .Include(d => d.Stages.OrderBy(s => s.StageOrder))
            .FirstOrDefaultAsync(d => d.Id == request.DefinitionId && d.IsActive, ct);

        if (definition is null)
            return BadRequest(new { Message = $"Workflow definition {request.DefinitionId} not found or inactive." });

        var firstStage = definition.Stages.MinBy(s => s.StageOrder)!;

        var item = new WorkflowItem
        {
            Id                = Guid.NewGuid(),
            DefinitionId      = definition.Id,
            Title             = request.Title,
            Description       = request.Description,
            SubmittedBy       = CallerName(),
            Status            = WorkflowStatus.Pending,
            CurrentStageOrder = firstStage.StageOrder,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow
        };

        db.WorkflowItems.Add(item);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Workflow {Id} ('{Title}') submitted by {User}, first stage: {Stage}.",
            item.Id, item.Title, item.SubmittedBy, firstStage.StageName);

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    // ── Resubmit ──────────────────────────────────────────────────────────────

    /// <summary>Resubmit a workflow that was returned for rework.</summary>
    [HttpPost("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(Guid id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var item = await db.WorkflowItems.FindAsync([id], ct);
        if (item is null) return NotFound(new { Message = $"Workflow {id} not found." });

        if (item.Status != WorkflowStatus.Rework)
            return BadRequest(new { Message = "Only workflows in Rework status can be resubmitted." });

        var caller = CallerName();
        if (item.SubmittedBy != caller && !User.IsInRole(UserRoles.Admin))
            return Forbid();

        item.Status    = WorkflowStatus.Pending;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Workflow {Id} resubmitted by {User} at stage order {Order}.", id, caller, item.CurrentStageOrder);
        return Ok(item);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>List workflow items, optionally filtered by status.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] WorkflowStatus? status = null,
        CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var query = db.WorkflowItems
            .AsNoTracking()
            .Include(w => w.Definition)
                .ThenInclude(d => d.Stages.OrderBy(s => s.StageOrder))
            .Include(w => w.FormSubmission)
                .ThenInclude(s => s!.FormDefinition)
                    .ThenInclude(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .AsQueryable();

        if (status.HasValue) query = query.Where(w => w.Status == status.Value);

        var items = await query.OrderByDescending(w => w.CreatedAt).ToListAsync(ct);
        return Ok(items);
    }

    /// <summary>Get a single workflow item including its approval history.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var item = await db.WorkflowItems
            .AsNoTracking()
            .Include(w => w.Definition)
                .ThenInclude(d => d.Stages.OrderBy(s => s.StageOrder))
            .Include(w => w.Approvals.OrderBy(a => a.ActedAt))
            .Include(w => w.FormSubmission)
                .ThenInclude(s => s!.FormDefinition)
                    .ThenInclude(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (item is null) return NotFound(new { Message = $"Workflow {id} not found." });
        return Ok(item);
    }

    // ── Approval actions ──────────────────────────────────────────────────────

    /// <summary>Approve the current stage. Advances to the next stage or finalises.</summary>
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] WorkflowActionRequest request, CancellationToken ct)
        => await ActAsync(id, "Approved", request.Comments, ct);

    /// <summary>Send the workflow back to the previous stage for rework.</summary>
    [HttpPost("{id:guid}/rework")]
    public async Task<IActionResult> Rework(Guid id, [FromBody] WorkflowActionRequest request, CancellationToken ct)
        => await ActAsync(id, "Rework", request.Comments, ct);

    /// <summary>Decline the workflow (terminal).</summary>
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] WorkflowActionRequest request, CancellationToken ct)
        => await ActAsync(id, "Declined", request.Comments, ct);

    // ── Document management ───────────────────────────────────────────────────

    /// <summary>Upload a document and attach it to a workflow.</summary>
    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> UploadDocument(Guid id, IFormFile file, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.WorkflowItems.AnyAsync(w => w.Id == id, ct);
        if (!exists) return NotFound(new { Message = $"Workflow {id} not found." });

        if (file is null || file.Length == 0)
            return BadRequest(new { Message = "No file provided." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var doc = new WorkflowDocument
        {
            Id          = Guid.NewGuid(),
            WorkflowId  = id,
            FileName    = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            Content     = ms.ToArray(),
            UploadedBy  = CallerName(),
            UploadedAt  = DateTime.UtcNow
        };

        db.WorkflowDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocId} ({FileName}) attached to workflow {WorkflowId} by {User}.",
            doc.Id, doc.FileName, id, doc.UploadedBy);

        return CreatedAtAction(nameof(GetDocument), new { id, documentId = doc.Id }, doc.ToMetadata());
    }

    /// <summary>List all documents attached to a workflow.</summary>
    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> ListDocuments(Guid id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.WorkflowItems.AnyAsync(w => w.Id == id, ct);
        if (!exists) return NotFound(new { Message = $"Workflow {id} not found." });

        var docs = await db.WorkflowDocuments
            .AsNoTracking()
            .Where(d => d.WorkflowId == id)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync(ct);

        return Ok(docs.Select(d => d.ToMetadata()));
    }

    /// <summary>Get document metadata by document ID.</summary>
    [HttpGet("{id:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> GetDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.WorkflowDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WorkflowId == id, ct);

        if (doc is null)
            return NotFound(new { Message = $"Document {documentId} not found on workflow {id}." });

        return Ok(doc.ToMetadata());
    }

    /// <summary>Download the raw file content of a document.</summary>
    [HttpGet("{id:guid}/documents/{documentId:guid}/download")]
    public async Task<IActionResult> DownloadDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.WorkflowDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WorkflowId == id, ct);

        if (doc is null)
            return NotFound(new { Message = $"Document {documentId} not found on workflow {id}." });

        return File(doc.Content, doc.ContentType, doc.FileName);
    }

    /// <summary>Delete a document from a workflow. Admin only.</summary>
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var doc = await db.WorkflowDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.WorkflowId == id, ct);

        if (doc is null)
            return NotFound(new { Message = $"Document {documentId} not found on workflow {id}." });

        db.WorkflowDocuments.Remove(doc);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocId} ({FileName}) removed from workflow {WorkflowId}.",
            documentId, doc.FileName, id);

        return NoContent();
    }

    // ── Core state machine ────────────────────────────────────────────────────

    private async Task<IActionResult> ActAsync(
        Guid id, string action, string? comments, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var item = await db.WorkflowItems
            .Include(w => w.Definition)
                .ThenInclude(d => d.Stages.OrderBy(s => s.StageOrder))
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (item is null) return NotFound(new { Message = $"Workflow {id} not found." });

        if (item.Status != WorkflowStatus.Pending)
            return BadRequest(new { Message = $"Workflow is {item.Status} and cannot be acted on." });

        var stages     = item.Definition.Stages.OrderBy(s => s.StageOrder).ToList();
        var current    = stages.FirstOrDefault(s => s.StageOrder == item.CurrentStageOrder);

        if (current is null)
            return BadRequest(new { Message = "Current stage not found in definition." });

        // Check caller's role matches the current stage (Admin bypasses)
        if (!User.IsInRole(UserRoles.Admin) && !User.IsInRole(current.RequiredRole))
            return BadRequest(new { Message = $"Stage '{current.StageName}' requires role '{current.RequiredRole}'." });

        var caller = CallerName();

        // Record approval history
        db.WorkflowApprovals.Add(new WorkflowApproval
        {
            Id             = Guid.NewGuid(),
            WorkflowItemId = item.Id,
            StageOrder     = current.StageOrder,
            StageName      = current.StageName,
            Action         = action,
            ActedBy        = caller,
            Comments       = comments,
            ActedAt        = DateTime.UtcNow
        });

        item.ReviewedBy = caller;
        item.Comments   = comments;
        item.UpdatedAt  = DateTime.UtcNow;

        switch (action)
        {
            case "Approved":
                var next = stages.FirstOrDefault(s => s.StageOrder > current.StageOrder);
                if (next is null)
                {
                    item.Status = WorkflowStatus.Approved;
                    logger.LogInformation("Workflow {Id} fully approved by {User}.", id, caller);
                }
                else
                {
                    item.CurrentStageOrder = next.StageOrder;
                    logger.LogInformation("Workflow {Id} advanced to '{Stage}' by {User}.", id, next.StageName, caller);
                }
                break;

            case "Rework":
                var prev = stages.LastOrDefault(s => s.StageOrder < current.StageOrder);
                item.CurrentStageOrder = prev?.StageOrder ?? current.StageOrder;
                item.Status            = WorkflowStatus.Rework;
                logger.LogInformation("Workflow {Id} sent for rework by {User}, back to '{Stage}'.",
                    id, caller, prev?.StageName ?? current.StageName);
                break;

            case "Declined":
                item.Status = WorkflowStatus.Declined;
                logger.LogInformation("Workflow {Id} declined by {User}.", id, caller);
                break;
        }

        await db.SaveChangesAsync(ct);
        return Ok(item);
    }

    private string CallerName() =>
        User.FindFirstValue(JwtRegisteredClaimNames.Name)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? "unknown";
}
