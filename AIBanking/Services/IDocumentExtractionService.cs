using AIBanking.DTOs;
using AIBanking.Enums;

namespace AIBanking.Services;

public interface IDocumentExtractionService
{
    /// <summary>
    /// Extracts person information from a single document using AI vision.
    /// </summary>
    Task<ExtractedPersonInfo> ExtractAsync(
        byte[]       content,
        string       contentType,
        DocumentType documentType,
        CancellationToken ct = default);

    /// <summary>
    /// Merges extraction results from multiple documents into one record.
    /// Identity card values take priority over form values for the same field.
    /// </summary>
    ExtractedPersonInfo Merge(IEnumerable<(DocumentType type, ExtractedPersonInfo info)> extractions);
}
