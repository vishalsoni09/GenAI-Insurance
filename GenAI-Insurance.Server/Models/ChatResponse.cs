namespace GenAI_Insurance.Server.Models;

public class ChatResponse
{
    public string Answer { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string? GeneratedSql { get; set; }
    // Optional suggested topic (e.g., "health", "car") returned by server when it proposes a follow-up topic
    public string? SuggestedTopic { get; set; }
}
