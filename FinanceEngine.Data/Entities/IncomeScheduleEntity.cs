namespace FinanceEngine.Data.Entities;

public enum IncomeFrequency
{
    Weekly,
    BiWeekly,
    SemiMonthly,
    Monthly
}

public class IncomeScheduleEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public IncomeFrequency Frequency { get; set; }
    public DateTime NextPayDate { get; set; }
    public int? TargetAccountId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AccountEntity? TargetAccount { get; set; }
}
