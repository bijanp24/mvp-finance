# WI-P8-002: RecurringEventExpansionService

## Objective
Create a stateless service to expand recurring contribution schedules into lists of individual contribution events for use in projections and simulations.

## Context
- `InvestmentProjectionCalculator` accepts a list of `InvestmentContribution` records with dates and amounts.
- Currently, users must manually specify each contribution date.
- This service generates contribution events from a schedule over any date range.
- Same pattern can be applied to income schedules for consistency.

## Files to Create/Modify
- `FinanceEngine/Services/RecurringEventExpansionService.cs` (NEW)
- `FinanceEngine.Tests/Services/RecurringEventExpansionServiceTests.cs` (NEW)

## Service Design

```csharp
public static class RecurringEventExpansionService
{
    /// <summary>
    /// Expands a recurring contribution schedule into individual contribution events.
    /// </summary>
    public static IEnumerable<InvestmentContribution> ExpandContributions(
        RecurringContributionEntity schedule,
        DateOnly startDate,
        DateOnly endDate)
    {
        // Implementation: iterate from NextContributionDate by Frequency
        // Yield each occurrence within [startDate, endDate]
    }

    /// <summary>
    /// Expands an income schedule into individual income events.
    /// Provides symmetry with contribution expansion.
    /// </summary>
    public static IEnumerable<IncomeEvent> ExpandIncome(
        IncomeScheduleEntity schedule,
        DateOnly startDate,
        DateOnly endDate)
    {
        // Implementation: same pattern as contributions
    }

    /// <summary>
    /// Calculates the next N occurrences from an anchor date with given frequency.
    /// </summary>
    public static IEnumerable<DateOnly> GetOccurrences(
        DateOnly anchorDate,
        ContributionFrequency frequency,
        DateOnly startDate,
        DateOnly endDate)
    {
        // Core date iteration logic
    }
}
```

## Algorithm Notes

### Date Iteration by Frequency
```csharp
private static DateOnly AddFrequency(DateOnly date, ContributionFrequency frequency)
{
    return frequency switch
    {
        ContributionFrequency.Weekly => date.AddDays(7),
        ContributionFrequency.BiWeekly => date.AddDays(14),
        ContributionFrequency.SemiMonthly => date.AddDays(15), // Approximation
        ContributionFrequency.Monthly => date.AddMonths(1),
        ContributionFrequency.Quarterly => date.AddMonths(3),
        ContributionFrequency.Annually => date.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(frequency))
    };
}
```

### SemiMonthly Handling
SemiMonthly (twice per month) is complex:
- Option A: Simple +15 days (current approach in PayFrequency)
- Option B: Fixed days (1st and 15th, or 15th and last day)
- Recommendation: Use Option A for MVP, document limitation

### Edge Cases
- `startDate` > `endDate`: return empty
- `anchorDate` > `endDate`: return empty
- `anchorDate` < `startDate`: iterate forward until within range
- Schedule `IsActive = false`: caller should filter before calling

## Test Cases

```csharp
[Fact]
public void ExpandContributions_Monthly_ReturnsCorrectDates()
{
    var schedule = new RecurringContributionEntity
    {
        Amount = 500,
        Frequency = ContributionFrequency.Monthly,
        NextContributionDate = new DateTime(2025, 1, 15)
    };

    var contributions = RecurringEventExpansionService.ExpandContributions(
        schedule,
        new DateOnly(2025, 1, 1),
        new DateOnly(2025, 6, 30));

    Assert.Equal(6, contributions.Count());
    Assert.Equal(new DateOnly(2025, 1, 15), contributions.First().Date);
    Assert.Equal(500, contributions.First().Amount);
}

[Fact]
public void ExpandContributions_StartAfterAnchor_SkipsEarlierDates()
{
    // Anchor is Jan 1, start is March 1 - should skip Jan and Feb
}

[Fact]
public void ExpandContributions_BiWeekly_ReturnsCorrectCount()
{
    // 52 weeks / 2 = 26 occurrences per year
}

[Fact]
public void GetOccurrences_EmptyRange_ReturnsEmpty()
{
    // startDate > endDate
}
```

## Acceptance Criteria
- [ ] `RecurringEventExpansionService` created as static class
- [ ] `ExpandContributions` generates correct events for all frequency types
- [ ] `ExpandIncome` provides symmetry for income schedules
- [ ] Edge cases handled (empty range, anchor outside range)
- [ ] Minimum 10 unit tests covering all frequency types and edge cases
- [ ] All tests pass

## Verification
```bash
dotnet test --filter "FullyQualifiedName~RecurringEventExpansion"
```

## Dependencies
- WI-P8-001 (RecurringContributionEntity must exist)

## Parallel Execution
- Must wait for WI-P8-001
- After completion: WI-P8-005, WI-P8-006, WI-P8-007 can run in parallel
