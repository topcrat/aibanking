using AIBanking.Enums;

namespace AIBanking.Models;

public class BankAccount
{
    public Guid        Id            { get; set; }
    public string      AccountNumber { get; set; } = string.Empty;
    public Guid        CustomerId    { get; set; }
    public Guid        ApplicationId { get; set; }
    public AccountType AccountType   { get; set; }
    public KycTier     KycTier       { get; set; } = KycTier.Tier1;

    /// <summary>Single-transaction limit (NGN) enforced by the CBN tier.</summary>
    public decimal     SingleTransactionLimit { get; set; }

    /// <summary>Daily cumulative transaction limit (NGN) enforced by the CBN tier.</summary>
    public decimal     DailyTransactionLimit  { get; set; }

    /// <summary>Maximum account balance allowed (NGN) for this KYC tier.</summary>
    public decimal     MaximumBalance         { get; set; }

    public DateTime    CreatedAt     { get; set; }
}

