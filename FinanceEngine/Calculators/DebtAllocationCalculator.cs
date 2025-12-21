using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;
using FinanceEngine.Models.Outputs;

namespace FinanceEngine.Calculators;

public static class DebtAllocationCalculator
{
    public static DebtAllocationResult Calculate(DebtAllocationInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (input.Debts == null)
            throw new ArgumentNullException(nameof(input.Debts));

        if (input.ExtraPaymentAmount < 0)
            throw new ArgumentException("Extra payment amount cannot be negative.", nameof(input.ExtraPaymentAmount));

        var debts = input.Debts.ToList();

        // Validate debts
        foreach (var debt in debts)
        {
            if (debt.Balance < 0)
                throw new ArgumentException($"Debt balance cannot be negative: {debt.Name}");
            if (debt.AnnualPercentageRate < 0)
                throw new ArgumentException($"APR cannot be negative: {debt.Name}");
            if (debt.MinimumPayment < 0)
                throw new ArgumentException($"Minimum payment cannot be negative: {debt.Name}");
        }

        // Filter out debts with zero balance
        debts = debts.Where(d => d.Balance > 0).ToList();

        if (debts.Count == 0)
        {
            return new DebtAllocationResult(
                PaymentsByDebt: new Dictionary<string, DebtPayment>(),
                TotalPayment: 0m,
                StrategyUsed: input.Strategy
            );
        }

        // Apply allocation strategy
        var paymentsByDebt = input.Strategy switch
        {
            AllocationStrategy.Avalanche => AllocateAvalanche(debts, input.ExtraPaymentAmount),
            AllocationStrategy.Snowball => AllocateSnowball(debts, input.ExtraPaymentAmount),
            AllocationStrategy.Hybrid => AllocateHybrid(debts, input.ExtraPaymentAmount),
            _ => throw new ArgumentException($"Unknown allocation strategy: {input.Strategy}")
        };

        var totalPayment = paymentsByDebt.Values.Sum(p => p.TotalPayment);

        return new DebtAllocationResult(
            PaymentsByDebt: paymentsByDebt,
            TotalPayment: totalPayment,
            StrategyUsed: input.Strategy
        );
    }

    private static Dictionary<string, DebtPayment> AllocateAvalanche(List<Debt> debts, decimal extraPayment)
    {
        // Sort by effective APR descending (highest first)
        // EffectiveAPR automatically uses promotional APR when active, regular APR when expired
        var sortedDebts = debts.OrderByDescending(d => d.EffectiveAPR).ToList();
        return AllocateWithPriority(sortedDebts, extraPayment);
    }

    private static Dictionary<string, DebtPayment> AllocateSnowball(List<Debt> debts, decimal extraPayment)
    {
        // Sort by balance ascending (smallest first)
        var sortedDebts = debts.OrderBy(d => d.Balance).ToList();
        return AllocateWithPriority(sortedDebts, extraPayment);
    }

    private static Dictionary<string, DebtPayment> AllocateHybrid(List<Debt> debts, decimal extraPayment)
    {
        // Hybrid is same as Avalanche for extra payment allocation
        // The "hybrid" aspect is that minimums are always enforced first (which we do in all strategies)
        return AllocateAvalanche(debts, extraPayment);
    }

    private static Dictionary<string, DebtPayment> AllocateWithPriority(List<Debt> prioritizedDebts, decimal extraPayment)
    {
        var payments = new Dictionary<string, DebtPayment>();
        var remainingExtra = extraPayment;

        foreach (var debt in prioritizedDebts)
        {
            var minimumPayment = debt.MinimumPayment;
            var extraForThisDebt = 0m;

            // If this is the highest priority debt with remaining balance, allocate all extra payment
            if (remainingExtra > 0 && debt == prioritizedDebts.First(d => d.Balance > 0))
            {
                // Don't pay more than the remaining balance
                var maxPayment = debt.Balance - minimumPayment;
                extraForThisDebt = Math.Min(remainingExtra, maxPayment);
                remainingExtra -= extraForThisDebt;
            }

            var totalPayment = minimumPayment + extraForThisDebt;
            // Don't pay more than the balance
            totalPayment = Math.Min(totalPayment, debt.Balance);

            var remainingBalance = debt.Balance - totalPayment;

            payments[debt.Name] = new DebtPayment(
                DebtName: debt.Name,
                MinimumPayment: minimumPayment,
                ExtraPayment: extraForThisDebt,
                TotalPayment: totalPayment,
                RemainingBalance: remainingBalance
            );
        }

        return payments;
    }
}
