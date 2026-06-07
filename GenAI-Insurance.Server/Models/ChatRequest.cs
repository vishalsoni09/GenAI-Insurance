namespace GenAI_Insurance.Server.Models;

public class ChatRequest
{
    // Session identifier (maps to previous UserId)
    public string SessionId { get; set; } = "default-session";
    // Natural language question or message
    public string Question { get; set; } = string.Empty;
}
