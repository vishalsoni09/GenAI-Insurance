using Microsoft.AspNetCore.Mvc;
using GenAI_Insurance.Server.Models;
using GenAI_Insurance.Server.Services;
using System.Data;

namespace GenAI_Insurance.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SqlAgentController : ControllerBase
{
    private readonly SqlAgentService _agent;

    public SqlAgentController(SqlAgentService agent)
    {
        _agent = agent;
    }

    [HttpPost("query")]
    public ActionResult<object> Query(SqlRequest req)
    {
        try
        {
            var (sql, meta, rows) = _agent.RunQuestionWithSql(req.Question);
            return Ok(new { sql, meta, rows });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
