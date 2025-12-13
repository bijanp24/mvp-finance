namespace FinanceEngine.Models.Outputs;

public record InvestmentProjectionResult(
    List<InvestmentProjectionPoint> Projections,
    decimal FinalNominalValue,
    decimal FinalRealValue,
    decimal TotalContributions,
    decimal TotalNominalGrowth,
    decimal TotalRealGrowth
);

public record InvestmentProjectionPoint(
    DateTime Date,
    decimal NominalValue,
    decimal RealValue,
    decimal TotalContributed,
    decimal NominalGrowth,
    decimal RealGrowth
);
