namespace FinanceEngine.Data.Entities;

public enum ContributionFrequency
{
    Weekly = 7,
    BiWeekly = 14,
    SemiMonthly = 15,
    Monthly = 30,
    Quarterly = 90,
    Annually = 365
}

public class RecurringContributionEntity
{
    public int Id { get; set; }

    // Description
    public string Name { get; set; } = string.Empty;  // e.g., "401k Contribution"

    // Amount per occurrence
    public decimal Amount { get; set; }

    // Schedule
    public ContributionFrequency Frequency { get; set; }
    public DateTime NextContributionDate { get; set; }  // Anchor for forward calculation

    // Accounts (double-entry)
    public int SourceAccountId { get; set; }   // Cash account (debit)
    public int TargetAccountId { get; set; }   // Investment/Savings account (credit)

    // Foreign key navigation
    public AccountEntity? SourceAccount { get; set; }
    public AccountEntity? TargetAccount { get; set; }

    // Metadata
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

