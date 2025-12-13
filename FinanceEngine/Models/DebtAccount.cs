namespace FinanceEngine.Models;

public record DebtAccount(
    string Name,
    decimal InitialBalance,
    decimal AnnualPercentageRate,
    decimal MinimumPayment
)
{
    public decimal CurrentBalance { get; init; } = InitialBalance;
}
