using AIBanking.AIAgent.Agents;
using AIBanking.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AIBanking.AIAgent.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly IBankingAgentService     _agent;
    private readonly ILogger<AgentController> _logger;

    public AgentController(IBankingAgentService agent, ILogger<AgentController> logger)
    {
        _agent  = agent;
        _logger = logger;
    }

    /// <summary>
    /// Send a message to the banking AI agent.
    /// Omit ConversationId to start a new conversation; the response returns the ID for follow-up turns.
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { Message = "Message cannot be empty." });

        _logger.LogInformation("Agent chat request. ConversationId={Id}", request.ConversationId ?? "new");

        var response = await _agent.ChatAsync(request, ct);
        return Ok(response);
    }

    /// <summary>Clear the conversation history for the given conversation ID.</summary>
    [HttpDelete("conversations/{conversationId}")]
    public IActionResult ClearConversation(string conversationId)
    {
        var removed = _agent.ClearConversation(conversationId);
        if (!removed)
            return NotFound(new { Message = $"Conversation '{conversationId}' not found." });

        _logger.LogInformation("Conversation {Id} cleared.", conversationId);
        return NoContent();
    }
}
