namespace FinanceEngine.Models;

/// <summary>
/// Represents a recurring contribution for simulation.
/// Used to transfer money from cash to investment accounts.
/// </summary>
public record SimulationContribution(
    DateTime Date,
    decimal Amount,
    string SourceAccountName,   // For tracking (e.g., "Cash")
    string TargetAccountName    // Investment account name
);

