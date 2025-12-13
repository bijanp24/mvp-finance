namespace FinanceEngine.Models;

public enum SimulationEventType
{
    Income,
    Expense,
    DebtPayment,
    DebtCharge,
    InterestAccrual
}

public record SimulationEvent(
    DateTime Date,
    SimulationEventType Type,
    string Description,
    decimal Amount,
    string? RelatedDebtName = null
);
