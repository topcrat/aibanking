using AIBanking.Enums;

namespace AIBanking.Models;

public class ApplicationDocument
{
    public Guid         Id            { get; set; }
    public Guid         ApplicationId { get; set; }
    public DocumentType Type          { get; set; }
    public string       FileName      { get; set; } = string.Empty;
    public string       ContentType   { get; set; } = string.Empty;
    public byte[]       Content       { get; set; } = [];
    public DateTime     UploadedAt    { get; set; }
}
