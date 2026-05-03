using AIBanking.DTOs;

namespace AIBanking.Agents;

public interface IBankingAgentService
{
    /// <summary>
    /// Send a message to the banking agent and receive a reply.
    /// Conversation history is maintained per <paramref name="request.ConversationId"/>.
    /// Omit <c>ConversationId</c> to start a new conversation; the response will include the new ID.
    /// </summary>
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>Clear the stored conversation history for the given conversation ID.</summary>
    bool ClearConversation(string conversationId);
}
