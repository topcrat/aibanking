namespace AIBanking.Enums;

public enum DigitalEnrollmentStatus
{
    Pending,    // Enrollment initiated, credentials not yet activated
    Active,     // Customer can log in
    Suspended,  // Temporarily blocked (fraud, compliance)
    Cancelled
}
