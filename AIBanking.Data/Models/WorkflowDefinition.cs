namespace AIBanking.Models;

public class WorkflowDefinition
{
    public Guid   Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool   IsActive    { get; set; } = true;

    public ICollection<WorkflowStageDefinition> Stages { get; set; } = [];
}

public class WorkflowStageDefinition
{
    public Guid   Id           { get; set; }
    public Guid   DefinitionId { get; set; }
    public int    StageOrder   { get; set; }
    public string StageName    { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;

    public WorkflowDefinition Definition { get; set; } = null!;
}
