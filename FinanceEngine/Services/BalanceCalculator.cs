namespace FinanceEngine.Services;

/// <summary>
/// Pure calculation service for computing account balances based on initial balance and events.
/// </summary>
public static class BalanceCalculator
{
    /// <summary>
    /// Calculates the current balance for an account given its type, initial balance, and events.
    /// </summary>
    /// <param name="accountType">The type of account (Cash, Debt, Investment)</param>
    /// <param name="initialBalance">The starting balance of the account</param>
    /// <param name="events">Collection of financial events affecting the balance</param>
    /// <returns>The calculated current balance</returns>
    public static decimal Calculate(AccountType accountType, decimal initialBalance, IEnumerable<FinancialEvent> events)
    {
        var balance = initialBalance;
        
        foreach (var evt in events)
        {
            balance += evt.Type switch
            {
                EventType.Income => evt.Amount,
                EventType.Expense => -evt.Amount,
                EventType.DebtCharge => evt.Amount,
                EventType.DebtPayment => -evt.Amount,
                EventType.InterestFee => evt.Amount,
                EventType.SavingsContribution => accountType == AccountType.Cash ? -evt.Amount : evt.Amount,
                EventType.InvestmentContribution => accountType == AccountType.Cash ? -evt.Amount : evt.Amount,
                _ => 0
            };
        }
        
        return balance;
    }
}

/// <summary>
/// Represents an account type for balance calculation purposes.
/// </summary>
public enum AccountType
{
    Cash,
    Debt,
    Investment
}

/// <summary>
/// Represents a financial event type for balance calculation purposes.
/// </summary>
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

/// <summary>
/// Represents a financial event for balance calculation purposes.
/// </summary>
public record FinancialEvent(EventType Type, decimal Amount);


