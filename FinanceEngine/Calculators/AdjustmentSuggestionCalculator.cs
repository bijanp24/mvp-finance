using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Calculators;

/// <summary>
/// Generates actionable suggestions to improve financial health.
/// </summary>
public static class AdjustmentSuggestionCalculator
{
    /// <summary>
    /// Generates suggestions based on safe-to-spend status and budget analysis.
    /// </summary>
    public static AdjustmentSuggestionResult Calculate(AdjustmentSuggestionInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var suggestions = new List<AdjustmentSuggestion>();

        // Analyze and generate suggestions based on priority
        GenerateOverspendSuggestions(suggestions, input);
        GenerateGoalSuggestions(suggestions, input);
        GenerateSafeToSpendSuggestions(suggestions, input);
        GenerateGeneralSuggestions(suggestions, input);

        // Sort by priority and limit
        var sortedSuggestions = suggestions
            .OrderByDescending(s => s.Priority)
            .Take(input.MaxSuggestions)
            .ToList();

        return new AdjustmentSuggestionResult(
            Suggestions: sortedSuggestions,
            HasUrgentSuggestions: sortedSuggestions.Any(s => s.Priority == SuggestionPriority.High),
            TotalPotentialSavings: sortedSuggestions.Sum(s => s.PotentialSavings ?? 0m)
        );
    }

    private static void GenerateOverspendSuggestions(List<AdjustmentSuggestion> suggestions, AdjustmentSuggestionInput input)
    {
        if (!input.BudgetAnalysis.HasOverspending)
            return;

        foreach (var overspend in input.BudgetAnalysis.OverspentCategories)
        {
            // High priority if overspend affects goals
            var priority = overspend.GoalImpacts.Any(g => g.DelayedMonths > 0)
                ? SuggestionPriority.High
                : SuggestionPriority.Medium;

            var reductionPercent = overspend.SpentAmount > 0
                ? Math.Round((overspend.OverspendAmount / overspend.SpentAmount) * 100)
                : 0;

            suggestions.Add(new AdjustmentSuggestion(
                Id: $"overspend_{overspend.CategoryId}",
                Category: SuggestionCategory.ReduceSpending,
                Title: $"Reduce {overspend.CategoryName} spending",
                Description: $"You've spent ${overspend.OverspendAmount:N0} over budget on {overspend.CategoryName}. " +
                            $"Consider reducing spending by {reductionPercent}% to stay on track.",
                Priority: priority,
                PotentialSavings: overspend.OverspendAmount,
                ActionType: SuggestionActionType.ReduceBudgetCategory,
                ActionTarget: overspend.CategoryId.ToString(),
                ImpactOnGoals: overspend.GoalImpacts.Select(g => g.GoalName).ToArray()
            ));
        }

        // Add aggregate suggestion if multiple categories overspent
        if (input.BudgetAnalysis.OverspentCategories.Count() >= 2)
        {
            suggestions.Add(new AdjustmentSuggestion(
                Id: "overspend_aggregate",
                Category: SuggestionCategory.ReduceSpending,
                Title: "Review overall discretionary spending",
                Description: $"Multiple budget categories are overspent (${input.BudgetAnalysis.TotalOverspend:N0} total). " +
                            "Consider reviewing your spending habits across all categories.",
                Priority: SuggestionPriority.High,
                PotentialSavings: input.BudgetAnalysis.TotalOverspend,
                ActionType: SuggestionActionType.ReviewBudgets,
                ActionTarget: null,
                ImpactOnGoals: input.BudgetAnalysis.OverallGoalImpacts.Select(g => g.GoalName).ToArray()
            ));
        }
    }

    private static void GenerateGoalSuggestions(List<AdjustmentSuggestion> suggestions, AdjustmentSuggestionInput input)
    {
        foreach (var impact in input.SafeToSpendResult.GoalImpacts)
        {
            if (impact.ContributionGap <= 0)
                continue;

            var priority = impact.CurrentStatus == "Behind" || impact.CurrentStatus == "AtRisk"
                ? SuggestionPriority.High
                : SuggestionPriority.Medium;

            suggestions.Add(new AdjustmentSuggestion(
                Id: $"goal_{impact.GoalId}",
                Category: SuggestionCategory.IncreaseContribution,
                Title: $"Increase {impact.GoalName} contribution",
                Description: $"Your {impact.GoalName} goal needs ${impact.ContributionGap:N0}/month more. " +
                            impact.ImpactMessage,
                Priority: priority,
                PotentialSavings: null, // This is about saving more, not spending less
                ActionType: SuggestionActionType.IncreaseGoalContribution,
                ActionTarget: impact.GoalId.ToString(),
                ImpactOnGoals: new[] { impact.GoalName }
            ));
        }
    }

