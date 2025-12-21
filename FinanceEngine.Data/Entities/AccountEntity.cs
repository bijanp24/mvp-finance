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

    // Promotional APR fields
    public decimal? PromotionalAnnualPercentageRate { get; set; }  // e.g., 0.00 for 0% promo
    public DateTime? PromotionalPeriodEndDate { get; set; }        // When promo expires

    // Balance transfer fee
    public decimal? BalanceTransferFeePercentage { get; set; }     // e.g., 5.00 for 5%

    // Statement and due date fields (hybrid: day-of-month with optional override)
    public int? StatementDayOfMonth { get; set; }                  // 1-31, default recurring date
    public DateTime? StatementDateOverride { get; set; }           // Optional specific date override
    public int? PaymentDueDayOfMonth { get; set; }                 // 1-31, default recurring date
    public DateTime? PaymentDueDateOverride { get; set; }          // Optional specific date override

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<FinancialEventEntity> Events { get; set; } = new List<FinancialEventEntity>();
}
