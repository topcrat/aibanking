namespace AIBanking.DTOs;

public class ExtractedPersonInfo
{
    public string? FullName         { get; set; }
    public string? DateOfBirth      { get; set; }   // ISO 8601: yyyy-MM-dd
    public string? Gender           { get; set; }
    public string? PhoneNumber      { get; set; }
    public string? ResidenceAddress { get; set; }
    public string? NationalIdNumber { get; set; }   // 11-digit NIN (NIMC)
}
