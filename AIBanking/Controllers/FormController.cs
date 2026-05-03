using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using AIBanking.Data;
using AIBanking.Enums;
using AIBanking.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIBanking.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FormController(
    IDbContextFactory<BankingDbContext> dbFactory,
    ILogger<FormController>             logger) : ControllerBase
{
    // ── Form definitions ──────────────────────────────────────────────────────

    /// <summary>List all active form definitions.</summary>
    [HttpGet("definitions")]
    public async Task<IActionResult> ListDefinitions(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var forms = await db.FormDefinitions
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .Include(f => f.WorkflowDefinition)
                .ThenInclude(wd => wd.Stages.OrderBy(s => s.StageOrder))
            .Where(f => f.IsActive)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);
        return Ok(forms);
    }

    /// <summary>Get a single form definition by ID.</summary>
    [HttpGet("definitions/{id:guid}")]
    public async Task<IActionResult> GetDefinition(Guid id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var form = await db.FormDefinitions
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .Include(f => f.WorkflowDefinition)
                .ThenInclude(wd => wd.Stages.OrderBy(s => s.StageOrder))
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (form is null) return NotFound(new { Message = $"Form definition {id} not found." });
        return Ok(form);
    }

    /// <summary>Create a new form definition. Admin only.</summary>
    [HttpPost("definitions")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CreateDefinition(
        [FromBody] CreateFormDefinitionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { Message = "Name is required." });
        if (request.Fields is not { Count: > 0 })
            return BadRequest(new { Message = "At least one field is required." });

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var workflowExists = await db.WorkflowDefinitions
            .AnyAsync(w => w.Id == request.WorkflowDefinitionId && w.IsActive, ct);
        if (!workflowExists)
            return BadRequest(new { Message = $"Workflow definition {request.WorkflowDefinitionId} not found or inactive." });

        var form = new FormDefinition
        {
            Id                   = Guid.NewGuid(),
            Name                 = request.Name.Trim(),
            Description          = request.Description?.Trim() ?? string.Empty,
            WorkflowDefinitionId = request.WorkflowDefinitionId,
            IsActive             = true,
            CreatedAt            = DateTime.UtcNow,
            Fields = request.Fields
                .OrderBy(f => f.FieldOrder)
                .Select((f, i) => new FormFieldDefinition
                {
                    Id               = Guid.NewGuid(),
                    FieldOrder       = i + 1,
                    FieldKey         = f.FieldKey.Trim(),
                    Label            = f.Label.Trim(),
                    FieldType        = f.FieldType,
                    IsRequired       = f.IsRequired,
                    Placeholder      = f.Placeholder?.Trim(),
                    OptionsJson      = f.Options is { Count: > 0 }
                                          ? JsonSerializer.Serialize(f.Options)
                                          : null,
                })
                .ToList()
        };

        db.FormDefinitions.Add(form);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Form definition '{Name}' created with {Count} fields.", form.Name, form.Fields.Count);
        return CreatedAtAction(nameof(GetDefinition), new { id = form.Id }, form);
    }

    // ── Form submissions ──────────────────────────────────────────────────────

    /// <summary>Submit a form. Creates a workflow item automatically.</summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] FormSubmitRequest request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var form = await db.FormDefinitions
            .AsNoTracking()
            .Include(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .Include(f => f.WorkflowDefinition)
                .ThenInclude(wd => wd.Stages.OrderBy(s => s.StageOrder))
            .FirstOrDefaultAsync(f => f.Id == request.FormDefinitionId && f.IsActive, ct);

        if (form is null)
            return BadRequest(new { Message = $"Form definition {request.FormDefinitionId} not found or inactive." });

        // Validate required fields
        var missing = form.Fields
            .Where(f => f.IsRequired && f.FieldType != FormFieldType.File)
            .Where(f => !request.Values.TryGetValue(f.FieldKey, out var v) || string.IsNullOrWhiteSpace(v))
            .Select(f => f.Label)
            .ToList();

        if (missing.Count > 0)
            return BadRequest(new { Message = $"Missing required fields: {string.Join(", ", missing)}." });

        var caller     = CallerName();
        var firstStage = form.WorkflowDefinition.Stages.MinBy(s => s.StageOrder)!;
        var title      = request.Values.TryGetValue("fullName", out var name) && !string.IsNullOrWhiteSpace(name)
                             ? $"{form.Name} — {name}"
                             : form.Name;

        var workflowItem = new WorkflowItem
        {
            Id                = Guid.NewGuid(),
            DefinitionId      = form.WorkflowDefinitionId,
            Title             = title,
            Description       = form.Description,
            SubmittedBy       = caller,
            Status            = WorkflowStatus.Pending,
            CurrentStageOrder = firstStage.StageOrder,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow
        };

        var submission = new FormSubmission
        {
            Id               = Guid.NewGuid(),
            FormDefinitionId = form.Id,
            WorkflowItemId   = workflowItem.Id,
            SubmittedBy      = caller,
            SubmittedAt      = DateTime.UtcNow,
            ValuesJson       = JsonSerializer.Serialize(request.Values),
        };

        db.WorkflowItems.Add(workflowItem);
        db.FormSubmissions.Add(submission);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Form '{Form}' submitted by {User} → workflow {WorkflowId}, stage '{Stage}'.",
            form.Name, caller, workflowItem.Id, firstStage.StageName);

        return CreatedAtAction(nameof(GetSubmission), new { id = submission.Id },
            new { submission, workflowItem });
    }

    /// <summary>Get a form submission by ID.</summary>
    [HttpGet("submissions/{id:guid}")]
    public async Task<IActionResult> GetSubmission(Guid id, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var submission = await db.FormSubmissions
            .AsNoTracking()
            .Include(s => s.FormDefinition)
                .ThenInclude(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .Include(s => s.WorkflowItem)
                .ThenInclude(w => w.Approvals.OrderBy(a => a.ActedAt))
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (submission is null) return NotFound(new { Message = $"Submission {id} not found." });
        return Ok(submission);
    }

    /// <summary>List submissions for a workflow item.</summary>
    [HttpGet("submissions/by-workflow/{workflowItemId:guid}")]
    public async Task<IActionResult> GetByWorkflow(Guid workflowItemId, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var submission = await db.FormSubmissions
            .AsNoTracking()
            .Include(s => s.FormDefinition)
                .ThenInclude(f => f.Fields.OrderBy(ff => ff.FieldOrder))
            .FirstOrDefaultAsync(s => s.WorkflowItemId == workflowItemId, ct);

        if (submission is null) return NotFound(new { Message = "No form submission for this workflow item." });
        return Ok(submission);
    }

    private string CallerName() =>
        User.FindFirstValue(JwtRegisteredClaimNames.Name)
        ?? User.FindFirstValue(ClaimTypes.Name)
        ?? "unknown";
}

// ── Request DTOs ──────────────────────────────────────────────────────────────

public sealed class CreateFormDefinitionRequest
{
    public string  Name                 { get; set; } = string.Empty;
    public string? Description          { get; set; }
    public Guid    WorkflowDefinitionId { get; set; }
    public List<FormFieldRequest> Fields { get; set; } = [];
}

public sealed class FormFieldRequest
{
    public int          FieldOrder  { get; set; }
    public string       FieldKey    { get; set; } = string.Empty;
    public string       Label       { get; set; } = string.Empty;
    public FormFieldType FieldType  { get; set; }
    public bool         IsRequired  { get; set; }
    public string?      Placeholder { get; set; }
    public List<string>? Options    { get; set; }
}

public sealed class FormSubmitRequest
{
    public Guid FormDefinitionId { get; set; }
    public Dictionary<string, string> Values { get; set; } = [];
}
