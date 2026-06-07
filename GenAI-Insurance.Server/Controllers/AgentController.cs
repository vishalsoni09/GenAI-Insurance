using Microsoft.AspNetCore.Mvc;
using GenAI_Insurance.Server.Services;
using GenAI_Insurance.Server.Models;

namespace GenAI_Insurance.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AgentController : ControllerBase
{
    private readonly AgentService _agent;

    public AgentController(AgentService agent)
    {
        _agent = agent;
    }

    [HttpPost("handle")]
    public ActionResult<object> Handle(SqlRequest req)
    {
        var res = _agent.Handle(req.Question);
        return Ok(res);
    }

    [HttpPost("chat")]
    public async Task<ActionResult<Models.ChatResponse>> Chat(ChatRequest req)
    {
        // Use the async ProcessAsync path so DB-backed queries (e.g. bank counts) are executed
        var resp = await _agent.ProcessAsync(req);
        return Ok(resp);
    }
}
