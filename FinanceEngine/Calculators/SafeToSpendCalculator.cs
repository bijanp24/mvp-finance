using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;
using FinanceEngine.Services;

namespace FinanceEngine.Calculators;

/// <summary>
/// Calculates dynamic safe-to-spend amount based on goals, budgets, and time horizon.
/// Formula: SafeToSpend = AvailableCash - UpcomingBills - RequiredGoalContributions - Buffer
/// </summary>
public static class SafeToSpendCalculator
{
    private const decimal HealthyThreshold = 0.20m;  // >20% of available cash = Healthy

    /// <summary>
    /// Calculates dynamic safe-to-spend amount.
    /// </summary>
    public static SafeToSpendResult Calculate(SafeToSpendInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.AvailableCash < 0)
            throw new ArgumentException("Available cash cannot be negative.", nameof(input.AvailableCash));

        // Calculate time horizon end date
        var horizonEndDate = CalculateHorizonEndDate(input);
        var daysInHorizon = Math.Max(1, (int)(horizonEndDate - input.CalculationDate).TotalDays);

        // Calculate upcoming bills within horizon
        var upcomingBills = CalculateUpcomingBills(input.Budgets, input.CalculationDate, horizonEndDate);

        // Calculate required goal contributions for the horizon period
        var requiredGoalContributions = CalculateRequiredGoalContributions(input.Goals, daysInHorizon);

        // Calculate safe-to-spend
        var safeToSpend = input.AvailableCash - upcomingBills - requiredGoalContributions - input.MinimumBuffer;

        // Determine status
        var (status, goalImpacts) = DetermineStatus(input, safeToSpend, requiredGoalContributions);

        // Generate status message
        var statusMessage = GenerateStatusMessage(status, safeToSpend, input.AvailableCash, goalImpacts);

        // Build breakdown
        var breakdown = new SafeToSpendBreakdown(
            AvailableCash: input.AvailableCash,
            UpcomingBills: upcomingBills,
            RequiredGoalContributions: requiredGoalContributions,
            MinimumBuffer: input.MinimumBuffer,
            DaysInHorizon: daysInHorizon
        );

