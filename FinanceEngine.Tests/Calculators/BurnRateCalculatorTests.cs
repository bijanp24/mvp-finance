using FinanceEngine.Calculators;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;

namespace FinanceEngine.Tests.Calculators;

public class BurnRateCalculatorTests
{
    [Fact]
    public void Calculate_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BurnRateCalculator.Calculate(null));
    }

    [Fact]
    public void Calculate_WithNullSpendingEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var input = new BurnRateInput(
            SpendingEvents: null,
            CalculationDate: DateTime.Now
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BurnRateCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithInvalidWindowDays_ThrowsArgumentException()
    {
        // Arrange
        var input = new BurnRateInput(
            SpendingEvents: Array.Empty<SpendingEvent>(),
            CalculationDate: DateTime.Now,
            WindowDays: new[] { -1 }
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => BurnRateCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithEmptySpendingEvents_ReturnsZeroBurnRate()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var input = new BurnRateInput(
            SpendingEvents: Array.Empty<SpendingEvent>(),
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        Assert.Single(result.BurnRatesByWindow);
        var window7 = result.BurnRatesByWindow[7];
        Assert.Equal(0m, window7.AverageDailySpend);
        Assert.Equal(0m, window7.StandardDeviation);
        Assert.Equal(0m, window7.MinDailySpend);
        Assert.Equal(0m, window7.MaxDailySpend);
    }

    [Fact]
    public void Calculate_WithSingleDaySpending_CalculatesCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new[]
        {
            new SpendingEvent(new DateTime(2024, 1, 31), 100m)
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];
        Assert.Equal(100m / 7m, window7.AverageDailySpend); // $100 over 7 days = ~$14.29/day
        Assert.Equal(100m, window7.MaxDailySpend);
    }

    [Fact]
    public void Calculate_WithMultipleDaysSpending_CalculatesCorrectBurnRate()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new[]
        {
            new SpendingEvent(new DateTime(2024, 1, 29), 50m),
            new SpendingEvent(new DateTime(2024, 1, 30), 75m),
            new SpendingEvent(new DateTime(2024, 1, 31), 25m)
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];
        var expectedAverage = 150m / 7m; // Total $150 over 7 days
        Assert.Equal(expectedAverage, window7.AverageDailySpend);
        Assert.Equal(25m, window7.MinDailySpend); // Min non-zero spending
        Assert.Equal(75m, window7.MaxDailySpend);
    }

    [Fact]
    public void Calculate_WithMultipleTransactionsSameDay_SumsCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new[]
        {
            new SpendingEvent(new DateTime(2024, 1, 31), 30m),
            new SpendingEvent(new DateTime(2024, 1, 31), 40m),
            new SpendingEvent(new DateTime(2024, 1, 31), 30m)
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];
        var expectedAverage = 100m / 7m; // $100 total over 7 days
        Assert.Equal(expectedAverage, window7.AverageDailySpend);
        Assert.Equal(100m, window7.MaxDailySpend); // All on one day
    }

    [Fact]
    public void Calculate_ExcludesEventsOutsideWindow()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new[]
        {
            new SpendingEvent(new DateTime(2024, 1, 20), 1000m), // Outside 7-day window
            new SpendingEvent(new DateTime(2024, 1, 30), 50m),   // Inside window
            new SpendingEvent(new DateTime(2024, 1, 31), 50m)    // Inside window
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];
        var expectedAverage = 100m / 7m; // Only $100 from events within window
        Assert.Equal(expectedAverage, window7.AverageDailySpend);
    }

    [Fact]
    public void Calculate_WithMultipleWindows_CalculatesAllCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new List<SpendingEvent>();

        // Add spending for 90 days: $10/day
        for (int i = 0; i < 90; i++)
        {
            spendingEvents.Add(new SpendingEvent(
                calculationDate.AddDays(-i),
                10m
            ));
        }

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7, 30, 90 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        Assert.Equal(3, result.BurnRatesByWindow.Count);

        // All windows should show $10/day average
        Assert.Equal(10m, result.BurnRatesByWindow[7].AverageDailySpend);
        Assert.Equal(10m, result.BurnRatesByWindow[30].AverageDailySpend);
        Assert.Equal(10m, result.BurnRatesByWindow[90].AverageDailySpend);
    }

    [Fact]
    public void Calculate_StandardDeviation_CalculatesCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new[]
        {
            // Consistent spending pattern for easy calculation
            new SpendingEvent(new DateTime(2024, 1, 28), 10m),
            new SpendingEvent(new DateTime(2024, 1, 29), 20m),
            new SpendingEvent(new DateTime(2024, 1, 30), 30m)
            // Days 25, 26, 27, 31 have $0
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];

        // Mean = 60 / 7 ≈ 8.57
        // Values: [0, 10, 20, 30, 0, 0, 0]
        // Variance = sum((x - mean)^2) / n
        // Should be > 0 since we have variability
        Assert.True(window7.StandardDeviation > 0m);
    }

    [Fact]
    public void Calculate_Percentiles_CalculateCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 10);
        var spendingEvents = new[]
        {
            new SpendingEvent(new DateTime(2024, 1, 4), 10m),
            new SpendingEvent(new DateTime(2024, 1, 5), 20m),
            new SpendingEvent(new DateTime(2024, 1, 6), 30m),
            new SpendingEvent(new DateTime(2024, 1, 7), 40m),
            new SpendingEvent(new DateTime(2024, 1, 8), 50m),
            new SpendingEvent(new DateTime(2024, 1, 9), 60m),
            new SpendingEvent(new DateTime(2024, 1, 10), 70m)
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];

        // Sorted values: [10, 20, 30, 40, 50, 60, 70]
        // 25th percentile should be around 20-30
        // 75th percentile should be around 50-60
        // 90th percentile should be around 60-70
        Assert.True(window7.Percentile25 >= 10m && window7.Percentile25 <= 30m);
        Assert.True(window7.Percentile75 >= 50m && window7.Percentile75 <= 60m);
        Assert.True(window7.Percentile90 >= 60m && window7.Percentile90 <= 70m);
    }

    [Fact]
    public void Calculate_WithDecimalPrecision_MaintainsPrecision()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 31);
        var spendingEvents = new[]
        {
            new SpendingEvent(new DateTime(2024, 1, 31), 10.50m),
            new SpendingEvent(new DateTime(2024, 1, 30), 20.75m),
            new SpendingEvent(new DateTime(2024, 1, 29), 15.25m)
        };

        var input = new BurnRateInput(
            SpendingEvents: spendingEvents,
            CalculationDate: calculationDate,
            WindowDays: new[] { 7 }
        );

        // Act
        var result = BurnRateCalculator.Calculate(input);

        // Assert
        var window7 = result.BurnRatesByWindow[7];
        var expectedTotal = 10.50m + 20.75m + 15.25m; // 46.50
        var expectedAverage = expectedTotal / 7m;

        Assert.Equal(expectedAverage, window7.AverageDailySpend);
    }
}
