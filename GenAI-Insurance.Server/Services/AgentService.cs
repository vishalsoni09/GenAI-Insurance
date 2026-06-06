namespace GenAI_Insurance.Server.Services;

public class AgentService
{
    private readonly OpenAIService _openAI;
    private readonly RagService _rag;

    public AgentService(OpenAIService openAI, RagService rag)
    {
        _openAI = openAI;
        _rag = rag;
    }

    public string Handle(string userMessage)
    {
        var docs = _rag.Query(userMessage);
        var context = string.Join("\n---\n", docs.Take(3));
        return _openAI.Reply(userMessage + "\nContext:\n" + context);
    }
}
