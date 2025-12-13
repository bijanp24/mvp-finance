namespace FinanceEngine.Models.Outputs;

public record ForwardSimulationResult(
    List<SimulationSnapshot> Snapshots,
    DateTime? DebtFreeDate,
    decimal FinalCashBalance,
    Dictionary<string, decimal> FinalDebtBalances,
    decimal TotalInterestPaid
);

public record SimulationSnapshot(
    DateTime Date,
    decimal CashBalance,
    Dictionary<string, decimal> DebtBalances,
    decimal TotalDebt,
    decimal InterestAccruedThisPeriod,
    string EventDescription
);
