using Microsoft.AspNetCore.Mvc;
using GenAI_Insurance.Server.Models;
using GenAI_Insurance.Server.Services;

namespace GenAI_Insurance.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoanController : ControllerBase
{
    private readonly LoanEligibilityService _service;

    public LoanController(LoanEligibilityService service)
    {
        _service = service;
    }

    [HttpPost("assess")]
    public ActionResult<LoanResponse> Assess(LoanRequest request)
    {
        var resp = _service.Assess(request);
        return Ok(resp);
    }
}
