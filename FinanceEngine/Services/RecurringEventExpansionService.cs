using FinanceEngine.Models;

namespace FinanceEngine.Services;

/// <summary>
/// Frequency enum for recurring events (contributions, income, etc.)
/// </summary>
public enum RecurringFrequency
{
    Weekly = 7,
    BiWeekly = 14,
    SemiMonthly = 15,
    Monthly = 30,
    Quarterly = 90,
    Annually = 365
}

/// <summary>
/// Stateless service to expand recurring contribution and income schedules 
/// into lists of individual events for use in projections and simulations.
/// </summary>
public static class RecurringEventExpansionService
{
    /// <summary>
    /// Expands a recurring contribution schedule into individual contribution events.
    /// </summary>
    /// <param name="amount">The contribution amount per occurrence</param>
    /// <param name="frequency">How often the contribution recurs</param>
    /// <param name="anchorDate">The next contribution date (anchor for forward calculation)</param>
    /// <param name="startDate">Start of the date range (inclusive)</param>
    /// <param name="endDate">End of the date range (inclusive)</param>
    /// <returns>Enumerable of investment contributions within the date range</returns>
    public static IEnumerable<InvestmentContribution> ExpandContributions(
        decimal amount,
        RecurringFrequency frequency,
        DateTime anchorDate,
        DateOnly startDate,
        DateOnly endDate)
    {
        var anchor = DateOnly.FromDateTime(anchorDate);
        
        foreach (var occurrenceDate in GetOccurrences(anchor, frequency, startDate, endDate))
        {
            yield return new InvestmentContribution(
                occurrenceDate.ToDateTime(TimeOnly.MinValue),
                amount
            );
        }
    }

    /// <summary>
    /// Expands an income schedule into individual income events.
    /// Provides symmetry with contribution expansion.
    /// </summary>
    /// <param name="amount">The income amount per occurrence</param>
    /// <param name="frequency">How often the income recurs</param>
    /// <param name="anchorDate">The next pay date (anchor for forward calculation)</param>
    /// <param name="description">Description of the income</param>
    /// <param name="startDate">Start of the date range (inclusive)</param>
    /// <param name="endDate">End of the date range (inclusive)</param>
    /// <returns>Enumerable of income events within the date range</returns>
    public static IEnumerable<IncomeEvent> ExpandIncome(
        decimal amount,
        RecurringFrequency frequency,
        DateTime anchorDate,
        string description,
        DateOnly startDate,
        DateOnly endDate)
    {
        var anchor = DateOnly.FromDateTime(anchorDate);
        
        foreach (var occurrenceDate in GetOccurrences(anchor, frequency, startDate, endDate))
        {
            yield return new IncomeEvent(
                occurrenceDate.ToDateTime(TimeOnly.MinValue),
                amount,
                description
            );
        }
    }

    /// <summary>
    /// Calculates all occurrences from an anchor date with given frequency within a date range.
    /// </summary>
    /// <param name="anchorDate">The starting reference date for the recurring schedule</param>
    /// <param name="frequency">How often the event recurs</param>
    /// <param name="startDate">Start of the date range (inclusive)</param>
    /// <param name="endDate">End of the date range (inclusive)</param>
    /// <returns>Enumerable of dates representing each occurrence</returns>
    public static IEnumerable<DateOnly> GetOccurrences(
        DateOnly anchorDate,
        RecurringFrequency frequency,
        DateOnly startDate,
        DateOnly endDate)
    {
        // Handle invalid ranges
        if (startDate > endDate)
            yield break;

        if (anchorDate > endDate)
            yield break;

        var currentDate = anchorDate;

        // Fast-forward to first date within range if anchor is before start
        while (currentDate < startDate)
        {
            currentDate = AddFrequency(currentDate, frequency);
        }

        // Yield all occurrences within the range
        while (currentDate <= endDate)
        {
            yield return currentDate;
            currentDate = AddFrequency(currentDate, frequency);
        }
    }

    /// <summary>
    /// Adds the specified frequency interval to a date.
    /// </summary>
    private static DateOnly AddFrequency(DateOnly date, RecurringFrequency frequency)
    {
        return frequency switch
        {
            RecurringFrequency.Weekly => date.AddDays(7),
            RecurringFrequency.BiWeekly => date.AddDays(14),
            RecurringFrequency.SemiMonthly => date.AddDays(15), // Approximation for MVP
            RecurringFrequency.Monthly => date.AddMonths(1),
            RecurringFrequency.Quarterly => date.AddMonths(3),
            RecurringFrequency.Annually => date.AddYears(1),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, "Unsupported frequency type")
        };
    }
}

