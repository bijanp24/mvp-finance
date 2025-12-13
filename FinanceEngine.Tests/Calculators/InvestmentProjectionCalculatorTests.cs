using FinanceEngine.Calculators;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;

namespace FinanceEngine.Tests.Calculators;

public class InvestmentProjectionCalculatorTests
{
    #region Input Validation Tests

    [Fact]
    public void Calculate_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => InvestmentProjectionCalculator.Calculate(null));
    }

    [Fact]
    public void Calculate_WithNegativeInitialBalance_ThrowsArgumentException()
    {
        // Arrange
        var input = new InvestmentProjectionInput(
            InitialBalance: -1000m,
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 12, 31),
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => InvestmentProjectionCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithEndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: new DateTime(2024, 12, 31),
            EndDate: new DateTime(2024, 1, 1),
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => InvestmentProjectionCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithNullContributions_ThrowsArgumentNullException()
    {
        // Arrange
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 12, 31),
            Contributions: null,
            NominalAnnualReturn: 0.07m
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => InvestmentProjectionCalculator.Calculate(input));
    }

    #endregion

    #region Basic Calculation Tests

    [Fact]
    public void Calculate_WithZeroReturnAndNoContributions_MaintainsValue()
    {
        // Arrange - 30 days, no growth, no contributions
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 30);
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0m,
            InflationRate: 0m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        Assert.Equal(1000m, result.FinalNominalValue);
        Assert.Equal(1000m, result.TotalContributions);
        Assert.Equal(0m, result.TotalNominalGrowth);
    }

    [Fact]
    public void Calculate_WithPositiveReturn_IncreasesValue()
    {
        // Arrange - 1 year with 7% return
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 10000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m,
            InflationRate: 0m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        // Should be approximately $10,700 (7% growth)
        Assert.True(result.FinalNominalValue > 10000m);
        Assert.True(result.FinalNominalValue >= 10650m && result.FinalNominalValue <= 10750m);
        Assert.True(result.TotalNominalGrowth > 0m);
    }

    [Fact]
    public void Calculate_WithContributions_AddsContributionsToValue()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 3, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: new[]
            {
                new InvestmentContribution(new DateTime(2024, 2, 1), 500m),
                new InvestmentContribution(new DateTime(2024, 3, 1), 500m)
            },
            NominalAnnualReturn: 0m,
            InflationRate: 0m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        // Initial 1000 + 500 + 500 = 2000
        Assert.Equal(2000m, result.TotalContributions);
        Assert.Equal(2000m, result.FinalNominalValue);
    }

    [Fact]
    public void Calculate_WithContributionsAndGrowth_GrowsCorrectly()
    {
        // Arrange - Monthly contributions with 12% annual return
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 0m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: new[]
            {
                new InvestmentContribution(new DateTime(2024, 1, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 2, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 3, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 4, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 5, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 6, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 7, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 8, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 9, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 10, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 11, 1), 1000m),
                new InvestmentContribution(new DateTime(2024, 12, 1), 1000m)
            },
            NominalAnnualReturn: 0.12m,
            InflationRate: 0m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        Assert.Equal(12000m, result.TotalContributions);
        // Should be more than contributions due to growth
        Assert.True(result.FinalNominalValue > 12000m);
        Assert.True(result.TotalNominalGrowth > 0m);
    }

    #endregion

    #region Inflation Tests

    [Fact]
    public void Calculate_WithInflation_ReducesRealValue()
    {
        // Arrange - 1 year with 3% inflation
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 10000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0m,
            InflationRate: 0.03m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        // Nominal value stays the same
        Assert.Equal(10000m, result.FinalNominalValue);
        // Real value should be less due to inflation
        Assert.True(result.FinalRealValue < result.FinalNominalValue);
        Assert.True(result.FinalRealValue >= 9650m && result.FinalRealValue <= 9750m);
    }

    [Fact]
    public void Calculate_WithReturnAboveInflation_PositiveRealGrowth()
    {
        // Arrange - 7% return with 3% inflation = ~4% real return
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 10000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m,
            InflationRate: 0.03m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        // Both nominal and real should grow
        Assert.True(result.FinalNominalValue > 10000m);
        Assert.True(result.FinalRealValue > 10000m);
        Assert.True(result.TotalNominalGrowth > result.TotalRealGrowth);
        Assert.True(result.TotalRealGrowth > 0m);
    }

    [Fact]
    public void Calculate_WithReturnBelowInflation_NegativeRealGrowth()
    {
        // Arrange - 2% return with 5% inflation = -3% real return
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 10000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.02m,
            InflationRate: 0.05m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        // Nominal grows, but real value decreases
        Assert.True(result.FinalNominalValue > 10000m);
        Assert.True(result.FinalRealValue < 10000m);
        Assert.True(result.TotalNominalGrowth > 0m);
        Assert.True(result.TotalRealGrowth < 0m);
    }

    #endregion

    #region Projection Tests

    [Fact]
    public void Calculate_GeneratesProjectionsForEachDay()
    {
        // Arrange - 10 days
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        // Should have 10 projections (one per day including start and end)
        Assert.Equal(10, result.Projections.Count);
        Assert.Equal(startDate, result.Projections.First().Date);
        Assert.Equal(endDate, result.Projections.Last().Date);
    }

    [Fact]
    public void Calculate_ProjectionPointsHaveCorrectStructure()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 5);
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m,
            InflationRate: 0.03m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        foreach (var point in result.Projections)
        {
            Assert.True(point.NominalValue >= 0);
            Assert.True(point.RealValue >= 0);
            Assert.True(point.TotalContributed >= 0);
            Assert.NotEqual(default(DateTime), point.Date);
        }
    }

    #endregion

    #region Monthly Projection Tests

    [Fact]
    public void CalculateMonthly_GeneratesMonthlyProjections()
    {
        // Arrange - 1 year = 12 months
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 1);
        var input = new InvestmentProjectionInput(
            InitialBalance: 10000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m
        );

        // Act
        var result = InvestmentProjectionCalculator.CalculateMonthly(input);

        // Assert
        // Should have 12 monthly projections
        Assert.Equal(12, result.Projections.Count);
    }

    [Fact]
    public void CalculateMonthly_AggregatesMonthlyContributions()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 3, 1);
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: new[]
            {
                // Multiple contributions in February
                new InvestmentContribution(new DateTime(2024, 2, 5), 100m),
                new InvestmentContribution(new DateTime(2024, 2, 15), 200m),
                new InvestmentContribution(new DateTime(2024, 2, 25), 150m)
            },
            NominalAnnualReturn: 0m,
            InflationRate: 0m
        );

        // Act
        var result = InvestmentProjectionCalculator.CalculateMonthly(input);

        // Assert
        // Should aggregate all February contributions (450 total)
        Assert.Equal(1450m, result.TotalContributions);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculate_WithZeroInitialBalance_WorksCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 12, 31);
        var input = new InvestmentProjectionInput(
            InitialBalance: 0m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: new[]
            {
                new InvestmentContribution(new DateTime(2024, 6, 1), 1000m)
            },
            NominalAnnualReturn: 0.10m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        Assert.Equal(1000m, result.TotalContributions);
        Assert.True(result.FinalNominalValue >= 1000m);
    }

    [Fact]
    public void Calculate_WithSameDayContributions_AggregatesCorrectly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 5);
        var input = new InvestmentProjectionInput(
            InitialBalance: 0m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: new[]
            {
                new InvestmentContribution(new DateTime(2024, 1, 3), 100m),
                new InvestmentContribution(new DateTime(2024, 1, 3), 200m),
                new InvestmentContribution(new DateTime(2024, 1, 3), 300m)
            },
            NominalAnnualReturn: 0m,
            InflationRate: 0m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        Assert.Equal(600m, result.TotalContributions);
        Assert.Equal(600m, result.FinalNominalValue);
    }

    [Fact]
    public void Calculate_WithOneDayProjection_Works()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 1);
        var input = new InvestmentProjectionInput(
            InitialBalance: 1000m,
            StartDate: startDate,
            EndDate: endDate,
            Contributions: Array.Empty<InvestmentContribution>(),
            NominalAnnualReturn: 0.07m
        );

        // Act
        var result = InvestmentProjectionCalculator.Calculate(input);

        // Assert
        Assert.Single(result.Projections);
        Assert.Equal(1000m, result.FinalNominalValue);
    }

    #endregion
}
