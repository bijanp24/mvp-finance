namespace FinanceEngine.Services;

/// <summary>
/// Goal types for financial goal tracking.
/// </summary>
public enum GoalType
{
    DebtFree,           // Pay off all linked debt by target date
    InvestmentTarget,   // Reach $X in linked investment accounts
    SavingsGoal,        // Save $X for specific purpose (short-term)
    NetWorthMilestone   // Hit net worth target (assets - liabilities)
}

/// <summary>
/// Status of goal progress relative to target.
/// </summary>
public enum GoalStatus
{
    OnTrack,    // Current pace meets or exceeds target
    Ahead,      // Significantly ahead of schedule (>10% ahead)
    AtRisk,     // Slightly behind (<10% behind)
    Behind      // Off track (>10% behind), needs intervention
}

/// <summary>
/// Input for goal progress calculation.
/// </summary>
public record GoalProgressInput(
    GoalType Type,
    decimal? TargetAmount,              // Null for DebtFree (target is $0)
    DateTime TargetDate,
    decimal CurrentLinkedBalance,       // Current total of linked accounts
    decimal? MonthlyContributionRate,   // Optional: current monthly contribution rate
    DateTime CalculationDate            // Date to calculate progress from
);

/// <summary>
/// Result of goal progress calculation.
/// </summary>
public record GoalProgressResult(
    decimal CurrentValue,
    decimal TargetValue,
    decimal ProgressPercentage,         // 0-100+ (can exceed 100 if ahead)
    decimal RequiredMonthlyAmount,      // Amount needed per month to hit target
    DateTime? ProjectedCompletionDate,  // When goal will be reached at current pace
    GoalStatus Status,
    int MonthsRemaining,
    decimal AmountRemaining,            // How much more is needed (positive) or excess (negative)
    string StatusMessage
);

/// <summary>
/// Pure calculation service for computing goal progress.
/// </summary>
public static class GoalProgressCalculator
{
    private const decimal AheadThreshold = 0.10m;   // 10% ahead = Ahead status
    private const decimal AtRiskThreshold = 0.10m;  // Within 10% = AtRisk, beyond = Behind

    /// <summary>
    /// Calculates goal progress and projections.
    /// </summary>
    public static GoalProgressResult Calculate(GoalProgressInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.TargetDate < input.CalculationDate)
        {
            // Goal date has passed
            return CalculateExpiredGoal(input);
        }

