namespace FinanceEngine.Models.Inputs;

public record DebtAllocationInput(
    IEnumerable<Debt> Debts,
    decimal ExtraPaymentAmount,
    AllocationStrategy Strategy = AllocationStrategy.Avalanche
);
