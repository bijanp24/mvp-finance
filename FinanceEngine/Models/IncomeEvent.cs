namespace FinanceEngine.Models;

public record IncomeEvent(
    DateTime Date,
    decimal Amount,
    string Description
);
