namespace GenAI_Insurance.Server.Services;

public class AgentService
{
    private readonly OpenAIService _openAIService;
    private readonly RagService _ragService;
    private readonly DbService _dbService;
    private readonly MemoryService _memoryService;
    private readonly SqlGeneratorService _sqlGeneratorService;
    private readonly SqlAgentService _sqlAgent;

    public AgentService(
        OpenAIService openAIService,
        RagService ragService,
        DbService dbService,
        MemoryService memoryService,
        SqlGeneratorService sqlGeneratorService,
        SqlAgentService sqlAgent)
    {
        _openAIService = openAIService;
        _ragService = ragService;
        _dbService = dbService;
        _memoryService = memoryService;
        _sqlGeneratorService = sqlGeneratorService;
        _sqlAgent = sqlAgent;
    }

    public async Task<Models.ChatResponse> ProcessAsync(Models.ChatRequest request)
    {
        var question = request.Question?.Trim() ?? string.Empty;
        var lower = question.ToLowerInvariant();

        _memoryService.AddMessage(request.SessionId, "User", question);

        string answer = string.Empty;
        string source = string.Empty;
        string? generatedSql = null;

        // Predefined DB queries
        if (lower.Contains("how many banks"))
        {
            var count = await _dbService.GetBankCountAsync();
            answer = $"There are {count} distinct banks in the dataset.";
            source = "Azure SQL";
        }
        else if ((lower.Contains("which banks") && (lower.Contains("car") || lower.Contains("vehicle") || lower.Contains("motor")))
              || lower.Contains("which banks in") )
        {
            answer = await _dbService.GetBanksByCategoryAsync("Car");
            source = "Azure SQL";
        }
        else if (lower.Contains("most popular") || lower.Contains("trend"))
        {
            answer = await _dbService.GetMostPopularCategoryAsync();
            source = "Azure SQL Insight";
        }
        // RAG route
        else if (lower.Contains("policy") || lower.Contains("coverage") || lower.Contains("covered") || lower.Contains("document"))
        {
            var history = _memoryService.GetHistory(request.SessionId);
            var document = _ragService.GetRelevantDocument(question, history);

            // derive current topic from memory to anchor the assistant
            var currentTopic = _memoryService.GetTopic(request.SessionId) ?? (lower.Contains("car") ? "car" : lower.Contains("life") ? "life" : lower.Contains("health") ? "health" : null);
            var systemPrompt = $"You are a helpful loan and insurance assistant. Conversation topic: {currentTopic ?? "insurance"}. Use the provided document context and conversation history. Keep responses on-topic unless the user explicitly asks to change the subject.";

            var userPrompt = $"Conversation history:\n{string.Join("\n", history)}\n\nDocument context:\n{document}\n\nUser question:\n{question}";

            var resp = await _openAIService.GetChatCompletionRawAsync(systemPrompt, userPrompt);
            answer = resp.Choices?.FirstOrDefault()?.Message?.Content ?? "";
            answer = FormatAssistantText(answer);
            source = "RAG + Azure OpenAI";

            // detect suggested topics in answer text (best-effort)
            string? suggested = null;
            var ansLow = (answer ?? string.Empty).ToLowerInvariant();
            if (ansLow.Contains("would you like") || ansLow.Contains("do you want") || ansLow.Contains("would you like to learn"))
            {
                if (ansLow.Contains("health")) suggested = "health";
                else if (ansLow.Contains("car")) suggested = "car";
                else if (ansLow.Contains("life")) suggested = "life";
            }

            // store assistant reply and suggested topic in memory
            _memoryService.AddMessage(request.SessionId, "Assistant", answer, suggested);

            return new Models.ChatResponse
            {
                Answer = answer,
                Source = source,
                Score = string.Empty,
                GeneratedSql = generatedSql,
                SuggestedTopic = suggested
            };
        }
        // Dynamic SQL route
        else if (lower.Contains("show") || lower.Contains("list") || lower.Contains("find") || lower.Contains("display"))
        {
            generatedSql = await _sqlGeneratorService.GenerateSqlAsync(question);
            var dbResult = await _dbService.ExecuteDynamicQueryAsync(generatedSql);
            answer = dbResult;
            source = "Azure OpenAI SQL Generator + Azure SQL";
        }
        else
        {
            // General chat with memory
            var history = _memoryService.GetHistory(request.SessionId);
            var systemPrompt = "You are a helpful AI assistant for a banking and insurance POC. Use conversation history where relevant. Keep responses short and professional.";
            var userPrompt = $"Conversation history:\n{string.Join("\n", history)}\n\nUser question:\n{question}";

            var resp = await _openAIService.GetChatCompletionRawAsync(systemPrompt, userPrompt);
            answer = resp.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
            answer = FormatAssistantText(answer);
            source = "Azure OpenAI";
        }

        // Save assistant reply to memory (non-RAG routes)
        _memoryService.AddMessage(request.SessionId, "Assistant", answer);

        return new Models.ChatResponse
        {
            Answer = answer,
            Source = source,
            Score = string.Empty,
            GeneratedSql = generatedSql
        };
    }

    // Minimal post-processing to make LLM text friendlier in the UI:
    // - Remove common Markdown markers (**bold**, *italic*, `code`, headings like ###)
    // - Insert paragraph breaks after sentence-ending punctuation when followed by a capital letter
    private static string FormatAssistantText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;
        try
        {
            // remove heading markers (#)
            text = System.Text.RegularExpressions.Regex.Replace(text, "^#{1,6}\\s*", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            // remove bold and italic markers
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\*\\*(.*?)\\*\\*", "$1");
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\*(.*?)\\*", "$1");
            // remove inline code ticks
            text = System.Text.RegularExpressions.Regex.Replace(text, "`(.+?)`", "$1");
            // collapse multiple blank lines
            text = System.Text.RegularExpressions.Regex.Replace(text, "\\n{3,}", "\\n\\n");
            // insert paragraph break after sentence end when followed by a capital letter or digit
            text = System.Text.RegularExpressions.Regex.Replace(text, "(?<=[\\.\\?\\!])\\s+(?=[A-Z0-9])", "\\n\\n");
            // trim
            return text.Trim();
        }
        catch
        {
            return text;
        }
    }

    public object Handle(string userMessage)
    {
        // backward compatibility - simple synchronous flow
        var lower = userMessage.ToLowerInvariant();
        if (lower.Contains("bank") || lower.Contains("insurance") || lower.Contains("customers") || lower.Contains("list"))
        {
            try
            {
                var (sql, meta, rows) = _sqlAgent.RunQuestionWithSql(userMessage);
                return new { sql, meta, rows };
            }
            catch (Exception ex)
            {
                var docs = _ragService.Query(userMessage);
                var context = string.Join("\n---\n", docs.Take(3));
                var ai = _openAIService.Reply(userMessage + "\nContext:\n" + context + "\nNote: SQL error: " + ex.Message);
                var text = ai.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
                return new { reply = text };
            }
        }

        var rdocs = _ragService.Query(userMessage);
        var rcontext = string.Join("\n---\n", rdocs.Take(3));
        var rai = _openAIService.Reply(userMessage + "\nContext:\n" + rcontext);
        var rtext = rai.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        return new { reply = rtext };
    }
}
