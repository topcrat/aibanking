namespace AIBanking.Models;

public sealed class User
{
    public Guid      Id           { get; set; }
    public string    Username     { get; set; } = string.Empty;
    public string    PasswordHash { get; set; } = string.Empty;
    public string    FullName     { get; set; } = string.Empty;
    public string    Role         { get; set; } = UserRoles.Staff;   // Admin | Staff | Viewer
    public bool      IsActive     { get; set; } = true;
    public DateTime  CreatedAt    { get; set; }
    public DateTime? LastLoginAt  { get; set; }
}

public static class UserRoles
{
    public const string Admin       = "Admin";
    public const string Staff       = "Staff";
    public const string Viewer      = "Viewer";
    public const string Teller      = "Teller";
    public const string CPC         = "CPC";
    public const string TeamLeadCPC = "TeamLeadCPC";
    public const string Compliance      = "Compliance";
    public const string CreditAnalyst  = "CreditAnalyst";
    public const string TeamLeadCredit = "TeamLeadCredit";

    public static readonly string[] All =
        [Admin, Staff, Viewer, Teller, CPC, TeamLeadCPC, Compliance, CreditAnalyst, TeamLeadCredit];
}
