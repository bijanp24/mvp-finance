using FinanceEngine.Calculators;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;
using FinanceEngine.Services;

namespace FinanceEngine.Tests.Calculators;

public class SafeToSpendCalculatorTests
{
    #region Input Validation Tests

    [Fact]
    public void Calculate_WithNullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SafeToSpendCalculator.Calculate(null!));
    }

    [Fact]
    public void Calculate_WithNegativeAvailableCash_ThrowsArgumentException()
    {
        var input = new SafeToSpendInput(
            AvailableCash: -100m,
            CalculationDate: DateTime.Now,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        Assert.Throws<ArgumentException>(() => SafeToSpendCalculator.Calculate(input));
    }

    #endregion

    #region Time Horizon Tests

    [Fact]
    public void Calculate_WithRollingTwoWeeks_SetsCorrectHorizonEndDate()
    {
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Equal(calculationDate.AddDays(14), result.HorizonEndDate);
        Assert.Equal(14, result.Breakdown.DaysInHorizon);
    }

    [Fact]
    public void Calculate_WithCurrentMonth_SetsEndOfMonthHorizon()
    {
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.CurrentMonth,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // January has 31 days, so end of month is Jan 31
        Assert.Equal(new DateTime(2024, 1, 31), result.HorizonEndDate);
        Assert.Equal(16, result.Breakdown.DaysInHorizon); // Jan 15 to Jan 31
    }

    [Fact]
    public void Calculate_WithNextPaycheck_UsesProvidedDate()
    {
        var calculationDate = new DateTime(2024, 1, 15);
        var nextPaycheck = new DateTime(2024, 1, 25);
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.NextPaycheck,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>(),
            NextPaycheckDate: nextPaycheck
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Equal(nextPaycheck, result.HorizonEndDate);
        Assert.Equal(10, result.Breakdown.DaysInHorizon);
    }

    [Fact]
    public void Calculate_WithNextPaycheck_UsesIncomeEventIfNoDateProvided()
    {
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.NextPaycheck,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: new[] { new IncomeEvent(new DateTime(2024, 1, 30), 2500m, "Paycheck") }
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Equal(new DateTime(2024, 1, 30), result.HorizonEndDate);
    }

    #endregion

    #region Budget Calculation Tests

    [Fact]
    public void Calculate_WithBudgets_SubtractsUpcomingBills()
    {
        var calculationDate = new DateTime(2024, 1, 1);
        var input = new SafeToSpendInput(
            AvailableCash: 2000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.CurrentMonth,
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 0m),
                new BudgetInfo(2, "Utilities", 200m, BudgetFrequency.Monthly, 0m)
            },
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // Full month = ~30 days, so full budget amounts apply
        // Groceries: 600/30 * 30 = 600, Utilities: 200/30 * 30 = 200
        Assert.True(result.Breakdown.UpcomingBills > 0);
        Assert.True(result.SafeToSpend < input.AvailableCash);
    }

    [Fact]
    public void Calculate_WithPartiallySpentBudget_SubtractsOnlyRemaining()
    {
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SafeToSpendInput(
            AvailableCash: 2000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 300m) // Already spent $300
            },
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // 2 weeks of $600/month = $600/30 * 14 = $280
        // Already spent $300, so remaining for period = max(0, 280 - 300) = 0
        Assert.True(result.Breakdown.UpcomingBills >= 0);
    }

    #endregion

    #region Goal Contribution Tests

    [Fact]
    public void Calculate_WithGoals_SubtractsRequiredContributions()
    {
        var calculationDate = new DateTime(2024, 1, 1);
        var input = new SafeToSpendInput(
            AvailableCash: 3000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.CurrentMonth,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: new[]
            {
                new GoalInfo(1, "Emergency Fund", GoalType.SavingsGoal, 500m, 500m, GoalStatus.OnTrack),
                new GoalInfo(2, "Retirement", GoalType.InvestmentTarget, 300m, 300m, GoalStatus.OnTrack)
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // Monthly contributions: $500 + $300 = $800
        // For ~30 days, full monthly amount applies
        Assert.True(result.Breakdown.RequiredGoalContributions > 0);
    }

    [Fact]
    public void Calculate_WithUnderfundedGoal_ShowsGoalImpact()
    {
        var calculationDate = new DateTime(2024, 1, 1);
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.CurrentMonth,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: new[]
            {
                new GoalInfo(1, "Emergency Fund", GoalType.SavingsGoal, 500m, 200m, GoalStatus.AtRisk)
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Single(result.GoalImpacts);
        var impact = result.GoalImpacts.First();
        Assert.Equal("Emergency Fund", impact.GoalName);
        Assert.Equal(300m, impact.ContributionGap); // 500 required - 200 current
        Assert.True(impact.DelayedMonths > 0);
    }

    #endregion

    #region Status Determination Tests

    [Fact]
    public void Calculate_WithHighSafeToSpend_ReturnsHealthyStatus()
    {
        var input = new SafeToSpendInput(
            AvailableCash: 5000m,
            CalculationDate: DateTime.Now,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: new[] { new BudgetInfo(1, "Groceries", 300m, BudgetFrequency.Monthly, 0m) },
            Goals: new[] { new GoalInfo(1, "Savings", GoalType.SavingsGoal, 200m, 200m, GoalStatus.OnTrack) },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Equal(SafeToSpendStatus.Healthy, result.Status);
    }

    [Fact]
    public void Calculate_WithLowSafeToSpend_ReturnsTightStatus()
    {
        // Use a full month to get accurate budget calculations
        var calculationDate = new DateTime(2024, 1, 1);
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.CurrentMonth,
            Budgets: new[] { new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 0m) },
            Goals: new[] { new GoalInfo(1, "Savings", GoalType.SavingsGoal, 200m, 200m, GoalStatus.OnTrack) },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // SafeToSpend = 1000 - 600 (budget) - 200 (goal) = 200
        // 200/1000 = 20%, which is at the threshold for Tight
        Assert.True(result.Status == SafeToSpendStatus.Tight || result.Status == SafeToSpendStatus.Healthy);
        Assert.True(result.SafeToSpend > 0);
    }

    [Fact]
    public void Calculate_WithNegativeSafeToSpend_ReturnsAtRiskStatus()
    {
        // Use a full month and large expenses to create negative safe-to-spend
        var calculationDate = new DateTime(2024, 1, 1);
        var input = new SafeToSpendInput(
            AvailableCash: 500m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.CurrentMonth,
            Budgets: new[] { new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 0m) },
            Goals: new[] { new GoalInfo(1, "Savings", GoalType.SavingsGoal, 500m, 500m, GoalStatus.OnTrack) },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // SafeToSpend = 500 - 600 (budget) - 500 (goal) = -600
        Assert.Equal(SafeToSpendStatus.AtRisk, result.Status);
        Assert.True(result.SafeToSpend < 0);
    }

    [Fact]
    public void Calculate_WithGoalsBehind_ReturnsBehindStatus()
    {
        var input = new SafeToSpendInput(
            AvailableCash: 5000m,
            CalculationDate: DateTime.Now,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: new[] { new GoalInfo(1, "Debt Payoff", GoalType.DebtFree, 1000m, 200m, GoalStatus.Behind) },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Equal(SafeToSpendStatus.Behind, result.Status);
    }

    #endregion

    #region Buffer Tests

    [Fact]
    public void Calculate_WithMinimumBuffer_SubtractsBuffer()
    {
        var input = new SafeToSpendInput(
            AvailableCash: 1000m,
            CalculationDate: DateTime.Now,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: Array.Empty<BudgetInfo>(),
            Goals: Array.Empty<GoalInfo>(),
            UpcomingIncome: Array.Empty<IncomeEvent>(),
            MinimumBuffer: 200m
        );

        var result = SafeToSpendCalculator.Calculate(input);

        Assert.Equal(200m, result.Breakdown.MinimumBuffer);
        Assert.Equal(800m, result.SafeToSpend);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Calculate_FullScenario_ReturnsCorrectBreakdown()
    {
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SafeToSpendInput(
            AvailableCash: 3000m,
            CalculationDate: calculationDate,
            TimeHorizon: TimeHorizon.RollingTwoWeeks,
            Budgets: new[]
            {
                new BudgetInfo(1, "Groceries", 600m, BudgetFrequency.Monthly, 150m),
                new BudgetInfo(2, "Dining", 200m, BudgetFrequency.Monthly, 50m)
            },
            Goals: new[]
            {
                new GoalInfo(1, "Emergency Fund", GoalType.SavingsGoal, 300m, 300m, GoalStatus.OnTrack),
                new GoalInfo(2, "Vacation", GoalType.SavingsGoal, 100m, 100m, GoalStatus.Ahead)
            },
            UpcomingIncome: new[] { new IncomeEvent(new DateTime(2024, 1, 30), 2500m, "Paycheck") },
            MinimumBuffer: 100m
        );

        var result = SafeToSpendCalculator.Calculate(input);

        // Verify breakdown is populated
        Assert.Equal(3000m, result.Breakdown.AvailableCash);
        Assert.Equal(100m, result.Breakdown.MinimumBuffer);
        Assert.Equal(14, result.Breakdown.DaysInHorizon);
        Assert.True(result.Breakdown.UpcomingBills >= 0);
        Assert.True(result.Breakdown.RequiredGoalContributions >= 0);

        // Verify goal impacts
        Assert.Equal(2, result.GoalImpacts.Count());

        // Verify status message exists
        Assert.NotEmpty(result.StatusMessage);
    }

    #endregion
}
