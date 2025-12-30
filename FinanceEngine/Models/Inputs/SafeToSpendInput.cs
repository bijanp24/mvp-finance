using FinanceEngine.Services;

namespace FinanceEngine.Models.Inputs;

/// <summary>
/// Time horizon options for safe-to-spend calculation.
/// </summary>
public enum TimeHorizon
{
    NextPaycheck,       // Calculate until next income event
    CurrentMonth,       // Calculate for remainder of current calendar month
    RollingTwoWeeks     // Calculate for fixed 14-day rolling window
}

/// <summary>
/// Budget frequency for calculating upcoming bills.
/// </summary>
public enum BudgetFrequency
{
    Monthly,
    BiWeekly,
    Weekly
}

/// <summary>
/// Input for dynamic safe-to-spend calculation.
/// </summary>
public record SafeToSpendInput(
    decimal AvailableCash,
    DateTime CalculationDate,
    TimeHorizon TimeHorizon,
    IEnumerable<BudgetInfo> Budgets,
    IEnumerable<GoalInfo> Goals,
    IEnumerable<IncomeEvent> UpcomingIncome,
    decimal MinimumBuffer = 0m,
    DateTime? NextPaycheckDate = null
)
{
    public IEnumerable<BudgetInfo> Budgets { get; init; } = Budgets ?? Array.Empty<BudgetInfo>();
    public IEnumerable<GoalInfo> Goals { get; init; } = Goals ?? Array.Empty<GoalInfo>();
    public IEnumerable<IncomeEvent> UpcomingIncome { get; init; } = UpcomingIncome ?? Array.Empty<IncomeEvent>();
}

/// <summary>
/// Budget information for safe-to-spend calculation.
/// </summary>
public record BudgetInfo(
    int CategoryId,
    string CategoryName,
    decimal MonthlyAmount,
    BudgetFrequency Frequency,
    decimal SpentThisPeriod
);

/// <summary>
/// Goal information with required contribution for safe-to-spend calculation.
/// </summary>
public record GoalInfo(
    int GoalId,
    string GoalName,
    GoalType GoalType,
    decimal RequiredMonthlyContribution,
    decimal CurrentMonthlyContribution,
    GoalStatus Status
);
