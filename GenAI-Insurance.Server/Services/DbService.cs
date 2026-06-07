using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Data.SqlClient;

namespace GenAI_Insurance.Server.Services;

public class DbService
{
    private readonly string _connectionString;
    private readonly IConfiguration _config;

    public DbService(IConfiguration configuration)
    {
        _config = configuration;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'Default' not found.");
    }

    public async Task<int> GetBankCountAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = "SELECT COUNT(DISTINCT BankName) FROM InsuranceData;";
        await using var cmd = new SqlCommand(sql, conn);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public async Task<string> GetBanksByCategoryAsync(string category)
    {
        var output = new StringBuilder();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"SELECT BankName, State
FROM InsuranceData
WHERE Category = @Category
ORDER BY BankName;";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Category", category);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            output.AppendLine($"{reader["BankName"]} | {reader["State"]}");
        }

        return output.Length == 0 ? "No records found." : output.ToString();
    }

    public async Task<string> GetMostPopularCategoryAsync()
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        const string sql = @"SELECT TOP 1 Category, COUNT(*) AS Total
FROM InsuranceData
GROUP BY Category
ORDER BY Total DESC;";

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return $"{reader["Category"]} is the most popular category with {reader["Total"]} records.";
        }

        return "No insight data available.";
    }

    public async Task<string> ExecuteDynamicQueryAsync(string sql)
    {
        var validationError = ValidateGeneratedSql(sql);
        if (!string.IsNullOrEmpty(validationError))
        {
            return $"SQL blocked: {validationError}";
        }

        var output = new StringBuilder();
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        if (!reader.HasRows)
        {
            return "No records found.";
        }

        while (await reader.ReadAsync())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                output.Append(reader.GetName(i));
                output.Append(": ");
                output.Append(reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString());
                if (i < reader.FieldCount - 1) output.Append(" | ");
            }
            output.AppendLine();
        }

        return output.ToString();
    }

    private string? ValidateGeneratedSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return "Empty SQL.";

        var cleanSql = sql.Trim();
        var lower = cleanSql.ToLowerInvariant();

        if (!lower.StartsWith("select"))
            return "Only SELECT statements are allowed.";

        if (lower.Contains(";"))
            return "Multiple statements are not allowed.";

        var blockedKeywords = new[] { "insert", "update", "delete", "drop", "alter", "truncate", "exec", "execute", "merge", "create", "grant", "revoke" };
        foreach (var keyword in blockedKeywords)
        {
            if (lower.Contains(keyword))
                return $"Blocked keyword detected: {keyword}";
        }

        var allowed = _config.GetSection("SqlAllowedTables").Get<string[]>() ?? new[] { "insurancedata" };
        var allowedLower = allowed.Select(s => s.ToLowerInvariant()).ToArray();
        if (!allowedLower.Any(t => lower.Contains(t)))
            return "Only InsuranceData table is allowed.";

        // Soft validation: ensure FROM references allowed table
        var fromMatch = Regex.Match(lower, @"from\s+([\[\]a-z0-9_.]+)", RegexOptions.IgnoreCase);
        if (fromMatch.Success)
        {
            var table = fromMatch.Groups[1].Value;
            var normalized = table.Replace("[", "").Replace("]", "").Split('.').Last();
            if (!allowedLower.Contains(normalized))
                return "Invalid table detected.";
        }

        return null;
    }
}
