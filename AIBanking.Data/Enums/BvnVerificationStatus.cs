namespace AIBanking.Enums;

public enum BvnVerificationStatus
{
    Pending,     // Not yet verified
    Verified,    // BVN confirmed valid by provider
    Failed,      // BVN invalid or not found
    Suspicious   // BVN found but data mismatch (name/DOB)
}
