using AIBanking.Enums;

namespace AIBanking.Models;

public class WorkflowItem
{
    public Guid           Id                { get; set; }
    public Guid           DefinitionId      { get; set; }
    public string         Title             { get; set; } = string.Empty;
    public string         Description       { get; set; } = string.Empty;
    public string         SubmittedBy       { get; set; } = string.Empty;
    public WorkflowStatus Status            { get; set; }
    public int            CurrentStageOrder { get; set; }
    public string?        ReviewedBy        { get; set; }
    public string?        Comments          { get; set; }
    public DateTime       CreatedAt         { get; set; }
    public DateTime       UpdatedAt         { get; set; }

    public WorkflowDefinition            Definition      { get; set; } = null!;
    public ICollection<WorkflowApproval> Approvals       { get; set; } = [];
    public FormSubmission?               FormSubmission  { get; set; }
}
