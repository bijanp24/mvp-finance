namespace FinanceEngine.Models.Outputs;

public record DebtAllocationResult(
    Dictionary<string, DebtPayment> PaymentsByDebt,
    decimal TotalPayment,
    AllocationStrategy StrategyUsed
);

public record DebtPayment(
    string DebtName,
    decimal MinimumPayment,
    decimal ExtraPayment,
    decimal TotalPayment,
    decimal RemainingBalance
);
