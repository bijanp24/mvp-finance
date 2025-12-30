namespace FinanceEngine.Data.Entities;

public enum GoalType
{
    DebtFree,           // Pay off all linked debt by target date
    InvestmentTarget,   // Reach $X in linked investment accounts
    SavingsGoal,        // Save $X for specific purpose (short-term)
    NetWorthMilestone   // Hit net worth target (assets - liabilities)
}

public enum GoalStatus
{
    OnTrack,    // Current pace meets or exceeds target
    Ahead,      // Significantly ahead of schedule
    AtRisk,     // Slightly behind, corrective action recommended
    Behind      // Off track, needs intervention
}

public class GoalEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public GoalType Type { get; set; }
    public decimal? TargetAmount { get; set; }          // Null for DebtFree (target is $0)
    public DateTime TargetDate { get; set; }
    public string? LinkedAccountIds { get; set; }       // JSON array of account IDs, e.g., "[1,2,3]"
    public int Priority { get; set; } = 1;              // Lower = higher priority
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
