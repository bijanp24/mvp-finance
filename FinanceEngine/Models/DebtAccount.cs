namespace FinanceEngine.Models;

public record DebtAccount(
    string Name,
    decimal InitialBalance,
    decimal AnnualPercentageRate,
    decimal MinimumPayment,
    decimal? PromotionalAnnualPercentageRate = null,
    DateTime? PromotionalPeriodEndDate = null
)
{
    public decimal CurrentBalance { get; init; } = InitialBalance;

    public decimal EffectiveAPR =>
        PromotionalPeriodEndDate.HasValue &&
        PromotionalPeriodEndDate.Value > DateTime.UtcNow &&
        PromotionalAnnualPercentageRate.HasValue
            ? PromotionalAnnualPercentageRate.Value
            : AnnualPercentageRate;
}
