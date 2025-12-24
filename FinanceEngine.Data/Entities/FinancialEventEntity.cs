namespace FinanceEngine.Data.Entities;

public enum EventType
{
    Income,
    Expense,
    DebtCharge,
    DebtPayment,
    InterestFee,
    SavingsContribution,
    InvestmentContribution
}

public enum EventStatus
{
    Pending,
    Cleared
}

public class FinancialEventEntity
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public EventType Type { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? AccountId { get; set; }
    public int? TargetAccountId { get; set; }  // For transfers (e.g., DebtPayment from Cash to Debt)
    public EventStatus Status { get; set; } = EventStatus.Cleared;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public AccountEntity? Account { get; set; }
    public AccountEntity? TargetAccount { get; set; }
}
