namespace FinanceEngine.Data.Entities;

public enum AccountType
{
    Cash,       // Checking, HYSA
    Debt,       // Credit Cards, Loans
    Investment  // 401k, Brokerage
}

public class AccountEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal? AnnualPercentageRate { get; set; }  // For debt accounts
    public decimal? MinimumPayment { get; set; }        // For debt accounts
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<FinancialEventEntity> Events { get; set; } = new List<FinancialEventEntity>();
}
