using AIBanking.Enums;

namespace AIBanking.DTOs;

public class DocumentSummary
{
    public Guid         Id          { get; set; }
    public DocumentType Type        { get; set; }
    public string       FileName    { get; set; } = string.Empty;
    public string       ContentType { get; set; } = string.Empty;
    public DateTime     UploadedAt  { get; set; }
}
