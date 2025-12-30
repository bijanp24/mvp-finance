using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;
using FinanceEngine.Services;

namespace FinanceEngine.Calculators;

/// <summary>
/// Analyzes budget spending and calculates goal impact from overspending.
/// </summary>
public static class BudgetAnalysisCalculator
{
    /// <summary>
    /// Analyzes budgets for overspending and calculates goal impact.
    /// </summary>
    public static BudgetAnalysisResult Analyze(BudgetAnalysisInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        var overspentCategories = new List<BudgetOverspend>();
        decimal totalOverspend = 0m;

        foreach (var budget in input.Budgets)
        {
            // Calculate budget amount for the analysis period
            var periodBudget = CalculatePeriodBudget(budget, input.PeriodDays);
            var overspendAmount = Math.Max(0, budget.SpentThisPeriod - periodBudget);

            if (overspendAmount > 0)
            {
                totalOverspend += overspendAmount;

                // Calculate impact on goals
                var goalImpacts = CalculateGoalImpacts(overspendAmount, input.Goals);

                overspentCategories.Add(new BudgetOverspend(
                    CategoryId: budget.CategoryId,
                    CategoryName: budget.CategoryName,
                    BudgetAmount: periodBudget,
                    SpentAmount: budget.SpentThisPeriod,
                    OverspendAmount: overspendAmount,
                    GoalImpacts: goalImpacts
                ));
            }
        }

        // Calculate overall goal impact from total overspend
        var overallGoalImpacts = CalculateGoalImpacts(totalOverspend, input.Goals);

        return new BudgetAnalysisResult(
            OverspentCategories: overspentCategories,
            TotalOverspend: totalOverspend,
            OverallGoalImpacts: overallGoalImpacts,
            HasOverspending: totalOverspend > 0
        );
    }

    private static decimal CalculatePeriodBudget(BudgetInfo budget, int periodDays)
    {
        var dailyRate = budget.Frequency switch
        {
            BudgetFrequency.Weekly => budget.MonthlyAmount / 7m,
            BudgetFrequency.BiWeekly => budget.MonthlyAmount / 14m,
            BudgetFrequency.Monthly => budget.MonthlyAmount / 30m,
            _ => budget.MonthlyAmount / 30m
        };

        return dailyRate * periodDays;
    }

    private static List<GoalImpact> CalculateGoalImpacts(decimal overspendAmount, IEnumerable<GoalInfo> goals)
    {
        var impacts = new List<GoalImpact>();

        foreach (var goal in goals)
        {
            if (goal.RequiredMonthlyContribution <= 0)
                continue;

            // Calculate how many months of contributions the overspend represents
            var monthsImpact = overspendAmount / goal.RequiredMonthlyContribution;
            var delayedMonths = monthsImpact > 0.1m ? (int)Math.Ceiling(monthsImpact) : (int?)null;

            var impactMessage = delayedMonths.HasValue && delayedMonths.Value > 0
                ? $"Overspending could delay {goal.GoalName} by ~{delayedMonths} month(s)"
                : $"{goal.GoalName}: Minimal impact from current overspending";

            impacts.Add(new GoalImpact(
                GoalId: goal.GoalId,
                GoalName: goal.GoalName,
                GoalType: goal.GoalType.ToString(),
                CurrentStatus: goal.Status.ToString(),
                RequiredMonthlyContribution: goal.RequiredMonthlyContribution,
                CurrentMonthlyContribution: goal.CurrentMonthlyContribution,
                ContributionGap: goal.RequiredMonthlyContribution - goal.CurrentMonthlyContribution,
                DelayedMonths: delayedMonths,
                ImpactMessage: impactMessage
            ));
        }

        return impacts;
    }
}

/// <summary>
/// Input for budget analysis.
/// </summary>
public record BudgetAnalysisInput(
    IEnumerable<BudgetInfo> Budgets,
    IEnumerable<GoalInfo> Goals,
    int PeriodDays = 30
)
{
    public IEnumerable<BudgetInfo> Budgets { get; init; } = Budgets ?? Array.Empty<BudgetInfo>();
    public IEnumerable<GoalInfo> Goals { get; init; } = Goals ?? Array.Empty<GoalInfo>();
}

/// <summary>
/// Result of budget analysis.
/// </summary>
public record BudgetAnalysisResult(
    IEnumerable<BudgetOverspend> OverspentCategories,
    decimal TotalOverspend,
    IEnumerable<GoalImpact> OverallGoalImpacts,
    bool HasOverspending
)
{
    public IEnumerable<BudgetOverspend> OverspentCategories { get; init; } = OverspentCategories ?? Array.Empty<BudgetOverspend>();
    public IEnumerable<GoalImpact> OverallGoalImpacts { get; init; } = OverallGoalImpacts ?? Array.Empty<GoalImpact>();
}
