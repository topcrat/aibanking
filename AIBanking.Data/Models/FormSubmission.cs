namespace AIBanking.Models;

public class FormSubmission
{
    public Guid     Id               { get; set; }
    public Guid     FormDefinitionId { get; set; }
    public Guid     WorkflowItemId   { get; set; }
    public string   SubmittedBy      { get; set; } = string.Empty;
    public DateTime SubmittedAt      { get; set; }

    // Field values stored as JSON: { "fieldKey": "value", ... }
    public string ValuesJson { get; set; } = "{}";

    public FormDefinition FormDefinition { get; set; } = null!;
    public WorkflowItem   WorkflowItem   { get; set; } = null!;
}
