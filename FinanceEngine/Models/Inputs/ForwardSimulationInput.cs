namespace FinanceEngine.Models.Inputs;

public record ForwardSimulationInput(
    DateTime StartDate,
    DateTime EndDate,
    decimal InitialCash,
    IEnumerable<DebtAccount> Debts,
    IEnumerable<SimulationEvent> Events
);
