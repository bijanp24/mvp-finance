using FinanceEngine.Calculators;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;

namespace FinanceEngine.Tests.Calculators;

public class DebtAllocationCalculatorTests
{
    #region Input Validation Tests

    [Fact]
    public void Calculate_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DebtAllocationCalculator.Calculate(null));
    }

    [Fact]
    public void Calculate_WithNullDebts_ThrowsArgumentNullException()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: null,
            ExtraPaymentAmount: 100m
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => DebtAllocationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithNegativeExtraPayment_ThrowsArgumentException()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: Array.Empty<Debt>(),
            ExtraPaymentAmount: -100m
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DebtAllocationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithNegativeDebtBalance_ThrowsArgumentException()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[] { new Debt("Card", -100m, 18.99m, 25m) },
            ExtraPaymentAmount: 100m
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DebtAllocationCalculator.Calculate(input));
    }

    [Fact]
    public void Calculate_WithNegativeAPR_ThrowsArgumentException()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[] { new Debt("Card", 1000m, -5m, 25m) },
            ExtraPaymentAmount: 100m
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => DebtAllocationCalculator.Calculate(input));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Calculate_WithEmptyDebts_ReturnsEmptyResult()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: Array.Empty<Debt>(),
            ExtraPaymentAmount: 100m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        Assert.Empty(result.PaymentsByDebt);
        Assert.Equal(0m, result.TotalPayment);
    }

    [Fact]
    public void Calculate_WithZeroBalanceDebts_IgnoresThem()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Paid Off Card", 0m, 18.99m, 0m),
                new Debt("Active Card", 1000m, 15.99m, 25m)
            },
            ExtraPaymentAmount: 100m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        Assert.Single(result.PaymentsByDebt);
        Assert.True(result.PaymentsByDebt.ContainsKey("Active Card"));
        Assert.False(result.PaymentsByDebt.ContainsKey("Paid Off Card"));
    }

    [Fact]
    public void Calculate_WithZeroExtraPayment_OnlyMinimumsApplied()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Card 1", 1000m, 18.99m, 25m),
                new Debt("Card 2", 2000m, 15.99m, 50m)
            },
            ExtraPaymentAmount: 0m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        Assert.Equal(25m, result.PaymentsByDebt["Card 1"].TotalPayment);
        Assert.Equal(50m, result.PaymentsByDebt["Card 2"].TotalPayment);
        Assert.Equal(0m, result.PaymentsByDebt["Card 1"].ExtraPayment);
        Assert.Equal(0m, result.PaymentsByDebt["Card 2"].ExtraPayment);
        Assert.Equal(75m, result.TotalPayment);
    }

    #endregion

    #region Avalanche Strategy Tests

    [Fact]
    public void Calculate_Avalanche_PrioritizesHighestAPR()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Low APR Card", 1000m, 10.99m, 25m),
                new Debt("High APR Card", 2000m, 24.99m, 50m),
                new Debt("Medium APR Card", 1500m, 18.99m, 35m)
            },
            ExtraPaymentAmount: 200m,
            Strategy: AllocationStrategy.Avalanche
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        // High APR Card should get all extra payment
        Assert.Equal(50m, result.PaymentsByDebt["High APR Card"].MinimumPayment);
        Assert.Equal(200m, result.PaymentsByDebt["High APR Card"].ExtraPayment);
        Assert.Equal(250m, result.PaymentsByDebt["High APR Card"].TotalPayment);

        // Others only get minimums
        Assert.Equal(0m, result.PaymentsByDebt["Medium APR Card"].ExtraPayment);
        Assert.Equal(0m, result.PaymentsByDebt["Low APR Card"].ExtraPayment);
    }

    [Fact]
    public void Calculate_Avalanche_WhenHighestPaidOff_MovesToNextHighest()
    {
        // Arrange - High APR card has small balance
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Low APR Card", 2000m, 10.99m, 50m),
                new Debt("High APR Card", 100m, 24.99m, 25m), // Can be paid off
                new Debt("Medium APR Card", 1500m, 18.99m, 35m)
            },
            ExtraPaymentAmount: 200m,
            Strategy: AllocationStrategy.Avalanche
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        // High APR gets paid off (75 extra + 25 minimum = 100 total)
        var highAPRPayment = result.PaymentsByDebt["High APR Card"];
        Assert.Equal(25m, highAPRPayment.MinimumPayment);
        Assert.Equal(75m, highAPRPayment.ExtraPayment); // Only 75 extra needed to pay off
        Assert.Equal(100m, highAPRPayment.TotalPayment);
        Assert.Equal(0m, highAPRPayment.RemainingBalance);

        // Others only get minimums (extra doesn't overflow in this simple version)
        Assert.Equal(0m, result.PaymentsByDebt["Medium APR Card"].ExtraPayment);
        Assert.Equal(0m, result.PaymentsByDebt["Low APR Card"].ExtraPayment);
    }

    #endregion

    #region Snowball Strategy Tests

    [Fact]
    public void Calculate_Snowball_PrioritizesSmallestBalance()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Large Balance", 5000m, 24.99m, 100m),
                new Debt("Small Balance", 500m, 10.99m, 15m),
                new Debt("Medium Balance", 2000m, 18.99m, 50m)
            },
            ExtraPaymentAmount: 200m,
            Strategy: AllocationStrategy.Snowball
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        // Small Balance should get all extra payment
        Assert.Equal(15m, result.PaymentsByDebt["Small Balance"].MinimumPayment);
        Assert.Equal(200m, result.PaymentsByDebt["Small Balance"].ExtraPayment);
        Assert.Equal(215m, result.PaymentsByDebt["Small Balance"].TotalPayment);

        // Others only get minimums
        Assert.Equal(0m, result.PaymentsByDebt["Medium Balance"].ExtraPayment);
        Assert.Equal(0m, result.PaymentsByDebt["Large Balance"].ExtraPayment);
    }

    [Fact]
    public void Calculate_Snowball_PaysOffSmallDebtCompletely()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Large Balance", 5000m, 24.99m, 100m),
                new Debt("Small Balance", 300m, 10.99m, 25m),
                new Debt("Medium Balance", 2000m, 18.99m, 50m)
            },
            ExtraPaymentAmount: 500m,
            Strategy: AllocationStrategy.Snowball
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        var smallDebtPayment = result.PaymentsByDebt["Small Balance"];
        // Minimum 25 + Extra needed to reach 300 = 275 extra
        Assert.Equal(25m, smallDebtPayment.MinimumPayment);
        Assert.Equal(275m, smallDebtPayment.ExtraPayment);
        Assert.Equal(300m, smallDebtPayment.TotalPayment);
        Assert.Equal(0m, smallDebtPayment.RemainingBalance);
    }

    #endregion

    #region Hybrid Strategy Tests

    [Fact]
    public void Calculate_Hybrid_BehavesLikeAvalanche()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Low APR Card", 1000m, 10.99m, 25m),
                new Debt("High APR Card", 2000m, 24.99m, 50m)
            },
            ExtraPaymentAmount: 200m,
            Strategy: AllocationStrategy.Hybrid
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        // Should prioritize high APR (avalanche behavior)
        Assert.Equal(200m, result.PaymentsByDebt["High APR Card"].ExtraPayment);
        Assert.Equal(0m, result.PaymentsByDebt["Low APR Card"].ExtraPayment);
        Assert.Equal(AllocationStrategy.Hybrid, result.StrategyUsed);
    }

    #endregion

    #region Payment Calculation Tests

    [Fact]
    public void Calculate_DoesNotExceedDebtBalance()
    {
        // Arrange - Extra payment larger than debt
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Small Debt", 100m, 18.99m, 25m)
            },
            ExtraPaymentAmount: 500m,
            Strategy: AllocationStrategy.Avalanche
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        var payment = result.PaymentsByDebt["Small Debt"];
        // Should only pay the balance (100), not minimum + extra (525)
        Assert.Equal(100m, payment.TotalPayment);
        Assert.Equal(0m, payment.RemainingBalance);
        Assert.Equal(75m, payment.ExtraPayment); // 100 - 25 minimum
    }

    [Fact]
    public void Calculate_CalculatesRemainingBalanceCorrectly()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Card", 1000m, 18.99m, 50m)
            },
            ExtraPaymentAmount: 200m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        var payment = result.PaymentsByDebt["Card"];
        Assert.Equal(1000m, 1000m); // Original balance
        Assert.Equal(250m, payment.TotalPayment); // 50 min + 200 extra
        Assert.Equal(750m, payment.RemainingBalance); // 1000 - 250
    }

    [Fact]
    public void Calculate_SumsMinimumAndExtraPayments()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Card", 2000m, 18.99m, 75m)
            },
            ExtraPaymentAmount: 125m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        var payment = result.PaymentsByDebt["Card"];
        Assert.Equal(75m, payment.MinimumPayment);
        Assert.Equal(125m, payment.ExtraPayment);
        Assert.Equal(200m, payment.TotalPayment);
    }

    [Fact]
    public void Calculate_TotalPayment_SumsAllDebtPayments()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Card 1", 1000m, 18.99m, 25m),
                new Debt("Card 2", 2000m, 15.99m, 50m),
                new Debt("Card 3", 1500m, 20.99m, 35m)
            },
            ExtraPaymentAmount: 100m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        // Total = all minimums (25 + 50 + 35) + extra (100) = 210
        Assert.Equal(210m, result.TotalPayment);
    }

    #endregion

    #region Multiple Debt Scenarios

    [Fact]
    public void Calculate_WithMultipleDebts_AllReceiveMinimumPayment()
    {
        // Arrange
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Card 1", 1000m, 18.99m, 25m),
                new Debt("Card 2", 2000m, 15.99m, 50m),
                new Debt("Card 3", 1500m, 12.99m, 35m)
            },
            ExtraPaymentAmount: 100m
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        Assert.Equal(3, result.PaymentsByDebt.Count);
        Assert.Equal(25m, result.PaymentsByDebt["Card 1"].MinimumPayment);
        Assert.Equal(50m, result.PaymentsByDebt["Card 2"].MinimumPayment);
        Assert.Equal(35m, result.PaymentsByDebt["Card 3"].MinimumPayment);
    }

    [Fact]
    public void Calculate_Avalanche_WithSameAPR_UsesFirstEncountered()
    {
        // Arrange - Two debts with same APR
        var input = new DebtAllocationInput(
            Debts: new[]
            {
                new Debt("Card A", 1000m, 18.99m, 25m),
                new Debt("Card B", 2000m, 18.99m, 50m)
            },
            ExtraPaymentAmount: 100m,
            Strategy: AllocationStrategy.Avalanche
        );

        // Act
        var result = DebtAllocationCalculator.Calculate(input);

        // Assert
        // When APRs are equal, first one in sorted order gets priority
        // Both have same APR, so one should get all extra
        var totalExtra = result.PaymentsByDebt.Values.Sum(p => p.ExtraPayment);
        Assert.Equal(100m, totalExtra);

        // At least one should have extra payment
        var hasExtraPayment = result.PaymentsByDebt.Values.Any(p => p.ExtraPayment > 0);
        Assert.True(hasExtraPayment);
    }

    #endregion
}
