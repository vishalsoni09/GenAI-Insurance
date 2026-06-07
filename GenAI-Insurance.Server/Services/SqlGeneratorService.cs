using System.Linq;
using System.Threading.Tasks;
using GenAI_Insurance.Server.Services;
using GenAI_Insurance.Server.Models;

namespace GenAI_Insurance.Server.Services;

public class SqlGeneratorService
{
    private readonly OpenAIService _openAIService;

    public SqlGeneratorService(OpenAIService openAIService)
    {
        _openAIService = openAIService;
    }

    public async Task<string> GenerateSqlAsync(string question)
    {
        var systemPrompt = @"You are a strict SQL generator.
Generate only SQL Server SELECT statements.
Do not generate INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE, EXEC, MERGE, or multiple statements.
Return only raw SQL.";

        var userPrompt = $"""
Database schema:
Table: InsuranceData
Columns:
- Id (int)
- BankName (nvarchar)
- Category (nvarchar)
- State (nvarchar)
- Coverage (nvarchar)

Rules:
- SQL Server syntax only
- Use only InsuranceData table
- Only one SELECT query
- No comments
- No markdown
- Return only SQL text

User question:
{question}
""";

        var result = await _openAIService.GetChatCompletionRawAsync(systemPrompt, userPrompt);
        var sql = result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        return sql.Trim();
    }
}
