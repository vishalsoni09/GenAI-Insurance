using Microsoft.AspNetCore.Mvc;
using GenAI_Insurance.Server.Models;
using GenAI_Insurance.Server.Services;

namespace GenAI_Insurance.Server.Controllers;

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
        _memory.Save(req.UserId, req.Message);
        var reply = _openAI.Reply(req.Message);
        return Ok(new ChatResponse { Reply = reply });
    }
}
