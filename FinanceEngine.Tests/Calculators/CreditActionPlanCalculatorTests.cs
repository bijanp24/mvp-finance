using FinanceEngine.Calculators;
using FinanceEngine.Models;

namespace FinanceEngine.Tests.Calculators;

public class CreditActionPlanCalculatorTests
{
    private static Debt Card(string name, decimal balance, decimal apr, decimal minPayment) =>
        new(Name: name, Balance: balance, AnnualPercentageRate: apr, MinimumPayment: minPayment);

    #region Input Validation

    [Fact]
    public void Calculate_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CreditActionPlanCalculator.Calculate(null!));
    }

    [Fact]
    public void Calculate_NegativeWindfall_Throws()
    {
        var input = new CreditActionPlanInput(Array.Empty<Debt>(), Windfall: -1m, MonthlyEssentialExpenses: 0m);
        Assert.Throws<ArgumentException>(() => CreditActionPlanCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_NegativeEmergencyMonths_Throws()
    {
        var input = new CreditActionPlanInput(Array.Empty<Debt>(), Windfall: 0m, MonthlyEssentialExpenses: 100m, EmergencyFundMonths: -1);
        Assert.Throws<ArgumentException>(() => CreditActionPlanCalculator.Calculate(input));
    }

    #endregion

    #region Emergency Fund

    [Fact]
    public void Calculate_ReservesEmergencyFundBeforeDebt()
    {
        var debts = new[] { Card("Visa", 5000m, 0.20m, 100m) };
        var input = new CreditActionPlanInput(
            Debts: debts,
            Windfall: 30000m,
            MonthlyEssentialExpenses: 3000m,
            EmergencyFundMonths: 6,
            MonthlyIncome: 0m);

        var result = CreditActionPlanCalculator.Calculate(input);

        // 6 x $3,000 = $18,000 set aside first.
        Assert.Equal(18000m, result.EmergencyFundTarget);
        Assert.Equal(18000m, result.EmergencyFundReserved);
        Assert.True(result.IsEmergencyFundFunded);
        // Remaining $12,000 is available; only $5,000 of debt exists.
        Assert.Equal(5000m, result.WindfallToDebt);
        Assert.Equal(7000m, result.WindfallRemaining);
        Assert.Equal(0m, result.TotalDebtAfter);
    }

    [Fact]
    public void Calculate_WindfallSmallerThanTarget_FundsReserveOnly()
    {
        var debts = new[] { Card("Visa", 5000m, 0.20m, 100m) };
        var input = new CreditActionPlanInput(
            Debts: debts,
            Windfall: 10000m,
            MonthlyEssentialExpenses: 3000m,
            EmergencyFundMonths: 6); // target = 18,000

        var result = CreditActionPlanCalculator.Calculate(input);

        Assert.Equal(10000m, result.EmergencyFundReserved);
        Assert.False(result.IsEmergencyFundFunded);
        Assert.Equal(0m, result.WindfallToDebt);     // nothing left for debt
        Assert.Equal(5000m, result.TotalDebtAfter);  // debt untouched
    }

    #endregion

    #region Allocation Strategy

    [Fact]
    public void Calculate_Avalanche_PaysHighestAprFirst()
    {
        var debts = new[]
        {
            Card("Low APR", 2000m, 0.10m, 50m),
            Card("High APR", 2000m, 0.28m, 50m),
            Card("Mid APR", 2000m, 0.19m, 50m),
        };
        // No emergency fund; $2,000 to allocate - enough for exactly one card.
        var input = new CreditActionPlanInput(
            Debts: debts,
            Windfall: 2000m,
            MonthlyEssentialExpenses: 0m,
            EmergencyFundMonths: 0,
            Strategy: AllocationStrategy.Avalanche);

        var result = CreditActionPlanCalculator.Calculate(input);

        var first = result.Steps[0];
        Assert.Equal("High APR", first.DebtName);
        Assert.Equal(2000m, first.LumpSumApplied);
        Assert.True(first.IsFullyPaid);
        // The other two get nothing.
        Assert.Equal(0m, result.Steps[1].LumpSumApplied);
        Assert.Equal(0m, result.Steps[2].LumpSumApplied);
    }

    [Fact]
    public void Calculate_Snowball_PaysSmallestBalanceFirst()
    {
        var debts = new[]
        {
            Card("Big", 8000m, 0.15m, 100m),
            Card("Small", 1000m, 0.15m, 50m),
            Card("Medium", 3000m, 0.15m, 75m),
        };
        var input = new CreditActionPlanInput(
            Debts: debts,
            Windfall: 1000m,
            MonthlyEssentialExpenses: 0m,
            EmergencyFundMonths: 0,
            Strategy: AllocationStrategy.Snowball);

        var result = CreditActionPlanCalculator.Calculate(input);

        Assert.Equal("Small", result.Steps[0].DebtName);
        Assert.True(result.Steps[0].IsFullyPaid);
    }

    #endregion

    #region Interest Saved

    [Fact]
    public void Calculate_PayingDownBalance_SavesInterest()
    {
        var debts = new[] { Card("Visa", 5000m, 0.24m, 150m) };
        var input = new CreditActionPlanInput(
            Debts: debts,
            Windfall: 2000m,
            MonthlyEssentialExpenses: 0m,
            EmergencyFundMonths: 0);

        var result = CreditActionPlanCalculator.Calculate(input);

        var step = result.Steps[0];
        Assert.Equal(2000m, step.LumpSumApplied);
        Assert.Equal(3000m, step.BalanceAfterLumpSum);
        // Less principal at the same APR => strictly less interest and faster payoff.
        Assert.True(step.InterestAfter < step.InterestBefore);
        Assert.True(step.InterestSaved > 0m);
        Assert.True(result.MonthsToDebtFreeAfter < result.MonthsToDebtFreeBefore);
    }

    [Fact]
    public void Calculate_MinimumBelowInterest_NeverPaysOff()
    {
        // $10,000 at 24% accrues $200/mo interest; a $150 minimum never covers it.
        var debts = new[] { Card("Underwater", 10000m, 0.24m, 150m) };
        var input = new CreditActionPlanInput(
            Debts: debts,
            Windfall: 0m,
            MonthlyEssentialExpenses: 0m,
            EmergencyFundMonths: 0);

        var result = CreditActionPlanCalculator.Calculate(input);

        Assert.Null(result.Steps[0].MonthsToPayoffBefore);
        Assert.Contains(result.Recommendations, r => r.Contains("never pays off"));
    }

    [Fact]
    public void Calculate_NoDebt_RecommendsSavingsAndInvesting()
    {
        var input = new CreditActionPlanInput(
            Debts: Array.Empty<Debt>(),
            Windfall: 10000m,
            MonthlyEssentialExpenses: 1000m,
            EmergencyFundMonths: 3);

        var result = CreditActionPlanCalculator.Calculate(input);

        Assert.Empty(result.Steps);
        Assert.Equal(3000m, result.EmergencyFundReserved);
        Assert.Contains(result.Recommendations, r => r.Contains("no outstanding credit-card debt"));
    }

    #endregion
}
