namespace FinanceEngine.Data.Entities;

public enum BudgetFrequency
{
    Monthly,
    BiWeekly,
    Weekly
}

public class BudgetEntity
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public BudgetFrequency Frequency { get; set; } = BudgetFrequency.Monthly;
    public DateTime EffectiveDate { get; set; }
    public DateTime? EndDate { get; set; }          // Null = no end date (ongoing)
    public int? LinkedAccountId { get; set; }       // Optional: which account pays this
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public CategoryEntity Category { get; set; } = null!;
    public AccountEntity? LinkedAccount { get; set; }
}
