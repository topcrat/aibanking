namespace AIBanking.DTOs;

public class SubmitWorkflowRequest
{
    public Guid   DefinitionId { get; set; }
    public string Title        { get; set; } = string.Empty;
    public string Description  { get; set; } = string.Empty;
}
