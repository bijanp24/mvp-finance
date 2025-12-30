namespace FinanceEngine.Models.Outputs;

/// <summary>
/// Status of financial health for safe-to-spend calculation.
/// </summary>
public enum SafeToSpendStatus
{
    Healthy,    // On track for all goals with spending room (>20% available)
    Tight,      // On track but little discretionary room (0-20% available)
    AtRisk,     // SafeToSpend <= 0 OR some goals at risk
    Behind      // Goals already behind schedule
}

/// <summary>
/// Result of dynamic safe-to-spend calculation.
/// </summary>
public record SafeToSpendResult(
    decimal SafeToSpend,
    SafeToSpendStatus Status,
    SafeToSpendBreakdown Breakdown,
    IEnumerable<GoalImpact> GoalImpacts,
    string StatusMessage,
    DateTime HorizonEndDate
)
{
    public IEnumerable<GoalImpact> GoalImpacts { get; init; } = GoalImpacts ?? Array.Empty<GoalImpact>();
}

/// <summary>
/// Breakdown of safe-to-spend calculation components.
/// </summary>
public record SafeToSpendBreakdown(
    decimal AvailableCash,
    decimal UpcomingBills,
    decimal RequiredGoalContributions,
    decimal MinimumBuffer,
    int DaysInHorizon
);

/// <summary>
/// Impact on a specific goal if current spending continues.
/// </summary>
public record GoalImpact(
    int GoalId,
    string GoalName,
    string GoalType,
    string CurrentStatus,
    decimal RequiredMonthlyContribution,
    decimal CurrentMonthlyContribution,
    decimal ContributionGap,
    int? DelayedMonths,  // How many months goal would be delayed if underfunded
    string ImpactMessage
);

/// <summary>
/// Information about overspending in a budget category.
/// </summary>
public record BudgetOverspend(
    int CategoryId,
    string CategoryName,
    decimal BudgetAmount,
    decimal SpentAmount,
    decimal OverspendAmount,
    IEnumerable<GoalImpact> GoalImpacts
)
{
    public IEnumerable<GoalImpact> GoalImpacts { get; init; } = GoalImpacts ?? Array.Empty<GoalImpact>();
}
