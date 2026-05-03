namespace AIBanking.Enums;

public enum FraudRiskLevel
{
    Low,       // Score 0–25  — proceed normally
    Medium,    // Score 26–50 — proceed with flag, agent monitors
    High,      // Score 51–75 — require human review before approval
    Critical   // Score 76+   — block automatically, escalate to compliance
}
