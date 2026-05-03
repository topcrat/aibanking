using AIBanking.DTOs;
using AIBanking.Enums;

namespace AIBanking.Models;

public class AccountApplication
{
    public Guid                      Id            { get; set; }
    public AccountStatus             Status        { get; set; }

    /// <summary>11-digit Bank Verification Number supplied by the applicant.</summary>
    public string?                   BvnNumber     { get; set; }

    /// <summary>11-digit National Identification Number supplied by the applicant.</summary>
    public string?                   NinNumber     { get; set; }

    /// <summary>True when the applicant has given explicit NDPA data-usage consent.</summary>
    public bool                      ConsentGiven  { get; set; }

    public ExtractedPersonInfo?      ExtractedInfo { get; set; }
    public List<ApplicationDocument> Documents     { get; set; } = [];
    public List<ApplicationProcess>  Processes     { get; set; } = [];

    /// <summary>
    /// Populated when the application is sent back for rework.
    /// Records the specific issues the agent or reviewer identified.
    /// </summary>
    public string? ReworkNotes { get; set; }

    public DateTime                  CreatedAt     { get; set; }
    public DateTime                  UpdatedAt     { get; set; }

    /// <summary>Creates a new application pre-seeded with the required service processes.</summary>
    public static AccountApplication Create()
    {
        var id = Guid.NewGuid();
        return new AccountApplication
        {
            Id        = id,
            Status    = AccountStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Processes =
            [
                new() { Id = Guid.NewGuid(), ApplicationId = id, Name = ServiceProcess.CreateCustomer },
                new() { Id = Guid.NewGuid(), ApplicationId = id, Name = ServiceProcess.CreateAccount  }
            ]
        };
    }

    public bool HasDocument(DocumentType type) =>
        Documents.Any(d => d.Type == type);

    public bool AllDocumentsUploaded() =>
        HasDocument(DocumentType.AccountOpeningForm) &&
        HasDocument(DocumentType.IdentityCard);

    public ApplicationProcess? GetProcess(ServiceProcess name) =>
        Processes.FirstOrDefault(p => p.Name == name);

    public bool IsProcessComplete(ServiceProcess name) =>
        GetProcess(name)?.Status == ServiceProcessStatus.Completed;

    public bool AllProcessesComplete() =>
        Processes.All(p => p.Status == ServiceProcessStatus.Completed);
}
