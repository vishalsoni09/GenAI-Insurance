using Microsoft.AspNetCore.Mvc;
using GenAI_Insurance.Server.Services;

namespace GenAI_Insurance.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OpenAIController : ControllerBase
{
    private readonly OpenAIService _openAI;
    private readonly RagService _ragService;

    public OpenAIController(OpenAIService openAI, RagService ragService)
    {
        _openAI = openAI;
        _ragService = ragService;
    }

    [HttpGet("deployments")]
    public async Task<IActionResult> GetDeployments()
    {
        var raw = await _openAI.ListDeploymentsRawAsync();
        return Content(raw, "application/json");
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex()
    {
        try
        {
            var count = await _ragService.RebuildIndexAsync();
            return Ok(new { indexed = count });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }
}
