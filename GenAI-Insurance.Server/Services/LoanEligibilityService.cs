using GenAI_Insurance.Server.Models;

namespace GenAI_Insurance.Server.Services;

public class LoanEligibilityService
{
    public LoanResponse Assess(LoanRequest req)
    {
        var resp = new LoanResponse();

        if (req.Customer.CreditScore < 600)
        {
            resp.Eligible = false;
            resp.Reason = "Low credit score";
            return resp;
        }

        // Use an APR based on credit score or fixed for simplicity. Here we map credit score to APR bands.
        decimal apr = 0.12m; // default 12% APR
        if (req.Customer.CreditScore >= 750) apr = 0.075m; // 7.5%
        else if (req.Customer.CreditScore >= 700) apr = 0.09m; // 9%
        else if (req.Customer.CreditScore >= 650) apr = 0.10m; // 10%
        // convert APR to monthly rate
        var monthlyRate = apr / 12m;

        var monthlyPayment = CalculateMonthlyPayment(req.Amount, req.TermMonths, monthlyRate);
        var dti = monthlyPayment / req.Customer.MonthlyIncome;

        if (dti > 0.4m)
        {
            resp.Eligible = false;
            resp.Reason = "High debt-to-income ratio";
            return resp;
        }

        resp.Eligible = true;
        resp.MonthlyPayment = monthlyPayment;
        resp.InterestRatePercent = apr * 100m;
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
