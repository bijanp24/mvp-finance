using FinanceEngine.Calculators;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Tests.Calculators;

public class AdjustmentSuggestionCalculatorTests
{
    #region Helper Methods

    private static SafeToSpendResult CreateHealthyResult(decimal safeToSpend = 1000m)
    {
        return new SafeToSpendResult(
            SafeToSpend: safeToSpend,
            Status: SafeToSpendStatus.Healthy,
            Breakdown: new SafeToSpendBreakdown(2000m, 500m, 400m, 100m, 14),
            GoalImpacts: Array.Empty<GoalImpact>(),
            StatusMessage: "You're on track!",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );
    }

    private static SafeToSpendResult CreateAtRiskResult(decimal safeToSpend = -200m)
    {
        return new SafeToSpendResult(
            SafeToSpend: safeToSpend,
            Status: SafeToSpendStatus.AtRisk,
            Breakdown: new SafeToSpendBreakdown(1000m, 800m, 500m, 100m, 14),
            GoalImpacts: new[]
            {
                new GoalImpact(1, "Emergency Fund", "SavingsGoal", "AtRisk", 300m, 100m, 200m, 2, "Needs attention")
            },
            StatusMessage: "Action needed",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );
    }

    private static SafeToSpendResult CreateTightResult(decimal safeToSpend = 100m)
    {
        return new SafeToSpendResult(
            SafeToSpend: safeToSpend,
            Status: SafeToSpendStatus.Tight,
            Breakdown: new SafeToSpendBreakdown(1500m, 900m, 400m, 100m, 14),
            GoalImpacts: Array.Empty<GoalImpact>(),
            StatusMessage: "Budget is tight",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );
    }

    private static BudgetAnalysisResult CreateNoOverspendResult()
    {
        return new BudgetAnalysisResult(
            OverspentCategories: Array.Empty<BudgetOverspend>(),
            TotalOverspend: 0m,
            OverallGoalImpacts: Array.Empty<GoalImpact>(),
            HasOverspending: false
        );
    }

