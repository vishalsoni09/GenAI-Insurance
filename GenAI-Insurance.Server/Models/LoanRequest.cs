namespace GenAI_Insurance.Server.Models;

public class LoanRequest
{
    public Customer Customer { get; set; } = new Customer();
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
}
