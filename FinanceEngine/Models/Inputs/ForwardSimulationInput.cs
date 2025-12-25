namespace FinanceEngine.Models.Inputs;

public record ForwardSimulationInput(
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCash,
    IEnumerable<DebtAccount> Debts,
    IEnumerable<SimulationEvent> Events,
    IEnumerable<InvestmentAccount>? InvestmentAccounts = null,
    IEnumerable<SimulationContribution>? RecurringContributions = null
);
