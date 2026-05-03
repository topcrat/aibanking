namespace AIBanking.Models;

public class WorkflowApproval
{
    public Guid     Id             { get; set; }
    public Guid     WorkflowItemId { get; set; }
    public int      StageOrder     { get; set; }
    public string   StageName      { get; set; } = string.Empty;
    public string   Action         { get; set; } = string.Empty; // Approved | Rework | Declined
    public string   ActedBy        { get; set; } = string.Empty;
    public string?  Comments       { get; set; }
    public DateTime ActedAt        { get; set; }
}
