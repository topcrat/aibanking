using AIBanking.Enums;

namespace AIBanking.DTOs;

public class CreateAccountRequest
{
    public AccountType AccountType { get; set; } = AccountType.Savings;
}
