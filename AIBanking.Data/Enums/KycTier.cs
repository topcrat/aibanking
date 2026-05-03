namespace AIBanking.Enums;

/// <summary>CBN-compliant KYC tiers for individual savings accounts.</summary>
public enum KycTier
{
    /// <summary>BVN/NIN + basic bio-data. Low transaction limits.</summary>
    Tier1 = 1,

    /// <summary>Government-issued ID + verified address. Medium transaction limits.</summary>
    Tier2 = 2,

    /// <summary>Full KYC + proof of address. High transaction limits.</summary>
    Tier3 = 3
}
