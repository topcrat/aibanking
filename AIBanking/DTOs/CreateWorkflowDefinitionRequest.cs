namespace AIBanking.DTOs;

public class CreateWorkflowDefinitionRequest
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<StageRequest> Stages { get; set; } = [];
}

public class StageRequest
{
    public int    StageOrder   { get; set; }
    public string StageName    { get; set; } = string.Empty;
    public string RequiredRole { get; set; } = string.Empty;
}
