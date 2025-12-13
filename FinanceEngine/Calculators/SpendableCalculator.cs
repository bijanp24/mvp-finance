using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Calculators;

public static class SpendableCalculator
{
    private const decimal ConservativeMultiplier = 1.5m; // 50% buffer increase for conservative scenario

    public static SpendableResult Calculate(SpendableInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.AvailableCash < 0)
            throw new ArgumentException("Available cash cannot be negative.", nameof(input.AvailableCash));

        // Find next paycheck
        var nextPaycheck = input.UpcomingIncome
            .Where(i => i.Date > input.CalculationDate)
            .OrderBy(i => i.Date)
            .FirstOrDefault();

        var nextPaycheckDate = nextPaycheck?.Date;
        var daysUntilPaycheck = nextPaycheckDate.HasValue
            ? (int)(nextPaycheckDate.Value - input.CalculationDate).TotalDays
            : 0;

        // Filter obligations due before next paycheck (or all if no paycheck)
        var relevantObligations = nextPaycheckDate.HasValue
            ? input.UpcomingObligations.Where(o => o.DueDate > input.CalculationDate && o.DueDate <= nextPaycheckDate.Value)
            : input.UpcomingObligations.Where(o => o.DueDate > input.CalculationDate);

        var totalObligations = relevantObligations.Sum(o => o.Amount);

        // Filter planned contributions due before next paycheck
        var relevantContributions = nextPaycheckDate.HasValue
            ? input.PlannedContributions.Where(c => c.DueDate > input.CalculationDate && c.DueDate <= nextPaycheckDate.Value)
            : input.PlannedContributions.Where(c => c.DueDate > input.CalculationDate);

        var totalContributions = relevantContributions.Sum(c => c.Amount);

        // Calculate safety buffer
        var safetyBuffer = CalculateSafetyBuffer(input, daysUntilPaycheck);

        // Calculate spendable amount
        var spendableNow = input.AvailableCash - totalObligations - safetyBuffer - totalContributions;

        // Project expected cash at next paycheck
        var estimatedSpending = input.EstimatedDailySpend.HasValue && daysUntilPaycheck > 0
            ? input.EstimatedDailySpend.Value * daysUntilPaycheck
            : 0m;

        var expectedCashAtPaycheck = input.AvailableCash
            - totalObligations
            - totalContributions
            - estimatedSpending
            + (nextPaycheck?.Amount ?? 0m);

        // Build breakdown
        var breakdown = new SpendableBreakdown(
            AvailableCash: input.AvailableCash,
            TotalObligations: totalObligations,
            SafetyBuffer: safetyBuffer,
            PlannedContributions: totalContributions,
            DaysUntilNextPaycheck: daysUntilPaycheck
        );

        // Calculate conservative scenario if we have daily spend estimate
        SpendableScenario? conservativeScenario = null;
        if (input.EstimatedDailySpend.HasValue && daysUntilPaycheck > 0)
        {
            var conservativeBuffer = safetyBuffer * ConservativeMultiplier;
            var conservativeSpendable = input.AvailableCash - totalObligations - conservativeBuffer - totalContributions;
            var conservativeDailySpend = input.EstimatedDailySpend.Value * ConservativeMultiplier;
            var conservativeProjectedSpending = conservativeDailySpend * daysUntilPaycheck;
            var conservativeExpectedCash = input.AvailableCash
                - totalObligations
                - totalContributions
                - conservativeProjectedSpending
                + (nextPaycheck?.Amount ?? 0m);

            conservativeScenario = new SpendableScenario(
                ScenarioName: "Conservative",
                EstimatedDailySpend: conservativeDailySpend,
                SpendableAmount: conservativeSpendable,
                ExpectedCashAtPaycheck: conservativeExpectedCash
            );
        }

        return new SpendableResult(
            SpendableNow: spendableNow,
            ExpectedCashAtNextPaycheck: expectedCashAtPaycheck,
            NextPaycheckDate: nextPaycheckDate,
            Breakdown: breakdown,
            ConservativeScenario: conservativeScenario
        );
    }

    private static decimal CalculateSafetyBuffer(SpendableInput input, int daysUntilPaycheck)
    {
        // Use manual buffer if provided
        if (input.ManualSafetyBuffer.HasValue)
            return input.ManualSafetyBuffer.Value;

        // Calculate from estimated daily spend if available
        if (input.EstimatedDailySpend.HasValue && daysUntilPaycheck > 0)
            return input.EstimatedDailySpend.Value * daysUntilPaycheck;

        // No buffer if neither is specified
        return 0m;
    }
}
