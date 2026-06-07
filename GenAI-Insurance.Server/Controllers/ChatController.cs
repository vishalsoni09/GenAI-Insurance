using Microsoft.AspNetCore.Mvc;
using GenAI_Insurance.Server.Models;
using GenAI_Insurance.Server.Services;

namespace GenAI_Insurance.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly AgentService _agentService;

    public ChatController(AgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpPost]
    [HttpPost("Post")]
    public async Task<ActionResult<ChatResponse>> Post([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required.");
        }

        var response = await _agentService.ProcessAsync(request);

        if (string.IsNullOrWhiteSpace(response?.Answer))
        {
            // fallback: try synchronous Handle which may return a reply or OpenAIResponse
            try
            {
                var res = _agentService.Handle(request.Question);
                if (res is GenAI_Insurance.Server.Models.OpenAIResponse openai)
                {
                    response.Answer = openai.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
                    response.Source = "Azure OpenAI";
                }
                else if (res is IDictionary<string, object> dict)
                {
                    response.Answer = System.Text.Json.JsonSerializer.Serialize(dict);
                    response.Source = "agent";
                }
                else
                {
                    response.Answer = res?.GetType().GetProperty("reply")?.GetValue(res)?.ToString() ?? res?.ToString() ?? string.Empty;
                    response.Source = "agent";
                }
            }
            catch
            {
                // ignore and return original response
            }
        }

        return Ok(response);
    }
}
