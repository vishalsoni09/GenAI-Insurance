using Microsoft.AspNetCore.Mvc;
using LoanEligibility.Poc.Api.Models;
using LoanEligibility.Poc.Api.Services;

namespace LoanEligibility.Poc.Api.Controllers;

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
