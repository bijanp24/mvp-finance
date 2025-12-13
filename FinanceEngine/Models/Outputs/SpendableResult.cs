namespace FinanceEngine.Models.Outputs;

public record SpendableResult(
    decimal SpendableNow,
    decimal ExpectedCashAtNextPaycheck,
    DateTime? NextPaycheckDate,
    SpendableBreakdown Breakdown,
    SpendableScenario? ConservativeScenario = null
);

public record SpendableBreakdown(
    decimal AvailableCash,
    decimal TotalObligations,
    decimal SafetyBuffer,
    decimal PlannedContributions,
    int DaysUntilNextPaycheck
);

public record SpendableScenario(
    string ScenarioName,
    decimal EstimatedDailySpend,
    decimal SpendableAmount,
    decimal ExpectedCashAtPaycheck
);
