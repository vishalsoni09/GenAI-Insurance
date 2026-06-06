namespace GenAI_Insurance.Server.Models;

public class Customer
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public decimal MonthlyIncome { get; set; }
    public int CreditScore { get; set; }
}
