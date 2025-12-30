using FinanceEngine.Calculators;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Services;

namespace FinanceEngine.Tests.Calculators;

public class BudgetAnalysisCalculatorTests
{
    #region Input Validation Tests

    [Fact]
    public void Analyze_WithNullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => BudgetAnalysisCalculator.Analyze(null!));
    }

    [Fact]
    public void Analyze_WithEmptyBudgets_ReturnsNoOverspending()
    {
        var input = new BudgetAnalysisInput(
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.False(result.HasOverspending);
        Assert.Empty(result.OverspentCategories);
        Assert.Equal(0m, result.TotalOverspend);
    }

    #endregion

    #region Overspending Detection Tests

    [Fact]
    public void Analyze_WithBudgetUnderSpent_ReturnsNoOverspending()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 400m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.False(result.HasOverspending);
        Assert.Empty(result.OverspentCategories);
        Assert.Equal(0m, result.TotalOverspend);
    }

    [Fact]
    public void Analyze_WithBudgetExactlyMet_ReturnsNoOverspending()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 600m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.False(result.HasOverspending);
        Assert.Empty(result.OverspentCategories);
    }

    [Fact]
    public void Analyze_WithSingleBudgetOverspent_DetectsOverspending()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 750m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        Assert.Single(result.OverspentCategories);
        Assert.Equal(150m, result.TotalOverspend);

        var overspend = result.OverspentCategories.First();
        Assert.Equal("Groceries", overspend.CategoryName);
        Assert.Equal(600m, overspend.BudgetAmount);
        Assert.Equal(750m, overspend.SpentAmount);
        Assert.Equal(150m, overspend.OverspendAmount);
    }

    [Fact]
    public void Analyze_WithMultipleBudgetsOverspent_DetectsAll()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 700m),
                new BudgetInfo(2, "Dining", 200m, BudgetFrequency.Monthly, 350m),
                new BudgetInfo(3, "Utilities", 150m, BudgetFrequency.Monthly, 100m) // Under budget
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        Assert.Equal(2, result.OverspentCategories.Count());
        Assert.Equal(250m, result.TotalOverspend); // 100 + 150
    }

    #endregion

    #region Budget Frequency Tests

    [Fact]
    public void Analyze_WithWeeklyBudget_CalculatesCorrectPeriodAmount()
    {
        // Weekly budget of $100 for 14 days = $100/7 * 14 = $200
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Coffee", 100m, BudgetFrequency.Weekly, 250m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 14
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        var overspend = result.OverspentCategories.First();
        Assert.Equal(200m, overspend.BudgetAmount); // $100/week * 2 weeks
        Assert.Equal(50m, overspend.OverspendAmount); // $250 - $200
    }

    [Fact]
    public void Analyze_WithBiWeeklyBudget_CalculatesCorrectPeriodAmount()
    {
        // BiWeekly budget of $280 for 30 days = $280/14 * 30 = $600
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Gas", 280m, BudgetFrequency.BiWeekly, 700m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        var overspend = result.OverspentCategories.First();
        Assert.Equal(600m, overspend.BudgetAmount);
        Assert.Equal(100m, overspend.OverspendAmount);
    }

    #endregion

    #region Goal Impact Tests

    [Fact]
    public void Analyze_WithOverspendingAndGoals_CalculatesGoalImpact()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Shopping", 300m, BudgetFrequency.Monthly, 500m)
            },
            Goals: new[]
            {
                new GoalInfo(1, "Emergency Fund", GoalType.SavingsGoal, 200m, 200m, GoalStatus.OnTrack)
            },
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        Assert.Single(result.OverspentCategories);

        var overspend = result.OverspentCategories.First();
        Assert.Single(overspend.GoalImpacts);

        var goalImpact = overspend.GoalImpacts.First();
        Assert.Equal("Emergency Fund", goalImpact.GoalName);
    }

    [Fact]
    public void Analyze_WithOverspending_CalculatesOverallGoalImpacts()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Shopping", 300m, BudgetFrequency.Monthly, 600m)
            },
            Goals: new[]
            {
                new GoalInfo(1, "Emergency Fund", GoalType.SavingsGoal, 300m, 300m, GoalStatus.OnTrack),
                new GoalInfo(2, "Vacation", GoalType.SavingsGoal, 100m, 100m, GoalStatus.OnTrack)
            },
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        // Overall impacts should be calculated for total overspend ($300)
        Assert.Equal(2, result.OverallGoalImpacts.Count());

        var emergencyImpact = result.OverallGoalImpacts.First(g => g.GoalName == "Emergency Fund");
        Assert.NotNull(emergencyImpact.ImpactMessage);
    }

    [Fact]
    public void Analyze_WithLargeOverspend_CalculatesMonthsDelay()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Shopping", 200m, BudgetFrequency.Monthly, 700m) // $500 overspend
            },
            Goals: new[]
            {
                new GoalInfo(1, "Emergency Fund", GoalType.SavingsGoal, 250m, 250m, GoalStatus.OnTrack)
            },
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        var overallImpact = result.OverallGoalImpacts.First();
        // $500 overspend / $250 monthly = 2 months impact
        Assert.NotNull(overallImpact.DelayedMonths);
        Assert.True(overallImpact.DelayedMonths >= 2);
    }

    [Fact]
    public void Analyze_WithNoGoals_ReturnsEmptyGoalImpacts()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Shopping", 200m, BudgetFrequency.Monthly, 300m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        Assert.Empty(result.OverallGoalImpacts);
    }

    [Fact]
    public void Analyze_WithZeroContributionGoal_SkipsGoalInImpact()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Shopping", 200m, BudgetFrequency.Monthly, 300m)
            },
            Goals: new[]
            {
                new GoalInfo(1, "Completed Goal", GoalType.SavingsGoal, 0m, 0m, GoalStatus.Ahead)
            },
            PeriodDays: 30
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        Assert.Empty(result.OverallGoalImpacts);
    }

    #endregion

    #region Period Days Tests

    [Fact]
    public void Analyze_WithShortPeriod_CalculatesProRatedBudget()
    {
        // Monthly budget of $600 for 7 days = $600/30 * 7 = $140
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 200m)
            },
            Goals: Array.Empty<GoalInfo>(),
            PeriodDays: 7
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        Assert.True(result.HasOverspending);
        var overspend = result.OverspentCategories.First();
        Assert.Equal(140m, overspend.BudgetAmount);
        Assert.Equal(60m, overspend.OverspendAmount); // $200 - $140
    }

    [Fact]
    public void Analyze_WithDefaultPeriod_UsesThrityDays()
    {
        var input = new BudgetAnalysisInput(
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 700m)
            },
            Goals: Array.Empty<GoalInfo>()
        );

        var result = BudgetAnalysisCalculator.Analyze(input);

        var overspend = result.OverspentCategories.First();
        Assert.Equal(600m, overspend.BudgetAmount); // Full monthly amount
    }

    #endregion
}
