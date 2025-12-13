using FinanceEngine.Calculators;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;

namespace FinanceEngine.Tests.Calculators;

public class SpendableCalculatorTests
{
    #region Input Validation Tests

    [Fact]
    public void Calculate_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SpendableCalculator.Calculate(null));
    }

    [Fact]
    public void Calculate_WithNegativeAvailableCash_ThrowsArgumentException()
    {
        // Arrange
        var input = new SpendableInput(
            AvailableCash: -100m,
            CalculationDate: DateTime.Now,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SpendableCalculator.Calculate(input));
    }

    #endregion

    #region Basic Calculation Tests

    [Fact]
    public void Calculate_WithOnlyCash_ReturnsFullAmountAsSpendable()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        Assert.Equal(1000m, result.SpendableNow);
        Assert.Equal(0m, result.Breakdown.TotalObligations);
        Assert.Equal(0m, result.Breakdown.SafetyBuffer);
        Assert.Equal(0, result.Breakdown.DaysUntilNextPaycheck);
    }

    [Fact]
    public void Calculate_WithCashAndObligations_SubtractsObligations()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 200m, "Rent"),
                new Obligation(new DateTime(2024, 1, 22), 50m, "Utilities")
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Spendable = 1000 - 250 (obligations) = 750
        Assert.Equal(750m, result.SpendableNow);
        Assert.Equal(250m, result.Breakdown.TotalObligations);
    }

    [Fact]
    public void Calculate_WithManualSafetyBuffer_UsesSpecifiedBuffer()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: Array.Empty<IncomeEvent>(),
            ManualSafetyBuffer: 200m
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Spendable = 1000 - 200 (buffer) = 800
        Assert.Equal(800m, result.SpendableNow);
        Assert.Equal(200m, result.Breakdown.SafetyBuffer);
    }

    [Fact]
    public void Calculate_WithEstimatedDailySpend_CalculatesBufferFromDailySpend()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 30); // 15 days away
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 2000m, "Salary") },
            EstimatedDailySpend: 20m
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // 15 days until paycheck * $20/day = $300 buffer
        Assert.Equal(15, result.Breakdown.DaysUntilNextPaycheck);
        Assert.Equal(300m, result.Breakdown.SafetyBuffer);
        // Spendable = 1000 - 300 (buffer) = 700
        Assert.Equal(700m, result.SpendableNow);
    }

    [Fact]
    public void Calculate_WithPlannedContributions_SubtractsContributions()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 30);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 2000m, "Salary") },
            PlannedContributions: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 100m, "401k"),
                new Obligation(new DateTime(2024, 1, 25), 50m, "Savings")
            }
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Spendable = 1000 - 150 (contributions) = 850
        Assert.Equal(850m, result.SpendableNow);
        Assert.Equal(150m, result.Breakdown.PlannedContributions);
    }

    [Fact]
    public void Calculate_FullScenario_CalculatesCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 30); // 15 days
        var input = new SpendableInput(
            AvailableCash: 2000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 800m, "Rent"),
                new Obligation(new DateTime(2024, 1, 22), 100m, "Utilities")
            },
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 3000m, "Salary") },
            PlannedContributions: new[]
            {
                new Obligation(new DateTime(2024, 1, 25), 200m, "Savings")
            },
            EstimatedDailySpend: 30m
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Safety Buffer = 15 days * $30/day = $450
        // Spendable = 2000 - 900 (obligations) - 450 (buffer) - 200 (contributions) = 450
        Assert.Equal(450m, result.SpendableNow);
        Assert.Equal(900m, result.Breakdown.TotalObligations);
        Assert.Equal(450m, result.Breakdown.SafetyBuffer);
        Assert.Equal(200m, result.Breakdown.PlannedContributions);
    }

    #endregion

    #region Paycheck Scenarios

    [Fact]
    public void Calculate_WithNoUpcomingIncome_IncludesAllObligations()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 100m, "Bill 1"),
                new Obligation(new DateTime(2024, 2, 20), 100m, "Bill 2"),
                new Obligation(new DateTime(2024, 3, 20), 100m, "Bill 3")
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        Assert.Null(result.NextPaycheckDate);
        Assert.Equal(0, result.Breakdown.DaysUntilNextPaycheck);
        // All obligations counted since no paycheck to limit the window
        Assert.Equal(300m, result.Breakdown.TotalObligations);
    }

    [Fact]
    public void Calculate_WithMultiplePaychecks_UsesNextPaycheck()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 100m, "Before first paycheck"),
                new Obligation(new DateTime(2024, 2, 5), 100m, "Between paychecks"),
                new Obligation(new DateTime(2024, 2, 20), 100m, "After second paycheck")
            },
            UpcomingIncome: new[]
            {
                new IncomeEvent(new DateTime(2024, 1, 30), 2000m, "Paycheck 1"),
                new IncomeEvent(new DateTime(2024, 2, 15), 2000m, "Paycheck 2")
            }
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        Assert.Equal(new DateTime(2024, 1, 30), result.NextPaycheckDate);
        // Only obligation before first paycheck should be counted
        Assert.Equal(100m, result.Breakdown.TotalObligations);
    }

    [Fact]
    public void Calculate_WithObligationOnPaycheckDate_IncludesObligation()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 30);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(paycheckDate, 500m, "Rent on payday")
            },
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 2000m, "Salary") }
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Obligation on exact paycheck date should be included
        Assert.Equal(500m, result.Breakdown.TotalObligations);
    }

    #endregion

    #region Expected Cash Projection

    [Fact]
    public void Calculate_ProjectsExpectedCashAtPaycheck()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 30); // 15 days
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 500m, "Rent")
            },
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 2000m, "Salary") },
            EstimatedDailySpend: 20m
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Expected = 1000 (current) - 500 (obligations) - (15 days * $20) + 2000 (paycheck)
        // Expected = 1000 - 500 - 300 + 2000 = 2200
        Assert.Equal(2200m, result.ExpectedCashAtNextPaycheck);
    }

    [Fact]
    public void Calculate_WithoutDailySpend_ProjectsWithoutSpendingEstimate()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 30);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 500m, "Rent")
            },
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 2000m, "Salary") }
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Expected = 1000 - 500 + 2000 = 2500 (no spending estimate)
        Assert.Equal(2500m, result.ExpectedCashAtNextPaycheck);
    }

    #endregion

    #region Conservative Scenario

    [Fact]
    public void Calculate_WithDailySpend_GeneratesConservativeScenario()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var paycheckDate = new DateTime(2024, 1, 25); // 10 days
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: new[] { new IncomeEvent(paycheckDate, 2000m, "Salary") },
            EstimatedDailySpend: 20m
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        Assert.NotNull(result.ConservativeScenario);
        Assert.Equal("Conservative", result.ConservativeScenario.ScenarioName);

        // Conservative daily spend = $20 * 1.5 = $30
        Assert.Equal(30m, result.ConservativeScenario.EstimatedDailySpend);

        // Conservative buffer = ($20 * 10 days) * 1.5 = $300
        // Conservative spendable = 1000 - 300 = 700
        Assert.Equal(700m, result.ConservativeScenario.SpendableAmount);

        // Conservative expected = 1000 - (30 * 10) + 2000 = 2700
        Assert.Equal(2700m, result.ConservativeScenario.ExpectedCashAtPaycheck);
    }

    [Fact]
    public void Calculate_WithoutDailySpend_NoConservativeScenario()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        Assert.Null(result.ConservativeScenario);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculate_WhenSpendableGoesNegative_ReturnsNegativeAmount()
    {
        // Arrange - More obligations than cash
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 500m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 1000m, "Big expense")
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Spendable = 500 - 1000 = -500
        Assert.Equal(-500m, result.SpendableNow);
    }

    [Fact]
    public void Calculate_WithZeroAvailableCash_CalculatesCorrectly()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 0m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 20), 100m, "Bill")
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        Assert.Equal(-100m, result.SpendableNow);
    }

    [Fact]
    public void Calculate_IgnoresObligationsBeforeCalculationDate()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: new[]
            {
                new Obligation(new DateTime(2024, 1, 10), 200m, "Past obligation"),
                new Obligation(new DateTime(2024, 1, 20), 100m, "Future obligation")
            },
            UpcomingIncome: Array.Empty<IncomeEvent>()
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Should only count the future obligation
        Assert.Equal(100m, result.Breakdown.TotalObligations);
        Assert.Equal(900m, result.SpendableNow);
    }

    [Fact]
    public void Calculate_IgnoresIncomeBeforeCalculationDate()
    {
        // Arrange
        var calculationDate = new DateTime(2024, 1, 15);
        var input = new SpendableInput(
            AvailableCash: 1000m,
            CalculationDate: calculationDate,
            UpcomingObligations: Array.Empty<Obligation>(),
            UpcomingIncome: new[]
            {
                new IncomeEvent(new DateTime(2024, 1, 10), 2000m, "Past paycheck"),
                new IncomeEvent(new DateTime(2024, 1, 30), 2000m, "Next paycheck")
            }
        );

        // Act
        var result = SpendableCalculator.Calculate(input);

        // Assert
        // Should use the future paycheck, not the past one
        Assert.Equal(new DateTime(2024, 1, 30), result.NextPaycheckDate);
    }

    #endregion
}
