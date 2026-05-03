namespace AIBanking.Enums;

public enum AccountStatus
{
    Draft,             // Application created, no documents yet
    PendingDocuments,  // Some documents still missing
    UnderReview,       // All documents uploaded, extraction done
    Approved,          // Reviewer approved; service processes running
    Active,            // All service processes complete — account is live
    Rework,            // Flagged back to the applicant/staff for corrections
    Rejected
}