        return new SafeToSpendResult(
            SafeToSpend: safeToSpend,
            Status: status,
            Breakdown: breakdown,
            GoalImpacts: goalImpacts,
            StatusMessage: statusMessage,
            HorizonEndDate: horizonEndDate
        );
    }

    /// <summary>
    /// Calculates the end date based on the selected time horizon.
    /// </summary>
    private static DateTime CalculateHorizonEndDate(SafeToSpendInput input)
    {
        return input.TimeHorizon switch
        {
            TimeHorizon.NextPaycheck => CalculateNextPaycheckDate(input),
            TimeHorizon.CurrentMonth => new DateTime(input.CalculationDate.Year, input.CalculationDate.Month, 1)
                .AddMonths(1)
                .AddDays(-1), // Last day of current month
            TimeHorizon.RollingTwoWeeks => input.CalculationDate.AddDays(14),
            _ => input.CalculationDate.AddDays(14) // Default to 2 weeks
        };
    }

    /// <summary>
    /// Finds the next paycheck date from income events or uses provided date.
    /// </summary>
    private static DateTime CalculateNextPaycheckDate(SafeToSpendInput input)
    {
        // Use explicitly provided date if available
        if (input.NextPaycheckDate.HasValue && input.NextPaycheckDate.Value > input.CalculationDate)
            return input.NextPaycheckDate.Value;

        // Find next income event
        var nextIncome = input.UpcomingIncome
            .Where(i => i.Date > input.CalculationDate)
            .OrderBy(i => i.Date)
            .FirstOrDefault();

        if (nextIncome != null)
            return nextIncome.Date;

        // Default to 2 weeks if no income found
        return input.CalculationDate.AddDays(14);
    }

    /// <summary>
    /// Calculates total upcoming bills within the time horizon.
    /// </summary>
    private static decimal CalculateUpcomingBills(IEnumerable<BudgetInfo> budgets, DateTime start, DateTime end)
    {
        var totalDays = (end - start).TotalDays;
        if (totalDays <= 0) return 0m;

        decimal total = 0m;

        foreach (var budget in budgets)
        {
            // Convert to daily rate based on frequency
            var dailyRate = budget.Frequency switch
            {
                BudgetFrequency.Weekly => budget.MonthlyAmount / 7m,
                BudgetFrequency.BiWeekly => budget.MonthlyAmount / 14m,
                BudgetFrequency.Monthly => budget.MonthlyAmount / 30m,
                _ => budget.MonthlyAmount / 30m
            };

            // Calculate amount for the horizon period
            var periodAmount = dailyRate * (decimal)totalDays;

            // Subtract what's already been spent
            var remaining = Math.Max(0, periodAmount - budget.SpentThisPeriod);
            total += remaining;
        }

        return total;
    }

    /// <summary>
    /// Calculates required goal contributions for the time period.
    /// </summary>
    private static decimal CalculateRequiredGoalContributions(IEnumerable<GoalInfo> goals, int daysInHorizon)
    {
        decimal total = 0m;
        var monthFraction = daysInHorizon / 30.0m;

        foreach (var goal in goals)
        {
            // Only count active goals that need contributions
            if (goal.RequiredMonthlyContribution > 0)
            {
                var periodContribution = goal.RequiredMonthlyContribution * monthFraction;
                total += periodContribution;
            }
        }

        return total;
    }

    /// <summary>
    /// Determines the status and calculates goal impacts.
    /// </summary>
    private static (SafeToSpendStatus status, List<GoalImpact> impacts) DetermineStatus(
        SafeToSpendInput input,
        decimal safeToSpend,
        decimal requiredGoalContributions)
    {
        var goalImpacts = new List<GoalImpact>();
        var hasGoalsBehind = false;
        var hasGoalsAtRisk = false;

        foreach (var goal in input.Goals)
        {
            var contributionGap = goal.RequiredMonthlyContribution - goal.CurrentMonthlyContribution;
            int? delayedMonths = null;

            // Calculate delay if underfunded
            if (contributionGap > 0 && goal.RequiredMonthlyContribution > 0)
            {
                // Rough estimate: each month of underfunding delays by proportional amount
                var underfundingRatio = contributionGap / goal.RequiredMonthlyContribution;
                delayedMonths = (int)Math.Ceiling(underfundingRatio * 12); // Months delayed over a year
            }

            var impactMessage = GenerateGoalImpactMessage(goal, contributionGap, delayedMonths);

            goalImpacts.Add(new GoalImpact(
                GoalId: goal.GoalId,
                GoalName: goal.GoalName,
                GoalType: goal.GoalType.ToString(),
                CurrentStatus: goal.Status.ToString(),
                RequiredMonthlyContribution: goal.RequiredMonthlyContribution,
                CurrentMonthlyContribution: goal.CurrentMonthlyContribution,
                ContributionGap: contributionGap,
                DelayedMonths: delayedMonths,
                ImpactMessage: impactMessage
            ));

            if (goal.Status == GoalStatus.Behind)
                hasGoalsBehind = true;
            else if (goal.Status == GoalStatus.AtRisk)
                hasGoalsAtRisk = true;
        }

        // Determine overall status
        SafeToSpendStatus status;
        if (hasGoalsBehind)
        {
            status = SafeToSpendStatus.Behind;
        }
        else if (safeToSpend <= 0 || hasGoalsAtRisk)
        {
            status = SafeToSpendStatus.AtRisk;
        }
        else if (input.AvailableCash > 0 && safeToSpend / input.AvailableCash > HealthyThreshold)
        {
            status = SafeToSpendStatus.Healthy;
        }
        else
        {
            status = SafeToSpendStatus.Tight;
        }

        return (status, goalImpacts);
    }

    /// <summary>
    /// Generates an impact message for a specific goal.
    /// </summary>
    private static string GenerateGoalImpactMessage(GoalInfo goal, decimal gap, int? delayedMonths)
    {
        if (gap <= 0)
            return $"{goal.GoalName}: On track with current contributions.";

        if (delayedMonths.HasValue && delayedMonths.Value > 0)
            return $"{goal.GoalName}: Underfunded by ${gap:N0}/month. Could delay goal by ~{delayedMonths} months.";

        return $"{goal.GoalName}: Needs ${gap:N0}/month more to stay on track.";
    }

    /// <summary>
    /// Generates the overall status message.
    /// </summary>
    private static string GenerateStatusMessage(
        SafeToSpendStatus status,
        decimal safeToSpend,
        decimal availableCash,
        List<GoalImpact> goalImpacts)
    {
        var goalsAtRiskCount = goalImpacts.Count(g => g.CurrentStatus == "AtRisk" || g.CurrentStatus == "Behind");

        return status switch
        {
            SafeToSpendStatus.Healthy => $"You have ${safeToSpend:N0} available to spend while staying on track for all goals.",
            SafeToSpendStatus.Tight => $"You have ${safeToSpend:N0} available, but it's tight. Consider reducing discretionary spending.",
            SafeToSpendStatus.AtRisk => safeToSpend <= 0
                ? $"Warning: No safe spending room. You're ${Math.Abs(safeToSpend):N0} short of covering obligations and goals."
                : $"Caution: {goalsAtRiskCount} goal(s) at risk. Review your budget allocations.",
            SafeToSpendStatus.Behind => $"Action needed: {goalsAtRiskCount} goal(s) behind schedule. Adjustments recommended.",
            _ => "Unable to determine status."
        };
    }
}
