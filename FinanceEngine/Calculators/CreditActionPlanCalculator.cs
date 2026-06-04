using System.Globalization;
using FinanceEngine.Models;

namespace FinanceEngine.Calculators;

/// <summary>
/// Builds a personalized debt-payoff action plan from a lump sum (a windfall).
/// It carves out an emergency reserve first - liquidity matters most when income
/// is uncertain - then allocates the remainder across debts by the chosen strategy
/// (avalanche by default) and quantifies the interest and time saved.
/// Pure and deterministic: no I/O, no persistence.
/// </summary>
public static class CreditActionPlanCalculator
{
    private const int MaxMonths = 600; // 50 years - a balance still open here effectively never pays off

    public static CreditActionPlanResult Calculate(CreditActionPlanInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Debts);

        if (input.Windfall < 0)
            throw new ArgumentException("Windfall cannot be negative.", nameof(input));
        if (input.MonthlyEssentialExpenses < 0)
            throw new ArgumentException("Monthly essential expenses cannot be negative.", nameof(input));
        if (input.EmergencyFundMonths < 0)
            throw new ArgumentException("Emergency fund months cannot be negative.", nameof(input));

        var debts = input.Debts.Where(d => d.Balance > 0).ToList();
        foreach (var d in debts)
        {
            if (d.AnnualPercentageRate < 0)
                throw new ArgumentException($"APR cannot be negative: {d.Name}");
            if (d.MinimumPayment < 0)
                throw new ArgumentException($"Minimum payment cannot be negative: {d.Name}");
        }

        // 1. Emergency reserve comes first. With no/low income you cannot afford to
        //    sink every dollar into debt and then have nothing liquid for a surprise.
        var emergencyTarget = input.MonthlyEssentialExpenses * input.EmergencyFundMonths;
        var emergencyReserved = Math.Min(input.Windfall, emergencyTarget);
        var available = input.Windfall - emergencyReserved;

        // 2. Order debts by the chosen strategy.
        var ordered = OrderDebts(debts, input.Strategy);

        // 3. Walk the ordered debts, applying the available windfall to each in turn.
        var steps = new List<DebtActionStep>();
        var remaining = available;
        var order = 1;
        foreach (var debt in ordered)
        {
            var apr = debt.EffectiveAPR;
            var monthlyRate = apr / 12m;

            var applied = Math.Min(remaining, debt.Balance);
            var balanceAfter = debt.Balance - applied;
            remaining -= applied;

            var (monthsBefore, interestBefore) = SimulatePayoff(debt.Balance, monthlyRate, debt.MinimumPayment);
            var (monthsAfter, interestAfter) = SimulatePayoff(balanceAfter, monthlyRate, debt.MinimumPayment);

            steps.Add(new DebtActionStep(
                Order: order++,
                DebtName: debt.Name,
                EffectiveAPR: apr,
                StartingBalance: debt.Balance,
                MinimumPayment: debt.MinimumPayment,
                LumpSumApplied: applied,
                BalanceAfterLumpSum: balanceAfter,
                IsFullyPaid: balanceAfter <= 0.005m,
                MonthsToPayoffBefore: monthsBefore,
                MonthsToPayoffAfter: monthsAfter,
                InterestBefore: interestBefore,
                InterestAfter: interestAfter,
                InterestSaved: interestBefore - interestAfter
            ));
        }

        var totalBefore = debts.Sum(d => d.Balance);
        var appliedToDebt = available - remaining;
        var totalAfter = totalBefore - appliedToDebt;
        var totalInterestSaved = steps.Sum(s => s.InterestSaved);

        // Portfolio is debt-free when the slowest remaining card is paid off.
        var monthsBeforeAll = steps.Count > 0 ? steps.Max(s => s.MonthsToPayoffBefore ?? MaxMonths) : 0;
        var monthsAfterAll = steps.Count > 0 ? steps.Max(s => s.MonthsToPayoffAfter ?? MaxMonths) : 0;

        var recommendations = BuildRecommendations(
            input, emergencyTarget, emergencyReserved, steps, remaining, totalInterestSaved);

