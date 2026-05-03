namespace AIBanking.DTOs;

public class CustomerResponse
{
    public Guid     Id               { get; set; }
    public Guid     ApplicationId    { get; set; }
    public string   FullName         { get; set; } = string.Empty;
    public string?  DateOfBirth      { get; set; }
    public string?  Gender           { get; set; }
    public string?  PhoneNumber      { get; set; }
    public string?  ResidenceAddress { get; set; }
    public DateTime CreatedAt        { get; set; }
}
