namespace FinanceEngine.Models.Outputs;

public record ForwardSimulationResult(
    List<SimulationSnapshot> Snapshots,
    DateTime? DebtFreeDate,
    DateTime? MillionaireDate,
    decimal FinalCashBalance,
    decimal FinalInvestmentBalance,
    decimal FinalNetWorth,
    Dictionary<string, decimal> FinalDebtBalances,
    Dictionary<string, FinalInvestmentBalance> FinalInvestmentBalances,
    decimal TotalInterestPaid,
    decimal TotalInvestmentGrowth,
    decimal TotalContributed
);

public record SimulationSnapshot(
    DateTime Date,
    decimal CashBalance,
    Dictionary<string, decimal> DebtBalances,
    Dictionary<string, InvestmentSnapshot> InvestmentBalances,
    decimal TotalDebt,
    decimal InterestAccruedThisPeriod,
    decimal DailyInvestmentGrowth,
    decimal NetWorth,
    string EventDescription
);

public record InvestmentSnapshot(
    string AccountName,
    decimal Balance,
    decimal DailyGrowth
);

public record FinalInvestmentBalance(
    string AccountName,
    decimal Balance,
    decimal TotalGrowth,
    decimal TotalContributed
);
