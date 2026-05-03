namespace AIBanking.DTOs;

public class ChatRequest
{
    public string  Message        { get; set; } = string.Empty;

    /// <summary>
    /// Leave null to start a new conversation.
    /// The response will contain the generated ID to use in follow-up messages.
    /// </summary>
    public string? ConversationId { get; set; }
}