    private static void GenerateSafeToSpendSuggestions(List<AdjustmentSuggestion> suggestions, AdjustmentSuggestionInput input)
    {
        var result = input.SafeToSpendResult;

        if (result.Status == SafeToSpendStatus.AtRisk && result.SafeToSpend < 0)
        {
            var shortfall = Math.Abs(result.SafeToSpend);
            suggestions.Add(new AdjustmentSuggestion(
                Id: "safetospend_negative",
                Category: SuggestionCategory.Emergency,
                Title: "Immediate action needed",
                Description: $"You're ${shortfall:N0} short of covering your obligations and goals. " +
                            "Consider reducing discretionary spending or adjusting goal timelines.",
                Priority: SuggestionPriority.Critical,
                PotentialSavings: null,
                ActionType: SuggestionActionType.ReviewBudgets,
                ActionTarget: null,
                ImpactOnGoals: result.GoalImpacts.Select(g => g.GoalName).ToArray()
            ));
        }
        else if (result.Status == SafeToSpendStatus.Tight)
        {
            suggestions.Add(new AdjustmentSuggestion(
                Id: "safetospend_tight",
                Category: SuggestionCategory.Warning,
                Title: "Budget is tight this period",
                Description: $"You have ${result.SafeToSpend:N0} available, which is below your comfort level. " +
                            "Minimize non-essential purchases until your next income.",
                Priority: SuggestionPriority.Medium,
                PotentialSavings: null,
                ActionType: SuggestionActionType.Monitor,
                ActionTarget: null,
                ImpactOnGoals: Array.Empty<string>()
            ));
        }
    }

    private static void GenerateGeneralSuggestions(List<AdjustmentSuggestion> suggestions, AdjustmentSuggestionInput input)
    {
        var result = input.SafeToSpendResult;

        // Suggest building buffer if minimal
        if (result.Breakdown.MinimumBuffer < 100m && result.Status == SafeToSpendStatus.Healthy)
        {
            suggestions.Add(new AdjustmentSuggestion(
                Id: "buffer_increase",
                Category: SuggestionCategory.Optimization,
                Title: "Consider increasing your buffer",
                Description: "Your safety buffer is low. Consider setting aside $100-200 for unexpected expenses.",
                Priority: SuggestionPriority.Low,
                PotentialSavings: null,
                ActionType: SuggestionActionType.IncreaseBuffer,
                ActionTarget: null,
                ImpactOnGoals: Array.Empty<string>()
            ));
        }

        // Positive reinforcement when healthy
        if (result.Status == SafeToSpendStatus.Healthy && suggestions.Count == 0)
        {
            suggestions.Add(new AdjustmentSuggestion(
                Id: "status_healthy",
                Category: SuggestionCategory.Positive,
                Title: "You're on track!",
                Description: $"Great job! You have ${result.SafeToSpend:N0} available while staying on track for all goals.",
                Priority: SuggestionPriority.Low,
                PotentialSavings: null,
                ActionType: SuggestionActionType.None,
                ActionTarget: null,
                ImpactOnGoals: Array.Empty<string>()
            ));
        }
    }
}

/// <summary>
/// Input for adjustment suggestion calculation.
/// </summary>
public record AdjustmentSuggestionInput(
    SafeToSpendResult SafeToSpendResult,
    BudgetAnalysisResult BudgetAnalysis,
    int MaxSuggestions = 5
);

/// <summary>
/// Result of adjustment suggestion calculation.
/// </summary>
public record AdjustmentSuggestionResult(
    IEnumerable<AdjustmentSuggestion> Suggestions,
    bool HasUrgentSuggestions,
    decimal TotalPotentialSavings
)
{
    public IEnumerable<AdjustmentSuggestion> Suggestions { get; init; } = Suggestions ?? Array.Empty<AdjustmentSuggestion>();
}

/// <summary>
/// A specific actionable suggestion.
/// </summary>
public record AdjustmentSuggestion(
    string Id,
    SuggestionCategory Category,
    string Title,
    string Description,
    SuggestionPriority Priority,
    decimal? PotentialSavings,
    SuggestionActionType ActionType,
    string? ActionTarget,
    IEnumerable<string> ImpactOnGoals
)
{
    public IEnumerable<string> ImpactOnGoals { get; init; } = ImpactOnGoals ?? Array.Empty<string>();
}

/// <summary>
/// Category of suggestion.
/// </summary>
public enum SuggestionCategory
{
    ReduceSpending,
    IncreaseContribution,
    Emergency,
    Warning,
    Optimization,
    Positive
}

/// <summary>
/// Priority level for suggestions.
/// </summary>
public enum SuggestionPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Type of action to take for the suggestion.
/// </summary>
public enum SuggestionActionType
{
    None,
    ReduceBudgetCategory,
    IncreaseGoalContribution,
    ReviewBudgets,
    IncreaseBuffer,
    Monitor
}
