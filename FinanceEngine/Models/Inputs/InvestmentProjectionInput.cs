namespace FinanceEngine.Models.Inputs;

public record InvestmentProjectionInput(
    decimal InitialBalance,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<InvestmentContribution> Contributions,
    decimal NominalAnnualReturn,
    decimal InflationRate = 0.03m
);
