namespace GenAI_Insurance.Server.Models;

public class ChatRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
