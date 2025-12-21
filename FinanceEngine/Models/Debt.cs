namespace FinanceEngine.Models;

public record Debt(
    string Name,
    decimal Balance,
    decimal AnnualPercentageRate,
    decimal MinimumPayment,
    decimal? PromotionalAnnualPercentageRate = null,
    DateTime? PromotionalPeriodEndDate = null
)
{
    public decimal EffectiveAPR =>
        PromotionalPeriodEndDate.HasValue &&
        PromotionalPeriodEndDate.Value > DateTime.UtcNow &&
        PromotionalAnnualPercentageRate.HasValue
            ? PromotionalAnnualPercentageRate.Value
            : AnnualPercentageRate;
}
