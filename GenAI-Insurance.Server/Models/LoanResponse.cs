namespace GenAI_Insurance.Server.Models;

public class LoanResponse
{
    public bool Eligible { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal MonthlyPayment { get; set; }
    // Annual interest rate expressed as percentage (e.g. 12.0 for 12%)
    public decimal InterestRatePercent { get; set; }
}
