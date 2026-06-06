namespace LoanEligibility.Poc.Api.Models;

public class LoanResponse
{
    public bool Eligible { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal MonthlyPayment { get; set; }
}
