namespace FinanceEngine.Models.Inputs;

public record SpendableInput(
    decimal AvailableCash,
    DateTime CalculationDate,
    IEnumerable<Obligation> UpcomingObligations,
    IEnumerable<IncomeEvent> UpcomingIncome,
    decimal? ManualSafetyBuffer = null,
    decimal? EstimatedDailySpend = null,
    IEnumerable<Obligation> PlannedContributions = null
)
{
    // Default to empty collections if not specified
    public IEnumerable<Obligation> UpcomingObligations { get; init; } = UpcomingObligations ?? Array.Empty<Obligation>();
    public IEnumerable<IncomeEvent> UpcomingIncome { get; init; } = UpcomingIncome ?? Array.Empty<IncomeEvent>();
    public IEnumerable<Obligation> PlannedContributions { get; init; } = PlannedContributions ?? Array.Empty<Obligation>();
}
