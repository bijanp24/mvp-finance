namespace FinanceEngine.Models;

/// <summary>
/// Represents an investment account for simulation purposes.
/// </summary>
public record InvestmentAccount(
    string Name,
    decimal InitialBalance,
    decimal AnnualReturnRate  // e.g., 0.07 for 7%
);