    private static BudgetAnalysisResult CreateOverspendResult()
    {
        return new BudgetAnalysisResult(
            OverspentCategories: new[]
            {
                new BudgetOverspend(
                    CategoryId: 1,
                    CategoryName: "Groceries",
                    BudgetAmount: 600m,
                    SpentAmount: 750m,
                    OverspendAmount: 150m,
                    GoalImpacts: new[]
                    {
                        new GoalImpact(1, "Emergency Fund", "SavingsGoal", "OnTrack", 200m, 200m, 0m, 1, "May delay goal")
                    }
                )
            },
            TotalOverspend: 150m,
            OverallGoalImpacts: new[]
            {
                new GoalImpact(1, "Emergency Fund", "SavingsGoal", "OnTrack", 200m, 200m, 0m, 1, "May delay goal")
            },
            HasOverspending: true
        );
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    public void Calculate_WithNullInput_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => AdjustmentSuggestionCalculator.Calculate(null!));
    }

    #endregion

    #region Healthy Status Tests

    [Fact]
    public void Calculate_WithHealthyStatusNoOverspend_ReturnsPositiveSuggestion()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateHealthyResult(),
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.False(result.HasUrgentSuggestions);
        Assert.Contains(result.Suggestions, s => s.Category == SuggestionCategory.Positive);
    }

    [Fact]
    public void Calculate_WithHealthyStatusLowBuffer_SuggestsBufferIncrease()
    {
        var safeToSpendResult = new SafeToSpendResult(
            SafeToSpend: 1000m,
            Status: SafeToSpendStatus.Healthy,
            Breakdown: new SafeToSpendBreakdown(2000m, 500m, 400m, 50m, 14), // Low buffer
            GoalImpacts: Array.Empty<GoalImpact>(),
            StatusMessage: "You're on track!",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: safeToSpendResult,
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.Contains(result.Suggestions, s => s.ActionType == SuggestionActionType.IncreaseBuffer);
    }

    #endregion

    #region Overspend Suggestion Tests

    [Fact]
    public void Calculate_WithOverspending_GeneratesReduceSpendingSuggestion()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateHealthyResult(),
            BudgetAnalysis: CreateOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.Contains(result.Suggestions, s =>
            s.Category == SuggestionCategory.ReduceSpending &&
            s.Title.Contains("Groceries"));
    }

    [Fact]
    public void Calculate_WithOverspending_CalculatesPotentialSavings()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateHealthyResult(),
            BudgetAnalysis: CreateOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        var grocerySuggestion = result.Suggestions.First(s => s.Title.Contains("Groceries"));
        Assert.Equal(150m, grocerySuggestion.PotentialSavings);
        Assert.True(result.TotalPotentialSavings >= 150m);
    }

    [Fact]
    public void Calculate_WithMultipleOverspentCategories_GeneratesAggregateSuggestion()
    {
        var budgetAnalysis = new BudgetAnalysisResult(
            OverspentCategories: new[]
            {
                new BudgetOverspend(1, "Groceries", 600m, 700m, 100m, Array.Empty<GoalImpact>()),
                new BudgetOverspend(2, "Dining", 200m, 350m, 150m, Array.Empty<GoalImpact>())
            },
            TotalOverspend: 250m,
            OverallGoalImpacts: Array.Empty<GoalImpact>(),
            HasOverspending: true
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateHealthyResult(),
            BudgetAnalysis: budgetAnalysis
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.Contains(result.Suggestions, s =>
            s.Id == "overspend_aggregate" &&
            s.ActionType == SuggestionActionType.ReviewBudgets);
    }

    [Fact]
    public void Calculate_WithOverspendAffectingGoals_SetsHighPriority()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateHealthyResult(),
            BudgetAnalysis: CreateOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        var grocerySuggestion = result.Suggestions.First(s => s.Title.Contains("Groceries"));
        Assert.Equal(SuggestionPriority.High, grocerySuggestion.Priority);
        Assert.Contains("Emergency Fund", grocerySuggestion.ImpactOnGoals);
    }

    #endregion

    #region Goal Suggestion Tests

    [Fact]
    public void Calculate_WithUnderfundedGoal_GeneratesIncreaseContributionSuggestion()
    {
        var safeToSpendResult = new SafeToSpendResult(
            SafeToSpend: 500m,
            Status: SafeToSpendStatus.Healthy,
            Breakdown: new SafeToSpendBreakdown(1500m, 500m, 400m, 100m, 14),
            GoalImpacts: new[]
            {
                new GoalImpact(1, "Vacation Fund", "SavingsGoal", "OnTrack", 300m, 150m, 150m, 2, "Underfunded")
            },
            StatusMessage: "On track",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: safeToSpendResult,
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.Contains(result.Suggestions, s =>
            s.Category == SuggestionCategory.IncreaseContribution &&
            s.Title.Contains("Vacation Fund"));
    }

    [Fact]
    public void Calculate_WithBehindGoal_SetsHighPriority()
    {
        var safeToSpendResult = new SafeToSpendResult(
            SafeToSpend: 500m,
            Status: SafeToSpendStatus.Behind,
            Breakdown: new SafeToSpendBreakdown(1500m, 500m, 400m, 100m, 14),
            GoalImpacts: new[]
            {
                new GoalImpact(1, "Debt Payoff", "DebtFree", "Behind", 500m, 200m, 300m, 6, "Significantly behind")
            },
            StatusMessage: "Goals behind",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: safeToSpendResult,
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        var debtSuggestion = result.Suggestions.First(s => s.Title.Contains("Debt Payoff"));
        Assert.Equal(SuggestionPriority.High, debtSuggestion.Priority);
    }

    #endregion

    #region At Risk Status Tests

    [Fact]
    public void Calculate_WithNegativeSafeToSpend_GeneratesEmergencySuggestion()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateAtRiskResult(-200m),
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.True(result.HasUrgentSuggestions);
        Assert.Contains(result.Suggestions, s =>
            s.Category == SuggestionCategory.Emergency &&
            s.Priority == SuggestionPriority.Critical);
    }

    [Fact]
    public void Calculate_WithNegativeSafeToSpend_ShortfallInDescription()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateAtRiskResult(-250m),
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        var emergencySuggestion = result.Suggestions.First(s => s.Category == SuggestionCategory.Emergency);
        Assert.Contains("$250", emergencySuggestion.Description);
    }

    #endregion

    #region Tight Status Tests

    [Fact]
    public void Calculate_WithTightStatus_GeneratesWarningSuggestion()
    {
        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateTightResult(100m),
            BudgetAnalysis: CreateNoOverspendResult()
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.Contains(result.Suggestions, s =>
            s.Category == SuggestionCategory.Warning &&
            s.Priority == SuggestionPriority.Medium);
    }

    #endregion

    #region MaxSuggestions Tests

    [Fact]
    public void Calculate_WithMaxSuggestions_LimitsSuggestionCount()
    {
        var budgetAnalysis = new BudgetAnalysisResult(
            OverspentCategories: new[]
            {
                new BudgetOverspend(1, "Cat1", 100m, 200m, 100m, Array.Empty<GoalImpact>()),
                new BudgetOverspend(2, "Cat2", 100m, 200m, 100m, Array.Empty<GoalImpact>()),
                new BudgetOverspend(3, "Cat3", 100m, 200m, 100m, Array.Empty<GoalImpact>()),
                new BudgetOverspend(4, "Cat4", 100m, 200m, 100m, Array.Empty<GoalImpact>())
            },
            TotalOverspend: 400m,
            OverallGoalImpacts: Array.Empty<GoalImpact>(),
            HasOverspending: true
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateHealthyResult(),
            BudgetAnalysis: budgetAnalysis,
            MaxSuggestions: 3
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        Assert.True(result.Suggestions.Count() <= 3);
    }

    [Fact]
    public void Calculate_SortsByPriorityDescending()
    {
        var budgetAnalysis = new BudgetAnalysisResult(
            OverspentCategories: new[]
            {
                new BudgetOverspend(1, "Groceries", 600m, 800m, 200m, new[]
                {
                    new GoalImpact(1, "Goal", "Type", "OnTrack", 100m, 100m, 0m, 2, "Impact")
                })
            },
            TotalOverspend: 200m,
            OverallGoalImpacts: Array.Empty<GoalImpact>(),
            HasOverspending: true
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: CreateTightResult(),
            BudgetAnalysis: budgetAnalysis
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        var priorities = result.Suggestions.Select(s => s.Priority).ToList();
        for (int i = 0; i < priorities.Count - 1; i++)
        {
            Assert.True(priorities[i] >= priorities[i + 1]);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Calculate_ComplexScenario_GeneratesMultipleSuggestionTypes()
    {
        var safeToSpendResult = new SafeToSpendResult(
            SafeToSpend: 150m,
            Status: SafeToSpendStatus.Tight,
            Breakdown: new SafeToSpendBreakdown(2000m, 1000m, 750m, 100m, 14),
            GoalImpacts: new[]
            {
                new GoalImpact(1, "Emergency Fund", "SavingsGoal", "AtRisk", 400m, 250m, 150m, 3, "Underfunded")
            },
            StatusMessage: "Budget is tight",
            HorizonEndDate: DateTime.Now.AddDays(14)
        );

        var budgetAnalysis = new BudgetAnalysisResult(
            OverspentCategories: new[]
            {
                new BudgetOverspend(1, "Dining", 300m, 450m, 150m, Array.Empty<GoalImpact>())
            },
            TotalOverspend: 150m,
            OverallGoalImpacts: Array.Empty<GoalImpact>(),
            HasOverspending: true
        );

        var input = new AdjustmentSuggestionInput(
            SafeToSpendResult: safeToSpendResult,
            BudgetAnalysis: budgetAnalysis
        );

        var result = AdjustmentSuggestionCalculator.Calculate(input);

        // Should have: ReduceSpending, IncreaseContribution, and Warning suggestions
        Assert.Contains(result.Suggestions, s => s.Category == SuggestionCategory.ReduceSpending);
        Assert.Contains(result.Suggestions, s => s.Category == SuggestionCategory.IncreaseContribution);
        Assert.Contains(result.Suggestions, s => s.Category == SuggestionCategory.Warning);
    }

    #endregion
}
