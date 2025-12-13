namespace FinanceEngine.Models;

public record Obligation(
    DateTime DueDate,
    decimal Amount,
    string Description
);
