using AppointmentManager.Api.Agent;
using AppointmentManager.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentManager.Api.Controllers;

[ApiController]
[Route("api/chat")]
public class ChatController(AgentOrchestrator orchestrator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Send([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var (sessionId, reply) = await orchestrator.HandleMessageAsync(request.SessionId, request.Message, ct);
        return Ok(new ChatResponse(sessionId, reply));
    }
}
