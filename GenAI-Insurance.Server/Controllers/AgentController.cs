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
    public ActionResult<Models.ChatResponse> Chat(ChatRequest req)
    {
        var res = _agent.Handle(req.Question);

        // If the handler returned an OpenAIResponse, extract first choice content
        if (res is GenAI_Insurance.Server.Models.OpenAIResponse openai)
        {
            var content = openai.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            return Ok(new Models.ChatResponse { Answer = content, Source = "Azure OpenAI", Score = "" });
        }

        if (res is not null && res is IDictionary<string, object>)
        {
            return Ok(new Models.ChatResponse { Answer = System.Text.Json.JsonSerializer.Serialize(res), Source = "agent", Score = "" });
        }

        var txt = res?.GetType().GetProperty("reply")?.GetValue(res)?.ToString() ?? res?.ToString() ?? "";
        return Ok(new Models.ChatResponse { Answer = txt, Source = "agent", Score = "" });
    }
}
