using AIBanking.Enums;

namespace AIBanking.DTOs;

public class BankAccountResponse
{
    public Guid        Id            { get; set; }
    public string      AccountNumber { get; set; } = string.Empty;
    public Guid        CustomerId    { get; set; }
    public Guid        ApplicationId { get; set; }
    public AccountType AccountType   { get; set; }
    public DateTime    CreatedAt     { get; set; }
}
