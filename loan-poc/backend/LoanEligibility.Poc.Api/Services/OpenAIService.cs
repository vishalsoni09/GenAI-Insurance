namespace LoanEligibility.Poc.Api.Services;

public class OpenAIService
{
    public string Reply(string message)
    {
        // In POC return canned response
        return "This is a simulated AI response to: " + message;
    }
}
