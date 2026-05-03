using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIBanking.DTOs;
using AIBanking.Enums;

namespace AIBanking.Services;

public class ClaudeDocumentExtractionService : IDocumentExtractionService
{
    private const string ApiUrl          = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const string Model            = "claude-opus-4-6";

    private readonly HttpClient            _http;
    private readonly ILogger<ClaudeDocumentExtractionService> _logger;
    private readonly string                _apiKey;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull
    };

    public ClaudeDocumentExtractionService(
        IHttpClientFactory         httpFactory,
        IConfiguration             configuration,
        ILogger<ClaudeDocumentExtractionService> logger)
    {
        _http   = httpFactory.CreateClient("anthropic");
        _logger = logger;
        _apiKey = configuration["Anthropic:ApiKey"]
                  ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");
    }

    public async Task<ExtractedPersonInfo> ExtractAsync(
        byte[]       content,
        string       contentType,
        DocumentType documentType,
        CancellationToken ct = default)
    {
        var docLabel   = documentType == DocumentType.IdentityCard ? "identity card" : "account opening form";
        var base64Data = Convert.ToBase64String(content);

        var messageContent = BuildMessageContent(base64Data, contentType, docLabel);

        var requestBody = new
        {
            model      = Model,
            max_tokens = 1024,
            messages   = new[]
            {
                new { role = "user", content = messageContent }
            }
        };

        var json    = JsonSerializer.Serialize(requestBody, JsonOpts);
        using var req = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key", _apiKey);
        req.Headers.Add("anthropic-version", AnthropicVersion);

        _logger.LogInformation("Sending {DocType} to Claude for extraction.", documentType);

        using var response = await _http.SendAsync(req, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Claude API error {Status}: {Error}", response.StatusCode, error);
            throw new HttpRequestException($"Claude API returned {response.StatusCode}: {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return ParseResponse(responseJson, documentType);
    }

    public ExtractedPersonInfo Merge(IEnumerable<(DocumentType type, ExtractedPersonInfo info)> extractions)
    {
        // Collect results; IdentityCard has higher priority for each field
        var list   = extractions.ToList();
        var form   = list.FirstOrDefault(x => x.type == DocumentType.AccountOpeningForm).info;
        var idCard = list.FirstOrDefault(x => x.type == DocumentType.IdentityCard).info;

        return new ExtractedPersonInfo
        {
            FullName         = idCard?.FullName         ?? form?.FullName,
            DateOfBirth      = idCard?.DateOfBirth      ?? form?.DateOfBirth,
            Gender           = idCard?.Gender           ?? form?.Gender,
            PhoneNumber      = form?.PhoneNumber        ?? idCard?.PhoneNumber,   // form likely has phone
            ResidenceAddress = form?.ResidenceAddress   ?? idCard?.ResidenceAddress,
            NationalIdNumber = idCard?.NationalIdNumber ?? form?.NationalIdNumber  // NIN on ID card
        };
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private static object[] BuildMessageContent(string base64Data, string contentType, string docLabel)
    {
        var prompt =
            $"This is a bank customer's {docLabel}. " +
            "Extract the following fields and return ONLY a valid JSON object with no extra text:\n" +
            "{\n" +
            "  \"fullName\": \"<full name or null>\",\n" +
            "  \"dateOfBirth\": \"<yyyy-MM-dd or null>\",\n" +
            "  \"gender\": \"<Male|Female|Other or null>\",\n" +
            "  \"phoneNumber\": \"<phone number or null>\",\n" +
            "  \"residenceAddress\": \"<full address or null>\",\n" +
            "  \"nationalIdNumber\": \"<11-digit NIN from NIMC or null>\"\n" +
            "}\n" +
            "NIN is an 11-digit number typically printed on Nigerian ID cards as 'NIN' or 'National Identification Number'. " +
            "Use null for any field you cannot find. Do not add any commentary.";

        // PDFs use the 'document' source type; images use 'image'
        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return
            [
                new
                {
                    type   = "document",
                    source = new { type = "base64", media_type = contentType, data = base64Data }
                },
                new { type = "text", text = prompt }
            ];
        }

        return
        [
            new
            {
                type   = "image",
                source = new { type = "base64", media_type = contentType, data = base64Data }
            },
            new { type = "text", text = prompt }
        ];
    }

    private ExtractedPersonInfo ParseResponse(string responseJson, DocumentType documentType)
    {
        try
        {
            using var doc  = JsonDocument.Parse(responseJson);
            var text = doc.RootElement
                          .GetProperty("content")[0]
                          .GetProperty("text")
                          .GetString() ?? string.Empty;

            // Strip any markdown code fences Claude may have added
            text = text.Trim();
            if (text.StartsWith("```")) text = text[(text.IndexOf('\n') + 1)..];
            if (text.EndsWith("```"))   text = text[..text.LastIndexOf("```")].TrimEnd();

            var extracted = JsonSerializer.Deserialize<ExtractedPersonInfoRaw>(text, JsonOpts);

            return new ExtractedPersonInfo
            {
                FullName         = NullIfEmpty(extracted?.FullName),
                DateOfBirth      = NullIfEmpty(extracted?.DateOfBirth),
                Gender           = NullIfEmpty(extracted?.Gender),
                PhoneNumber      = NullIfEmpty(extracted?.PhoneNumber),
                ResidenceAddress = NullIfEmpty(extracted?.ResidenceAddress),
                NationalIdNumber = NullIfEmpty(extracted?.NationalIdNumber)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response for {DocType}.", documentType);
            return new ExtractedPersonInfo();
        }
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "null" ? null : value;

    // Matches the JSON keys Claude returns
    private sealed class ExtractedPersonInfoRaw
    {
        public string? FullName         { get; set; }
        public string? DateOfBirth      { get; set; }
        public string? Gender           { get; set; }
        public string? PhoneNumber      { get; set; }
        public string? ResidenceAddress { get; set; }
        public string? NationalIdNumber { get; set; }
    }
}
