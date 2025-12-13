namespace FinanceEngine.Models.Inputs;

public record BurnRateInput(
    IEnumerable<SpendingEvent> SpendingEvents,
    DateTime CalculationDate,
    int[] WindowDays = null
)
{
    // Default to 7, 30, and 90-day windows if not specified
    public int[] WindowDays { get; init; } = WindowDays ?? new[] { 7, 30, 90 };
}
