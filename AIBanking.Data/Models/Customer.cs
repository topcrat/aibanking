using AIBanking.Enums;

namespace AIBanking.Models;

public class Customer
{
    public Guid     Id               { get; set; }
    public Guid     ApplicationId    { get; set; }
    public string   FullName         { get; set; } = string.Empty;
    public string?  DateOfBirth      { get; set; }
    public string?  Gender           { get; set; }
    public string?  PhoneNumber      { get; set; }
    public string?  ResidenceAddress { get; set; }
    public string?  NationalIdNumber { get; set; }   // 11-digit NIN
    public string?  BvnNumber        { get; set; }   // 11-digit BVN
    public KycTier  KycTier          { get; set; } = KycTier.Tier1;
    public DateTime CreatedAt        { get; set; }
}
