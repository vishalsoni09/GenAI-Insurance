using GenAI_Insurance.Server.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GenAI_Insurance.Server.Services;

public class SqlAgentService
{
    private readonly OpenAIService _openAI;
    private readonly IConfiguration _config;

    public SqlAgentService(OpenAIService openAI, IConfiguration config)
    {
        _openAI = openAI;
        _config = config;
    }

    // Generate SQL using OpenAI (simulated) and execute on Azure SQL, returning results as a serializable list of rows
    public List<Dictionary<string, object?>> RunQuestion(string question)
    {
        var (sql, meta) = _openAI.GenerateSql(question);

        // Basic validation: ensure only allowed tables are referenced
        var allowed = _config.GetSection("SqlAllowedTables").Get<string[]>() ?? Array.Empty<string>();
        var tableOk = allowed.Any(t => sql.IndexOf(t, StringComparison.InvariantCultureIgnoreCase) >= 0);
        if (!tableOk)
        {
            throw new InvalidOperationException("Generated SQL references disallowed tables.");
        }

        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(connStr);
        using var cmd = new SqlCommand(sql, conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();

        var results = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return results;
    }

    // Return both the generated SQL and the executed rows for callers that want the SQL text too
    public (string Sql, Models.OpenAIResponse Metadata, List<Dictionary<string, object?>> Rows) RunQuestionWithSql(string question)
    {
        var (sql, meta) = _openAI.GenerateSql(question);

        // Basic validation: ensure only allowed tables are referenced
        var allowed = _config.GetSection("SqlAllowedTables").Get<string[]>() ?? Array.Empty<string>();
        var tableOk = allowed.Any(t => sql.IndexOf(t, StringComparison.InvariantCultureIgnoreCase) >= 0);
        if (!tableOk)
        {
            throw new InvalidOperationException("Generated SQL references disallowed tables.");
        }

        var connStr = _config.GetConnectionString("DefaultConnection");
        using var conn = new SqlConnection(connStr);
        using var cmd = new SqlCommand(sql, conn);
        conn.Open();
        using var reader = cmd.ExecuteReader();

        var results = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }

        return (sql, meta, results);
    }
}
