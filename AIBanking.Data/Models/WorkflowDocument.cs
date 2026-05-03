using AIBanking.DTOs;

namespace AIBanking.Models;

public class WorkflowDocument
{
    public Guid     Id          { get; set; }
    public Guid     WorkflowId  { get; set; }
    public string   FileName    { get; set; } = string.Empty;
    public string   ContentType { get; set; } = string.Empty;
    public byte[]   Content     { get; set; } = [];
    public string   UploadedBy  { get; set; } = string.Empty;
    public DateTime UploadedAt  { get; set; }

    public DocumentMetadata ToMetadata() => new()
    {
        Id          = Id,
        WorkflowId  = WorkflowId,
        FileName    = FileName,
        ContentType = ContentType,
        SizeBytes   = Content.Length,
        UploadedBy  = UploadedBy,
        UploadedAt  = UploadedAt
    };
}
