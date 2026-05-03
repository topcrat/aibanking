using AIBanking.Enums;

namespace AIBanking.DTOs;

public class AccountApplicationResponse
{
    public Guid                   Id            { get; set; }
    public AccountStatus          Status        { get; set; }
    public ExtractedPersonInfo?   ExtractedInfo { get; set; }
    public List<DocumentSummary>  Documents     { get; set; } = [];
    public List<ProcessSummary>   Processes     { get; set; } = [];
    public string?                BvnNumber     { get; set; }
    public string?                NinNumber     { get; set; }
    public bool                   ConsentGiven  { get; set; }
    public string?                ReworkNotes   { get; set; }
    public DateTime               CreatedAt     { get; set; }
    public DateTime               UpdatedAt     { get; set; }
}

public class ProcessSummary
{
    public ServiceProcess       Name        { get; set; }
    public ServiceProcessStatus Status      { get; set; }
    public Guid?                ResultId    { get; set; }
    public DateTime?            CompletedAt { get; set; }
    public string?              Error       { get; set; }
}
