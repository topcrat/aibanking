namespace AIBanking.DTOs;

public class DocumentMetadata
{
    public Guid     Id          { get; set; }
    public Guid     WorkflowId  { get; set; }
    public string   FileName    { get; set; } = string.Empty;
    public string   ContentType { get; set; } = string.Empty;
    public long     SizeBytes   { get; set; }
    public string   UploadedBy  { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }
}