        return new CreditActionPlanResult(
            Strategy: input.Strategy,
            EmergencyFundTarget: emergencyTarget,
            EmergencyFundReserved: emergencyReserved,
            IsEmergencyFundFunded: emergencyTarget > 0 && emergencyReserved >= emergencyTarget - 0.005m,
            MonthsOfExpensesCovered: input.MonthlyEssentialExpenses > 0
                ? emergencyReserved / input.MonthlyEssentialExpenses
                : 0m,
            WindfallTotal: input.Windfall,
            WindfallToDebt: appliedToDebt,
            WindfallRemaining: remaining, // left over after the reserve and clearing all debt
            TotalDebtBefore: totalBefore,
            TotalDebtAfter: totalAfter,
            TotalInterestSaved: totalInterestSaved,
            MonthsToDebtFreeBefore: monthsBeforeAll,
            MonthsToDebtFreeAfter: monthsAfterAll,
            Steps: steps,
            Recommendations: recommendations
        );
    }

    private static List<Debt> OrderDebts(List<Debt> debts, AllocationStrategy strategy) => strategy switch
    {
        // Smallest balance first - the "quick wins" approach.
        AllocationStrategy.Snowball => debts.OrderBy(d => d.Balance).ThenByDescending(d => d.EffectiveAPR).ToList(),
        // Avalanche / Hybrid: highest effective APR first - mathematically cheapest.
        _ => debts.OrderByDescending(d => d.EffectiveAPR).ThenByDescending(d => d.Balance).ToList(),
    };

    /// <summary>
    /// Simulates paying off a single balance with a fixed monthly payment.
    /// Returns (months, totalInterest). Months is null when the payment never
    /// covers the monthly interest, so the balance never reaches zero.
    /// </summary>
    private static (int? Months, decimal Interest) SimulatePayoff(decimal balance, decimal monthlyRate, decimal payment)
    {
        if (balance <= 0) return (0, 0m);
        if (payment <= 0) return (null, 0m);

        var totalInterest = 0m;
        var months = 0;
        while (balance > 0 && months < MaxMonths)
        {
            var interest = balance * monthlyRate;
            var principal = payment - interest;
            if (principal <= 0) return (null, totalInterest); // payment can't cover interest
            balance -= principal;
            totalInterest += interest;
            months++;
        }

        return balance <= 0 ? (months, totalInterest) : (null, totalInterest);
    }

    private static List<string> BuildRecommendations(
        CreditActionPlanInput input,
        decimal emergencyTarget,
        decimal emergencyReserved,
        List<DebtActionStep> steps,
        decimal leftover,
        decimal totalInterestSaved)
    {
        var recs = new List<string>();

        if (input.MonthlyIncome <= 0 && emergencyTarget > 0)
        {
            recs.Add($"You currently have no monthly income, so keep a {input.EmergencyFundMonths}-month reserve "
                + $"of {Money(emergencyTarget)} liquid before accelerating any debt payoff.");
        }

        if (emergencyTarget > 0 && emergencyReserved < emergencyTarget - 0.005m)
        {
            recs.Add($"The windfall only covers {Money(emergencyReserved)} of the {Money(emergencyTarget)} "
                + "emergency target. Prioritize fully funding the reserve before paying extra toward debt.");
        }

        var paid = steps.Where(s => s.LumpSumApplied > 0).ToList();
        if (paid.Count > 0)
        {
            var top = paid[0];
            recs.Add($"Pay {top.DebtName} first ({Percent(top.EffectiveAPR)} APR, {Money(top.LumpSumApplied)} applied) "
                + "- it carries your most expensive interest.");
        }

        var cleared = steps.Count(s => s.IsFullyPaid);
        if (cleared > 0)
        {
            recs.Add($"This plan clears {cleared} of {steps.Count} card(s) outright.");
        }

        if (totalInterestSaved > 0.005m)
        {
            recs.Add($"Following this plan avoids about {Money(totalInterestSaved)} in interest versus paying only the minimums.");
        }

        if (steps.Any(s => s.BalanceAfterLumpSum > 0.005m && s.MonthsToPayoffAfter is null))
        {
            recs.Add("At least one remaining balance never pays off at its current minimum payment - "
                + "raise that card's monthly payment to escape perpetual interest.");
        }

        if (leftover > 0.005m)
        {
            recs.Add($"After the reserve and clearing all debt, {Money(leftover)} of the windfall is left over - "
                + "park it in a high-yield savings account or invest it.");
        }

        if (steps.Count == 0)
        {
            recs.Add("You have no outstanding credit-card debt. Direct the windfall to your emergency reserve and investments.");
        }

        return recs;
    }

    private static string Money(decimal value) =>
        "$" + value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Percent(decimal rate) =>
        (rate * 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%";
}

public record CreditActionPlanInput(
    IEnumerable<Debt> Debts,
    decimal Windfall,
    decimal MonthlyEssentialExpenses,
    int EmergencyFundMonths = 6,
    decimal MonthlyIncome = 0m,
    AllocationStrategy Strategy = AllocationStrategy.Avalanche
);

public record CreditActionPlanResult(
    AllocationStrategy Strategy,
    decimal EmergencyFundTarget,
    decimal EmergencyFundReserved,
    bool IsEmergencyFundFunded,
    decimal MonthsOfExpensesCovered,
    decimal WindfallTotal,
    decimal WindfallToDebt,
    decimal WindfallRemaining,
    decimal TotalDebtBefore,
    decimal TotalDebtAfter,
    decimal TotalInterestSaved,
    int MonthsToDebtFreeBefore,
    int MonthsToDebtFreeAfter,
    IReadOnlyList<DebtActionStep> Steps,
    IReadOnlyList<string> Recommendations
);

public record DebtActionStep(
    int Order,
    string DebtName,
    decimal EffectiveAPR,
    decimal StartingBalance,
    decimal MinimumPayment,
    decimal LumpSumApplied,
    decimal BalanceAfterLumpSum,
    bool IsFullyPaid,
    int? MonthsToPayoffBefore,
    int? MonthsToPayoffAfter,
    decimal InterestBefore,
    decimal InterestAfter,
    decimal InterestSaved
);