        return input.Type switch
        {
            GoalType.DebtFree => CalculateDebtFreeGoal(input),
            GoalType.InvestmentTarget => CalculateInvestmentGoal(input),
            GoalType.SavingsGoal => CalculateSavingsGoal(input),
            GoalType.NetWorthMilestone => CalculateNetWorthGoal(input),
            _ => throw new ArgumentException($"Unknown goal type: {input.Type}")
        };
    }

    private static GoalProgressResult CalculateDebtFreeGoal(GoalProgressInput input)
    {
        // For debt-free: CurrentLinkedBalance is current debt, target is 0
        var currentDebt = input.CurrentLinkedBalance;
        var targetValue = 0m;
        var amountRemaining = currentDebt; // Need to pay off this much

        var monthsRemaining = CalculateMonthsRemaining(input.CalculationDate, input.TargetDate);
        var requiredMonthly = monthsRemaining > 0 ? amountRemaining / monthsRemaining : amountRemaining;

        // Progress: 100% when debt is 0, 0% when at original debt level
        // We assume original debt was higher, so use current as basis
        var progressPercentage = currentDebt <= 0 ? 100m : 0m;

        // Calculate projected completion based on current payment rate
        DateTime? projectedCompletion = null;
        if (input.MonthlyContributionRate.HasValue && input.MonthlyContributionRate.Value > 0)
        {
            var monthsToPayoff = currentDebt / input.MonthlyContributionRate.Value;
            projectedCompletion = input.CalculationDate.AddMonths((int)Math.Ceiling(monthsToPayoff));
        }

        var status = DetermineDebtFreeStatus(input, requiredMonthly, projectedCompletion);
        var statusMessage = GenerateDebtFreeMessage(status, currentDebt, requiredMonthly, monthsRemaining);

        return new GoalProgressResult(
            CurrentValue: currentDebt,
            TargetValue: targetValue,
            ProgressPercentage: progressPercentage,
            RequiredMonthlyAmount: requiredMonthly,
            ProjectedCompletionDate: projectedCompletion,
            Status: status,
            MonthsRemaining: monthsRemaining,
            AmountRemaining: amountRemaining,
            StatusMessage: statusMessage
        );
    }

    private static GoalProgressResult CalculateInvestmentGoal(GoalProgressInput input)
    {
        return CalculateGrowthGoal(input, "investment");
    }

    private static GoalProgressResult CalculateSavingsGoal(GoalProgressInput input)
    {
        return CalculateGrowthGoal(input, "savings");
    }

    private static GoalProgressResult CalculateGrowthGoal(GoalProgressInput input, string goalName)
    {
        var currentValue = input.CurrentLinkedBalance;
        var targetValue = input.TargetAmount ?? 0m;
        var amountRemaining = targetValue - currentValue;

        var progressPercentage = targetValue > 0
            ? Math.Round((currentValue / targetValue) * 100m, 1)
            : (currentValue >= 0 ? 100m : 0m);

        var monthsRemaining = CalculateMonthsRemaining(input.CalculationDate, input.TargetDate);
        var requiredMonthly = monthsRemaining > 0 && amountRemaining > 0
            ? amountRemaining / monthsRemaining
            : 0m;

        // Calculate projected completion
        DateTime? projectedCompletion = null;
        if (input.MonthlyContributionRate.HasValue && input.MonthlyContributionRate.Value > 0 && amountRemaining > 0)
        {
            var monthsToComplete = amountRemaining / input.MonthlyContributionRate.Value;
            projectedCompletion = input.CalculationDate.AddMonths((int)Math.Ceiling(monthsToComplete));
        }
        else if (amountRemaining <= 0)
        {
            projectedCompletion = input.CalculationDate; // Already achieved
        }

        var status = DetermineGrowthStatus(input, currentValue, targetValue, monthsRemaining, projectedCompletion);
        var statusMessage = GenerateGrowthMessage(status, goalName, currentValue, targetValue, requiredMonthly, monthsRemaining);

        return new GoalProgressResult(
            CurrentValue: currentValue,
            TargetValue: targetValue,
            ProgressPercentage: progressPercentage,
            RequiredMonthlyAmount: requiredMonthly,
            ProjectedCompletionDate: projectedCompletion,
            Status: status,
            MonthsRemaining: monthsRemaining,
            AmountRemaining: amountRemaining,
            StatusMessage: statusMessage
        );
    }

    private static GoalProgressResult CalculateNetWorthGoal(GoalProgressInput input)
    {
        // CurrentLinkedBalance represents current net worth (can be negative)
        var currentNetWorth = input.CurrentLinkedBalance;
        var targetNetWorth = input.TargetAmount ?? 0m;
        var amountRemaining = targetNetWorth - currentNetWorth;

        // For net worth, progress can be negative (if net worth is negative)
        var progressPercentage = targetNetWorth != 0
            ? Math.Round((currentNetWorth / targetNetWorth) * 100m, 1)
            : (currentNetWorth >= 0 ? 100m : 0m);

        var monthsRemaining = CalculateMonthsRemaining(input.CalculationDate, input.TargetDate);
        var requiredMonthly = monthsRemaining > 0 && amountRemaining > 0
            ? amountRemaining / monthsRemaining
            : 0m;

        DateTime? projectedCompletion = null;
        if (input.MonthlyContributionRate.HasValue && input.MonthlyContributionRate.Value > 0 && amountRemaining > 0)
        {
            var monthsToComplete = amountRemaining / input.MonthlyContributionRate.Value;
            projectedCompletion = input.CalculationDate.AddMonths((int)Math.Ceiling(monthsToComplete));
        }
        else if (amountRemaining <= 0)
        {
            projectedCompletion = input.CalculationDate;
        }

        var status = DetermineGrowthStatus(input, currentNetWorth, targetNetWorth, monthsRemaining, projectedCompletion);
        var statusMessage = GenerateNetWorthMessage(status, currentNetWorth, targetNetWorth, requiredMonthly, monthsRemaining);

        return new GoalProgressResult(
            CurrentValue: currentNetWorth,
            TargetValue: targetNetWorth,
            ProgressPercentage: progressPercentage,
            RequiredMonthlyAmount: requiredMonthly,
            ProjectedCompletionDate: projectedCompletion,
            Status: status,
            MonthsRemaining: monthsRemaining,
            AmountRemaining: amountRemaining,
            StatusMessage: statusMessage
        );
    }

    private static GoalProgressResult CalculateExpiredGoal(GoalProgressInput input)
    {
        var currentValue = input.CurrentLinkedBalance;
        var targetValue = input.TargetAmount ?? 0m;

        bool goalMet = input.Type switch
        {
            GoalType.DebtFree => currentValue <= 0.01m,
            _ => currentValue >= targetValue
        };

        var status = goalMet ? GoalStatus.Ahead : GoalStatus.Behind;
        var progressPercentage = input.Type == GoalType.DebtFree
            ? (currentValue <= 0 ? 100m : 0m)
            : (targetValue > 0 ? Math.Round((currentValue / targetValue) * 100m, 1) : 100m);

        return new GoalProgressResult(
            CurrentValue: currentValue,
            TargetValue: targetValue,
            ProgressPercentage: progressPercentage,
            RequiredMonthlyAmount: 0m,
            ProjectedCompletionDate: null,
            Status: status,
            MonthsRemaining: 0,
            AmountRemaining: input.Type == GoalType.DebtFree ? currentValue : targetValue - currentValue,
            StatusMessage: goalMet ? "Goal achieved!" : "Goal deadline passed - not achieved"
        );
    }

    private static int CalculateMonthsRemaining(DateTime from, DateTime to)
    {
        var months = ((to.Year - from.Year) * 12) + to.Month - from.Month;
        return Math.Max(0, months);
    }

    private static GoalStatus DetermineDebtFreeStatus(
        GoalProgressInput input,
        decimal requiredMonthly,
        DateTime? projectedCompletion)
    {
        if (input.CurrentLinkedBalance <= 0.01m)
            return GoalStatus.Ahead; // Already debt-free

        if (!input.MonthlyContributionRate.HasValue || input.MonthlyContributionRate.Value <= 0)
            return GoalStatus.AtRisk; // No payment plan

        if (projectedCompletion.HasValue)
        {
            if (projectedCompletion.Value <= input.TargetDate)
            {
                // Will complete on or before target
                var daysAhead = (input.TargetDate - projectedCompletion.Value).TotalDays;
                var totalDays = (input.TargetDate - input.CalculationDate).TotalDays;
                if (totalDays > 0 && daysAhead / totalDays > (double)AheadThreshold)
                    return GoalStatus.Ahead;
                return GoalStatus.OnTrack;
            }
            else
            {
                // Will complete after target
                var daysLate = (projectedCompletion.Value - input.TargetDate).TotalDays;
                var totalDays = (input.TargetDate - input.CalculationDate).TotalDays;
                if (totalDays > 0 && daysLate / totalDays > (double)AtRiskThreshold)
                    return GoalStatus.Behind;
                return GoalStatus.AtRisk;
            }
        }

        return GoalStatus.AtRisk;
    }

    private static GoalStatus DetermineGrowthStatus(
        GoalProgressInput input,
        decimal currentValue,
        decimal targetValue,
        int monthsRemaining,
        DateTime? projectedCompletion)
    {
        if (currentValue >= targetValue)
            return GoalStatus.Ahead; // Already achieved

        if (monthsRemaining <= 0)
            return GoalStatus.Behind; // Past deadline

        // Calculate expected progress at this point
        // If target date is 12 months away and we're 3 months in, we should have ~25% progress
        var totalMonths = CalculateMonthsRemaining(input.CalculationDate.AddMonths(-monthsRemaining), input.TargetDate);
        if (totalMonths <= 0) totalMonths = monthsRemaining;

        var expectedProgress = 1.0m - ((decimal)monthsRemaining / totalMonths);
        var actualProgress = targetValue > 0 ? currentValue / targetValue : 1m;

        var progressDiff = actualProgress - expectedProgress;

        if (progressDiff >= AheadThreshold)
            return GoalStatus.Ahead;
        if (progressDiff >= -AtRiskThreshold)
            return GoalStatus.OnTrack;
        if (progressDiff >= -AtRiskThreshold * 2)
            return GoalStatus.AtRisk;
        return GoalStatus.Behind;
    }

    private static string GenerateDebtFreeMessage(GoalStatus status, decimal currentDebt, decimal requiredMonthly, int monthsRemaining)
    {
        return status switch
        {
            GoalStatus.Ahead => "Excellent! You're ahead of schedule on debt payoff.",
            GoalStatus.OnTrack => $"On track. Pay ${requiredMonthly:N0}/month to be debt-free in {monthsRemaining} months.",
            GoalStatus.AtRisk => $"Slightly behind. Consider increasing payments to ${requiredMonthly:N0}/month.",
            GoalStatus.Behind => $"Behind schedule. ${currentDebt:N0} remaining with {monthsRemaining} months left.",
            _ => "Unable to determine status."
        };
    }

    private static string GenerateGrowthMessage(GoalStatus status, string goalName, decimal current, decimal target, decimal requiredMonthly, int monthsRemaining)
    {
        var remaining = target - current;
        return status switch
        {
            GoalStatus.Ahead => $"Excellent! You've exceeded your {goalName} target.",
            GoalStatus.OnTrack => $"On track. Contribute ${requiredMonthly:N0}/month to reach ${target:N0} in {monthsRemaining} months.",
            GoalStatus.AtRisk => $"Slightly behind on {goalName} goal. ${remaining:N0} remaining.",
            GoalStatus.Behind => $"Behind on {goalName} goal. Need ${requiredMonthly:N0}/month to catch up.",
            _ => "Unable to determine status."
        };
    }

    private static string GenerateNetWorthMessage(GoalStatus status, decimal current, decimal target, decimal requiredMonthly, int monthsRemaining)
    {
        var remaining = target - current;
        return status switch
        {
            GoalStatus.Ahead => $"Excellent! Net worth ${current:N0} exceeds target of ${target:N0}.",
            GoalStatus.OnTrack => $"On track. Grow net worth by ${requiredMonthly:N0}/month to reach ${target:N0}.",
            GoalStatus.AtRisk => $"Slightly behind. ${remaining:N0} more needed in {monthsRemaining} months.",
            GoalStatus.Behind => $"Behind target. Current net worth ${current:N0}, target ${target:N0}.",
            _ => "Unable to determine status."
        };
    }
}
