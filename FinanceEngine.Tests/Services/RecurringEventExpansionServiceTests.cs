using FinanceEngine.Services;

namespace FinanceEngine.Tests.Services;

public class RecurringEventExpansionServiceTests
{
    #region ExpandContributions Tests

    [Fact]
    public void ExpandContributions_Monthly_ReturnsCorrectDatesAndAmounts()
    {
        // Arrange
        var amount = 500m;
        var frequency = RecurringFrequency.Monthly;
        var anchorDate = new DateTime(2025, 1, 15);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 6, 30);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert
        Assert.Equal(6, contributions.Count);
        Assert.Equal(new DateTime(2025, 1, 15), contributions[0].Date);
        Assert.Equal(500, contributions[0].Amount);
        Assert.Equal(new DateTime(2025, 2, 15), contributions[1].Date);
        Assert.Equal(new DateTime(2025, 3, 15), contributions[2].Date);
        Assert.Equal(new DateTime(2025, 4, 15), contributions[3].Date);
        Assert.Equal(new DateTime(2025, 5, 15), contributions[4].Date);
        Assert.Equal(new DateTime(2025, 6, 15), contributions[5].Date);
    }

    [Fact]
    public void ExpandContributions_Weekly_ReturnsCorrectCount()
    {
        // Arrange
        var amount = 100m;
        var frequency = RecurringFrequency.Weekly;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 31);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert - 5 weeks in January (1, 8, 15, 22, 29)
        Assert.Equal(5, contributions.Count);
        Assert.Equal(new DateTime(2025, 1, 1), contributions[0].Date);
        Assert.Equal(new DateTime(2025, 1, 8), contributions[1].Date);
        Assert.Equal(new DateTime(2025, 1, 29), contributions[4].Date);
    }

    [Fact]
    public void ExpandContributions_BiWeekly_ReturnsCorrectCount()
    {
        // Arrange
        var amount = 200m;
        var frequency = RecurringFrequency.BiWeekly;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 12, 31);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert - 27 occurrences in 2025 (365 days / 14 = 26.07, rounds to 27)
        Assert.Equal(27, contributions.Count);
        Assert.Equal(new DateTime(2025, 1, 1), contributions[0].Date);
        Assert.Equal(new DateTime(2025, 1, 15), contributions[1].Date);
        Assert.Equal(new DateTime(2025, 12, 31), contributions[26].Date);
    }

    [Fact]
    public void ExpandContributions_Quarterly_ReturnsCorrectCount()
    {
        // Arrange
        var amount = 3000m;
        var frequency = RecurringFrequency.Quarterly;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 12, 31);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert - 4 quarters per year
        Assert.Equal(4, contributions.Count);
        Assert.Equal(new DateTime(2025, 1, 1), contributions[0].Date);
        Assert.Equal(new DateTime(2025, 4, 1), contributions[1].Date);
        Assert.Equal(new DateTime(2025, 7, 1), contributions[2].Date);
        Assert.Equal(new DateTime(2025, 10, 1), contributions[3].Date);
    }

    [Fact]
    public void ExpandContributions_Annually_ReturnsCorrectCount()
    {
        // Arrange
        var amount = 10000m;
        var frequency = RecurringFrequency.Annually;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2027, 12, 31);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert - 3 years
        Assert.Equal(3, contributions.Count);
        Assert.Equal(new DateTime(2025, 1, 1), contributions[0].Date);
        Assert.Equal(new DateTime(2026, 1, 1), contributions[1].Date);
        Assert.Equal(new DateTime(2027, 1, 1), contributions[2].Date);
    }

    [Fact]
    public void ExpandContributions_SemiMonthly_ReturnsCorrectCount()
    {
        // Arrange
        var amount = 250m;
        var frequency = RecurringFrequency.SemiMonthly;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 3, 31);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert - Approximately 6 occurrences in 3 months (every 15 days)
        Assert.Equal(6, contributions.Count);
        Assert.Equal(new DateTime(2025, 1, 1), contributions[0].Date);
        Assert.Equal(new DateTime(2025, 1, 16), contributions[1].Date);
    }

    [Fact]
    public void ExpandContributions_StartAfterAnchor_SkipsEarlierDates()
    {
        // Arrange
        var amount = 500m;
        var frequency = RecurringFrequency.Monthly;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 3, 1);
        var endDate = new DateOnly(2025, 6, 30);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert - Should skip Jan and Feb, start from March
        Assert.Equal(4, contributions.Count);
        Assert.Equal(new DateTime(2025, 3, 1), contributions[0].Date);
        Assert.Equal(new DateTime(2025, 4, 1), contributions[1].Date);
    }

    [Fact]
    public void ExpandContributions_EmptyRange_ReturnsEmpty()
    {
        // Arrange
        var amount = 500m;
        var frequency = RecurringFrequency.Monthly;
        var anchorDate = new DateTime(2025, 1, 1);
        var startDate = new DateOnly(2025, 6, 1);
        var endDate = new DateOnly(2025, 1, 1); // End before start

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert
        Assert.Empty(contributions);
    }

    [Fact]
    public void ExpandContributions_AnchorAfterEnd_ReturnsEmpty()
    {
        // Arrange
        var amount = 500m;
        var frequency = RecurringFrequency.Monthly;
        var anchorDate = new DateTime(2025, 12, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 6, 30);

        // Act
        var contributions = RecurringEventExpansionService.ExpandContributions(
            amount, frequency, anchorDate, startDate, endDate).ToList();

        // Assert
        Assert.Empty(contributions);
    }

    #endregion

    #region ExpandIncome Tests

    [Fact]
    public void ExpandIncome_Monthly_ReturnsCorrectDatesAndAmounts()
    {
        // Arrange
        var amount = 5000m;
        var frequency = RecurringFrequency.Monthly;
        var anchorDate = new DateTime(2025, 1, 15);
        var description = "Salary";
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 3, 31);

        // Act
        var incomeEvents = RecurringEventExpansionService.ExpandIncome(
            amount, frequency, anchorDate, description, startDate, endDate).ToList();

        // Assert
        Assert.Equal(3, incomeEvents.Count);
        Assert.Equal(new DateTime(2025, 1, 15), incomeEvents[0].Date);
        Assert.Equal(5000, incomeEvents[0].Amount);
        Assert.Equal("Salary", incomeEvents[0].Description);
        Assert.Equal(new DateTime(2025, 2, 15), incomeEvents[1].Date);
        Assert.Equal(new DateTime(2025, 3, 15), incomeEvents[2].Date);
    }

    [Fact]
    public void ExpandIncome_BiWeekly_ReturnsCorrectCount()
    {
        // Arrange
        var amount = 2000m;
        var frequency = RecurringFrequency.BiWeekly;
        var anchorDate = new DateTime(2025, 1, 1);
        var description = "Paycheck";
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 12, 31);

        // Act
        var incomeEvents = RecurringEventExpansionService.ExpandIncome(
            amount, frequency, anchorDate, description, startDate, endDate).ToList();

        // Assert - 27 occurrences in 2025 (365 days / 14 = 26.07, rounds to 27)
        Assert.Equal(27, incomeEvents.Count);
        Assert.All(incomeEvents, evt => Assert.Equal("Paycheck", evt.Description));
    }

    #endregion

    #region GetOccurrences Tests

    [Fact]
    public void GetOccurrences_EmptyRange_ReturnsEmpty()
    {
        // Arrange
        var anchorDate = new DateOnly(2025, 1, 1);
        var startDate = new DateOnly(2025, 6, 1);
        var endDate = new DateOnly(2025, 1, 1);

        // Act
        var occurrences = RecurringEventExpansionService.GetOccurrences(
            anchorDate, RecurringFrequency.Monthly, startDate, endDate).ToList();

        // Assert
        Assert.Empty(occurrences);
    }

    [Fact]
    public void GetOccurrences_AnchorAfterEnd_ReturnsEmpty()
    {
        // Arrange
        var anchorDate = new DateOnly(2025, 12, 1);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 6, 30);

        // Act
        var occurrences = RecurringEventExpansionService.GetOccurrences(
            anchorDate, RecurringFrequency.Monthly, startDate, endDate).ToList();

        // Assert
        Assert.Empty(occurrences);
    }

    [Fact]
    public void GetOccurrences_AnchorBeforeStart_FastForwardsCorrectly()
    {
        // Arrange
        var anchorDate = new DateOnly(2025, 1, 1);
        var startDate = new DateOnly(2025, 3, 1);
        var endDate = new DateOnly(2025, 5, 31);

        // Act
        var occurrences = RecurringEventExpansionService.GetOccurrences(
            anchorDate, RecurringFrequency.Monthly, startDate, endDate).ToList();

        // Assert - Should start from March, not January
        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateOnly(2025, 3, 1), occurrences[0]);
        Assert.Equal(new DateOnly(2025, 4, 1), occurrences[1]);
        Assert.Equal(new DateOnly(2025, 5, 1), occurrences[2]);
    }

    [Fact]
    public void GetOccurrences_Weekly_GeneratesCorrectSequence()
    {
        // Arrange
        var anchorDate = new DateOnly(2025, 1, 6); // Monday
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2025, 1, 31);

        // Act
        var occurrences = RecurringEventExpansionService.GetOccurrences(
            anchorDate, RecurringFrequency.Weekly, startDate, endDate).ToList();

        // Assert
        Assert.Equal(4, occurrences.Count);
        Assert.Equal(new DateOnly(2025, 1, 6), occurrences[0]);
        Assert.Equal(new DateOnly(2025, 1, 13), occurrences[1]);
        Assert.Equal(new DateOnly(2025, 1, 20), occurrences[2]);
        Assert.Equal(new DateOnly(2025, 1, 27), occurrences[3]);
    }

    [Fact]
    public void GetOccurrences_Annually_SpansMultipleYears()
    {
        // Arrange
        var anchorDate = new DateOnly(2025, 6, 15);
        var startDate = new DateOnly(2025, 1, 1);
        var endDate = new DateOnly(2029, 12, 31);

        // Act
        var occurrences = RecurringEventExpansionService.GetOccurrences(
            anchorDate, RecurringFrequency.Annually, startDate, endDate).ToList();

        // Assert - 5 years
        Assert.Equal(5, occurrences.Count);
        Assert.Equal(new DateOnly(2025, 6, 15), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 6, 15), occurrences[1]);
        Assert.Equal(new DateOnly(2029, 6, 15), occurrences[4]);
    }

    [Fact]
    public void GetOccurrences_SingleDay_ReturnsOneOccurrence()
    {
        // Arrange
        var anchorDate = new DateOnly(2025, 1, 15);
        var startDate = new DateOnly(2025, 1, 15);
        var endDate = new DateOnly(2025, 1, 15);

        // Act
        var occurrences = RecurringEventExpansionService.GetOccurrences(
            anchorDate, RecurringFrequency.Monthly, startDate, endDate).ToList();

        // Assert
        Assert.Single(occurrences);
        Assert.Equal(new DateOnly(2025, 1, 15), occurrences[0]);
    }

    #endregion
}

