using FinanceEngine.Services;

namespace FinanceEngine.Tests.Services;

public class GoalProgressCalculatorTests
{
    private readonly DateTime _calculationDate = new DateTime(2025, 1, 1);

    #region DebtFree Goal Tests

    [Fact]
    public void Calculate_DebtFreeGoal_WithNoDebt_ReturnsAhead()
    {
        // Arrange
        var input = new GoalProgressInput(
            Type: GoalType.DebtFree,
            TargetAmount: null,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 0m,
            MonthlyContributionRate: 500m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(0m, result.CurrentValue);
        Assert.Equal(0m, result.TargetValue);
        Assert.Equal(GoalStatus.Ahead, result.Status);
        Assert.Equal(100m, result.ProgressPercentage);
    }

    [Fact]
    public void Calculate_DebtFreeGoal_OnTrack_ReturnsCorrectRequiredMonthly()
    {
        // Arrange - Jan 1 to Dec 31 = 11 months
        var input = new GoalProgressInput(
            Type: GoalType.DebtFree,
            TargetAmount: null,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 11000m, // $11k debt
            MonthlyContributionRate: 1000m, // Paying $1k/month = 11 months
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(11000m, result.CurrentValue);
        Assert.Equal(0m, result.TargetValue);
        Assert.Equal(11, result.MonthsRemaining); // Jan to Dec = 11 month difference
        Assert.Equal(1000m, result.RequiredMonthlyAmount);
        Assert.Equal(11000m, result.AmountRemaining);
        Assert.Equal(GoalStatus.OnTrack, result.Status);
    }

    [Fact]
    public void Calculate_DebtFreeGoal_NoPaymentRate_ReturnsAtRisk()
    {
        // Arrange
        var input = new GoalProgressInput(
            Type: GoalType.DebtFree,
            TargetAmount: null,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 10000m,
            MonthlyContributionRate: null,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.AtRisk, result.Status);
        Assert.Null(result.ProjectedCompletionDate);
    }

    [Fact]
    public void Calculate_DebtFreeGoal_LowPaymentRate_ReturnsBehind()
    {
        // Arrange - $24k debt, only paying $500/month = 48 months, but deadline is 12 months
        var input = new GoalProgressInput(
            Type: GoalType.DebtFree,
            TargetAmount: null,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 24000m,
            MonthlyContributionRate: 500m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.Behind, result.Status);
        Assert.True(result.ProjectedCompletionDate > input.TargetDate);
    }

    #endregion

    #region Investment Goal Tests

    [Fact]
    public void Calculate_InvestmentGoal_AlreadyAchieved_ReturnsAhead()
    {
        // Arrange
        var input = new GoalProgressInput(
            Type: GoalType.InvestmentTarget,
            TargetAmount: 50000m,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 55000m, // Already exceeded target
            MonthlyContributionRate: 1000m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(55000m, result.CurrentValue);
        Assert.Equal(50000m, result.TargetValue);
        Assert.Equal(GoalStatus.Ahead, result.Status);
        Assert.True(result.ProgressPercentage >= 100m);
        Assert.True(result.AmountRemaining < 0); // Negative = exceeded
    }

    [Fact]
    public void Calculate_InvestmentGoal_OnTrack_ReturnsCorrectProgress()
    {
        // Arrange - Need $60k, have $30k, 11 months (Jan to Dec) = ~$2727/month needed
        var input = new GoalProgressInput(
            Type: GoalType.InvestmentTarget,
            TargetAmount: 60000m,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 30000m,
            MonthlyContributionRate: 2800m, // Contributing enough to be on track
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(30000m, result.CurrentValue);
        Assert.Equal(60000m, result.TargetValue);
        Assert.Equal(50m, result.ProgressPercentage);
        Assert.Equal(11, result.MonthsRemaining);
        Assert.True(result.RequiredMonthlyAmount > 2700m && result.RequiredMonthlyAmount < 2800m); // ~$2727/month
        Assert.Equal(30000m, result.AmountRemaining);
    }

    [Fact]
    public void Calculate_InvestmentGoal_ZeroTarget_HandlesGracefully()
    {
        // Arrange
        var input = new GoalProgressInput(
            Type: GoalType.InvestmentTarget,
            TargetAmount: 0m,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 1000m,
            MonthlyContributionRate: 100m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.Ahead, result.Status);
        Assert.Equal(100m, result.ProgressPercentage);
    }

    #endregion

    #region Savings Goal Tests

    [Fact]
    public void Calculate_SavingsGoal_HalfwayThere_ReturnsCorrectProgress()
    {
        // Arrange - Saving for $5k vacation, have $2.5k, 6 months left
        var input = new GoalProgressInput(
            Type: GoalType.SavingsGoal,
            TargetAmount: 5000m,
            TargetDate: new DateTime(2025, 7, 1),
            CurrentLinkedBalance: 2500m,
            MonthlyContributionRate: 400m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(2500m, result.CurrentValue);
        Assert.Equal(5000m, result.TargetValue);
        Assert.Equal(50m, result.ProgressPercentage);
        Assert.Equal(6, result.MonthsRemaining);
        Assert.True(result.RequiredMonthlyAmount > 400m); // Need ~$417/month
    }

    [Fact]
    public void Calculate_SavingsGoal_AheadOfSchedule_ReturnsOnTrackOrAhead()
    {
        // Arrange - Need $3k by June, already have $2.5k in January = very ahead
        var input = new GoalProgressInput(
            Type: GoalType.SavingsGoal,
            TargetAmount: 3000m,
            TargetDate: new DateTime(2025, 6, 30),
            CurrentLinkedBalance: 2500m,
            MonthlyContributionRate: 200m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.True(result.Status == GoalStatus.OnTrack || result.Status == GoalStatus.Ahead);
        Assert.True(result.ProgressPercentage > 80m);
    }

    #endregion

    #region Net Worth Goal Tests

    [Fact]
    public void Calculate_NetWorthGoal_NegativeNetWorth_HandlesCorrectly()
    {
        // Arrange - Current net worth is -$5k (more debt than assets)
        var input = new GoalProgressInput(
            Type: GoalType.NetWorthMilestone,
            TargetAmount: 0m, // Goal is to reach $0 (break even)
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: -5000m,
            MonthlyContributionRate: 500m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(-5000m, result.CurrentValue);
        Assert.Equal(0m, result.TargetValue);
        Assert.Equal(5000m, result.AmountRemaining);
    }

    [Fact]
    public void Calculate_NetWorthGoal_PositiveProgress_ReturnsCorrectStatus()
    {
        // Arrange - Goal is $100k net worth, currently at $75k
        var input = new GoalProgressInput(
            Type: GoalType.NetWorthMilestone,
            TargetAmount: 100000m,
            TargetDate: new DateTime(2025, 12, 31),
            CurrentLinkedBalance: 75000m,
            MonthlyContributionRate: 2000m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(75000m, result.CurrentValue);
        Assert.Equal(100000m, result.TargetValue);
        Assert.Equal(75m, result.ProgressPercentage);
        Assert.Equal(25000m, result.AmountRemaining);
    }

    [Fact]
    public void Calculate_NetWorthGoal_Millionaire_AlreadyAchieved()
    {
        // Arrange - Goal is $1M, already at $1.2M
        var input = new GoalProgressInput(
            Type: GoalType.NetWorthMilestone,
            TargetAmount: 1000000m,
            TargetDate: new DateTime(2030, 12, 31),
            CurrentLinkedBalance: 1200000m,
            MonthlyContributionRate: 5000m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.Ahead, result.Status);
        Assert.Equal(120m, result.ProgressPercentage);
        Assert.Equal(-200000m, result.AmountRemaining); // Exceeded by $200k
    }

    #endregion

    #region Expired Goal Tests

    [Fact]
    public void Calculate_ExpiredGoal_Achieved_ReturnsAhead()
    {
        // Arrange - Goal deadline passed, but goal was achieved
        var input = new GoalProgressInput(
            Type: GoalType.SavingsGoal,
            TargetAmount: 5000m,
            TargetDate: new DateTime(2024, 12, 31), // Past date
            CurrentLinkedBalance: 6000m,
            MonthlyContributionRate: null,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.Ahead, result.Status);
        Assert.Contains("achieved", result.StatusMessage.ToLower());
    }

    [Fact]
    public void Calculate_ExpiredGoal_NotAchieved_ReturnsBehind()
    {
        // Arrange - Goal deadline passed, goal not achieved
        var input = new GoalProgressInput(
            Type: GoalType.SavingsGoal,
            TargetAmount: 5000m,
            TargetDate: new DateTime(2024, 12, 31), // Past date
            CurrentLinkedBalance: 3000m,
            MonthlyContributionRate: null,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.Behind, result.Status);
        Assert.Equal(0, result.MonthsRemaining);
    }

    [Fact]
    public void Calculate_ExpiredDebtFreeGoal_StillHasDebt_ReturnsBehind()
    {
        // Arrange
        var input = new GoalProgressInput(
            Type: GoalType.DebtFree,
            TargetAmount: null,
            TargetDate: new DateTime(2024, 12, 31),
            CurrentLinkedBalance: 5000m, // Still has debt
            MonthlyContributionRate: null,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.Equal(GoalStatus.Behind, result.Status);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculate_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GoalProgressCalculator.Calculate(null!));
    }

    [Fact]
    public void Calculate_ZeroMonthsRemaining_HandlesGracefully()
    {
        // Arrange - Target date is this month
        var input = new GoalProgressInput(
            Type: GoalType.SavingsGoal,
            TargetAmount: 1000m,
            TargetDate: new DateTime(2025, 1, 31),
            CurrentLinkedBalance: 500m,
            MonthlyContributionRate: 100m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert - Should not throw, months remaining should be 0-1
        Assert.True(result.MonthsRemaining >= 0);
    }

    [Fact]
    public void Calculate_VeryLongTimeframe_HandlesLargeNumbers()
    {
        // Arrange - 30 year goal
        var input = new GoalProgressInput(
            Type: GoalType.NetWorthMilestone,
            TargetAmount: 2000000m,
            TargetDate: new DateTime(2055, 12, 31),
            CurrentLinkedBalance: 50000m,
            MonthlyContributionRate: 1000m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.True(result.MonthsRemaining > 300); // ~30 years
        Assert.True(result.RequiredMonthlyAmount > 0);
    }

    [Fact]
    public void Calculate_ProjectedCompletionDate_CalculatesCorrectly()
    {
        // Arrange - $10k remaining at $1k/month = 10 months
        var input = new GoalProgressInput(
            Type: GoalType.SavingsGoal,
            TargetAmount: 20000m,
            TargetDate: new DateTime(2026, 12, 31),
            CurrentLinkedBalance: 10000m,
            MonthlyContributionRate: 1000m,
            CalculationDate: _calculationDate
        );

        // Act
        var result = GoalProgressCalculator.Calculate(input);

        // Assert
        Assert.NotNull(result.ProjectedCompletionDate);
        Assert.True(result.ProjectedCompletionDate.Value.Year == 2025);
        Assert.True(result.ProjectedCompletionDate.Value.Month >= 10); // October or later
    }

    #endregion
}
