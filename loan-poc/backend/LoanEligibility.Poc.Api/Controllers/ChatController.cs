using Microsoft.AspNetCore.Mvc;
using LoanEligibility.Poc.Api.Models;
using LoanEligibility.Poc.Api.Services;

namespace LoanEligibility.Poc.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly OpenAIService _openAI;
    private readonly MemoryService _memory;

    public ChatController(OpenAIService openAI, MemoryService memory)
    {
        _openAI = openAI;
        _memory = memory;
    }

    [HttpPost("message")]
    public ActionResult<ChatResponse> Message(ChatRequest req)
    {
        // Very simple echo + store memory
        _memory.Save(req.UserId, req.Message);
        var reply = _openAI.Reply(req.Message);
        return Ok(new ChatResponse { Reply = reply });
    }
}
