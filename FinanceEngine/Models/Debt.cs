namespace FinanceEngine.Models;

public record Debt(
    string Name,
    decimal Balance,
    decimal AnnualPercentageRate,
    decimal MinimumPayment
);
