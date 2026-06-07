namespace GenAI_Insurance.Server.Models;

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public System.DateTimeOffset Timestamp { get; set; }
    public string? Topic { get; set; }
}
