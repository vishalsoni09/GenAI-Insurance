using LoanEligibility.Poc.Api.Models;

namespace LoanEligibility.Poc.Api.Services;

public class LoanEligibilityService
{
    public LoanResponse Assess(LoanRequest req)
    {
        // Very naive rules for POC
        var resp = new LoanResponse();

        if (req.Customer.CreditScore < 600)
        {
            resp.Eligible = false;
            resp.Reason = "Low credit score";
            return resp;
        }

        var monthlyPayment = CalculateMonthlyPayment(req.Amount, req.TermMonths, 0.01m);
        var dti = monthlyPayment / req.Customer.MonthlyIncome;

        if (dti > 0.4m)
        {
            resp.Eligible = false;
            resp.Reason = "High debt-to-income ratio";
            return resp;
        }

        resp.Eligible = true;
        resp.MonthlyPayment = monthlyPayment;
        resp.Reason = "Eligible";
        return resp;
    }

    private decimal CalculateMonthlyPayment(decimal principal, int months, decimal monthlyRate)
    {
        if (monthlyRate <= 0) return principal / months;
        var ratePow = (decimal)Math.Pow((double)(1 + monthlyRate), months);
        return principal * monthlyRate * ratePow / (ratePow - 1);
    }
}
