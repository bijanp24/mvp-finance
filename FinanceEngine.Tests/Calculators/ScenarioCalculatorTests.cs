using FinanceEngine.Calculators;

namespace FinanceEngine.Tests.Calculators;

public class ScenarioCalculatorTests
{
    private static ScenarioInput CreateBaseInput() => new(
        TotalCash: 5000,
        TotalDebt: 10000,
        TotalInvestments: 20000,
        CurrentSafeToSpend: 1500,
        UpcomingBills: 2000,
        MonthlyIncome: 6000,
        BaseMonthlyExpenses: 3000,
        BaseDiscretionarySpending: 500,
        BaseDebtPayment: 300,
        BaseInvestmentContribution: 400,
        WeightedDebtApr: 0.18m,
        ExpectedInvestmentReturn: 0.07m,
        MonthlyDiscretionary: 500,
        ExtraDebtPayment: 0,
        ExtraInvestmentContribution: 0
    );

    [Fact]
    public void Calculate_WithBaselineSliders_ReturnsSameAsBaseline()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(input.CurrentSafeToSpend, result.AdjustedSafeToSpend);
        Assert.Equal(0, result.SliderSummary.TotalMonthlyChange);
    }

    [Fact]
    public void Calculate_WithExtraDebtPayment_ReducesSafeToSpend()
    {
        var input = CreateBaseInput() with { ExtraDebtPayment = 200 };

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(1300, result.AdjustedSafeToSpend); // 1500 - 200
        Assert.Equal(200, result.SliderSummary.TotalMonthlyChange);
    }

    [Fact]
    public void Calculate_WithExtraInvestment_ReducesSafeToSpend()
    {
        var input = CreateBaseInput() with { ExtraInvestmentContribution = 100 };

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(1400, result.AdjustedSafeToSpend); // 1500 - 100
        Assert.Equal(100, result.SliderSummary.TotalMonthlyChange);
    }

    [Fact]
    public void Calculate_WithReducedDiscretionary_IncreasesSafeToSpend()
    {
        var input = CreateBaseInput() with { MonthlyDiscretionary = 300 }; // Reduced from 500

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(1700, result.AdjustedSafeToSpend); // 1500 + 200
        Assert.Equal(-200, result.SliderSummary.TotalMonthlyChange);
    }

    [Fact]
    public void Calculate_WithCombinedSliders_CalculatesCorrectly()
    {
        var input = CreateBaseInput() with
        {
            MonthlyDiscretionary = 400,  // -100 from baseline
            ExtraDebtPayment = 100,       // +100
            ExtraInvestmentContribution = 50  // +50
        };

        var result = ScenarioCalculator.Calculate(input);

        // Net change: -100 + 100 + 50 = +50
        Assert.Equal(1450, result.AdjustedSafeToSpend); // 1500 - 50
        Assert.Equal(50, result.SliderSummary.TotalMonthlyChange);
    }

    [Fact]
    public void Calculate_DebtProjection_CalculatesMonthsToPayoff()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        Assert.NotNull(result.DebtProjection.MonthsToPayoff);
        Assert.True(result.DebtProjection.MonthsToPayoff > 0);
        Assert.NotNull(result.DebtProjection.FinalPayoffDate);
    }

    [Fact]
    public void Calculate_WithExtraDebtPayment_ReducesPayoffTime()
    {
        var baseInput = CreateBaseInput();
        var baseResult = ScenarioCalculator.Calculate(baseInput);

        var extraDebtInput = baseInput with { ExtraDebtPayment = 200 };
        var extraDebtResult = ScenarioCalculator.Calculate(extraDebtInput);

        Assert.True(extraDebtResult.DebtProjection.MonthsToPayoff < baseResult.DebtProjection.MonthsToPayoff);
        Assert.True(extraDebtResult.Comparison.MonthsSavedOnDebt > 0);
        Assert.True(extraDebtResult.Comparison.InterestSaved > 0);
    }

    [Fact]
    public void Calculate_WithZeroDebt_ReturnsZeroMonthsToPayoff()
    {
        var input = CreateBaseInput() with { TotalDebt = 0 };

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(0, result.DebtProjection.MonthsToPayoff);
        Assert.Equal(0, result.DebtProjection.TotalInterestPaid);
    }

    [Fact]
    public void Calculate_WithZeroDebtPayment_ReturnsNullPayoff()
    {
        var input = CreateBaseInput() with { BaseDebtPayment = 0, ExtraDebtPayment = 0 };

        var result = ScenarioCalculator.Calculate(input);

        Assert.Null(result.DebtProjection.MonthsToPayoff);
        Assert.Null(result.DebtProjection.FinalPayoffDate);
    }

    [Fact]
    public void Calculate_InvestmentProjection_ShowsGrowthOver12Months()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        Assert.True(result.InvestmentProjection.ProjectedValue > input.TotalInvestments);
        Assert.Equal(12, result.InvestmentProjection.MonthlySnapshots.Count);
    }

    [Fact]
    public void Calculate_WithExtraInvestment_ShowsAdditionalGrowth()
    {
        var baseInput = CreateBaseInput();
        var baseResult = ScenarioCalculator.Calculate(baseInput);

        var extraInvestInput = baseInput with { ExtraInvestmentContribution = 200 };
        var extraInvestResult = ScenarioCalculator.Calculate(extraInvestInput);

        Assert.True(extraInvestResult.InvestmentProjection.ProjectedValue > baseResult.InvestmentProjection.ProjectedValue);
        Assert.True(extraInvestResult.Comparison.AdditionalInvestmentGrowth > 0);
    }

    [Fact]
    public void Calculate_NetWorthProjection_ShowsChangeOver12Months()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(12, result.NetWorthProjection.MonthlySnapshots.Count);
        Assert.NotEqual(0, result.NetWorthProjection.NetWorthChange);
    }

    [Fact]
    public void Calculate_Comparison_ShowsNetBenefit()
    {
        var input = CreateBaseInput() with { ExtraDebtPayment = 100, ExtraInvestmentContribution = 100 };

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(
            result.Comparison.InterestSaved + result.Comparison.AdditionalInvestmentGrowth,
            result.Comparison.NetBenefit
        );
    }

    [Fact]
    public void Calculate_SliderSummary_ReflectsAllSliders()
    {
        var input = CreateBaseInput() with
        {
            MonthlyDiscretionary = 600,
            ExtraDebtPayment = 150,
            ExtraInvestmentContribution = 75
        };

        var result = ScenarioCalculator.Calculate(input);

        Assert.Equal(600, result.SliderSummary.MonthlyDiscretionary);
        Assert.Equal(150, result.SliderSummary.ExtraDebtPayment);
        Assert.Equal(75, result.SliderSummary.ExtraInvestmentContribution);
    }

    [Fact]
    public void Calculate_ThrowsOnNegativeDiscretionary()
    {
        var input = CreateBaseInput() with { MonthlyDiscretionary = -100 };

        Assert.Throws<ArgumentException>(() => ScenarioCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_ThrowsOnNegativeExtraDebtPayment()
    {
        var input = CreateBaseInput() with { ExtraDebtPayment = -50 };

        Assert.Throws<ArgumentException>(() => ScenarioCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_ThrowsOnNegativeExtraInvestment()
    {
        var input = CreateBaseInput() with { ExtraInvestmentContribution = -25 };

        Assert.Throws<ArgumentException>(() => ScenarioCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_MonthlySurplus_CalculatedCorrectly()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        // Income - (BaseExpenses + Discretionary + DebtPayment + InvestmentContribution)
        // 6000 - (3000 + 500 + 300 + 400) = 1800
        Assert.Equal(1800, result.MonthlySurplus);
    }

    [Fact]
    public void Calculate_DebtSnapshots_ShowProgressivePaydown()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        var snapshots = result.DebtProjection.MonthlySnapshots;
        Assert.True(snapshots.Count > 0);

        // Each snapshot should show decreasing balance
        for (int i = 1; i < snapshots.Count; i++)
        {
            Assert.True(snapshots[i].RemainingBalance <= snapshots[i - 1].RemainingBalance);
        }
    }

    [Fact]
    public void Calculate_InvestmentSnapshots_ShowProgressiveGrowth()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        var snapshots = result.InvestmentProjection.MonthlySnapshots;
        Assert.Equal(12, snapshots.Count);

        // Each snapshot should show increasing value
        for (int i = 1; i < snapshots.Count; i++)
        {
            Assert.True(snapshots[i].Value > snapshots[i - 1].Value);
        }
    }

    [Fact]
    public void Calculate_NetWorthSnapshots_ContainAllComponents()
    {
        var input = CreateBaseInput();

        var result = ScenarioCalculator.Calculate(input);

        foreach (var snapshot in result.NetWorthProjection.MonthlySnapshots)
        {
            Assert.Equal(snapshot.Cash + snapshot.Investments - snapshot.Debt, snapshot.NetWorth);
        }
    }
}
