using AIBanking.Enums;

namespace AIBanking.Models;

public class ApplicationProcess
{
    public Guid                 Id            { get; set; }
    public Guid                 ApplicationId { get; set; }   // FK → AccountApplication
    public ServiceProcess       Name          { get; set; }
    public ServiceProcessStatus Status        { get; set; } = ServiceProcessStatus.Pending;

    /// <summary>ID of the entity created by this process (CustomerId or AccountId).</summary>
    public Guid?     ResultId    { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string?   Error       { get; set; }
}
