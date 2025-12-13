namespace FinanceEngine.Models;

public enum AllocationStrategy
{
    Avalanche,  // Highest APR first
    Snowball,   // Smallest balance first
    Hybrid      // Minimum payments first, then strategy
}
