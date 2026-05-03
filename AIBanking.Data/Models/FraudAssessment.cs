using AIBanking.Enums;

namespace AIBanking.Models;

public class FraudAssessment
{
    public Guid          Id            { get; set; }
    public Guid          ApplicationId { get; set; }

    public int           RiskScore     { get; set; }   // 0–100
    public FraudRiskLevel RiskLevel    { get; set; }

    /// <summary>JSON array of triggered rule names and their individual scores.</summary>
    public string        Flags         { get; set; } = "[]";

    public string?       ReviewedBy    { get; set; }
    public string?       ReviewNotes   { get; set; }

    /// <summary>Cleared, Blocked, or EscalatedToCompliance.</summary>
    public string?       Outcome       { get; set; }

    public DateTime      AssessedAt    { get; set; }
    public DateTime?     ReviewedAt    { get; set; }
}
