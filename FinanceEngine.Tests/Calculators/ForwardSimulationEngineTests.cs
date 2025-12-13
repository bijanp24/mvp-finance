using FinanceEngine.Calculators;
using FinanceEngine.Models;
using FinanceEngine.Models.Inputs;

namespace FinanceEngine.Tests.Calculators;

public class ForwardSimulationEngineTests
{
    #region Input Validation Tests

    [Fact]
    public void Simulate_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ForwardSimulationEngine.Simulate(null));
    }

    [Fact]
    public void Simulate_WithEndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var input = new ForwardSimulationInput(
            StartDate: new DateTime(2024, 12, 31),
            EndDate: new DateTime(2024, 1, 1),
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>()
        );

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ForwardSimulationEngine.Simulate(input));
    }

    [Fact]
    public void Simulate_WithNullDebts_ThrowsArgumentNullException()
    {
        // Arrange
        var input = new ForwardSimulationInput(
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 12, 31),
            InitialCash: 1000m,
            Debts: null,
            Events: Array.Empty<SimulationEvent>()
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ForwardSimulationEngine.Simulate(input));
    }

    [Fact]
    public void Simulate_WithNullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var input = new ForwardSimulationInput(
            StartDate: new DateTime(2024, 1, 1),
            EndDate: new DateTime(2024, 12, 31),
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: null
        );

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ForwardSimulationEngine.Simulate(input));
    }

    #endregion

    #region Basic Simulation Tests

    [Fact]
    public void Simulate_WithNoDebtsOrEvents_MaintainsCashBalance()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(10, result.Snapshots.Count);
        Assert.Equal(1000m, result.FinalCashBalance);
        Assert.Equal(0m, result.TotalInterestPaid);
    }

    [Fact]
    public void Simulate_WithIncomeEvents_IncreasesCash()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: new[]
            {
                new SimulationEvent(new DateTime(2024, 1, 5), SimulationEventType.Income, "Paycheck", 2000m)
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(3000m, result.FinalCashBalance); // 1000 + 2000
    }

    [Fact]
    public void Simulate_WithExpenseEvents_DecreasesCash()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: new[]
            {
                new SimulationEvent(new DateTime(2024, 1, 5), SimulationEventType.Expense, "Groceries", 200m)
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(800m, result.FinalCashBalance); // 1000 - 200
    }

    #endregion

    #region Debt Tests

    [Fact]
    public void Simulate_WithDebt_AccruesInterestDaily()
    {
        // Arrange - 10 days with debt, no payments
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[]
            {
                new DebtAccount("Credit Card", 1000m, 0.20m, 25m) // 20% APR
            },
            Events: Array.Empty<SimulationEvent>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // Debt should grow due to interest
        Assert.True(result.FinalDebtBalances["Credit Card"] > 1000m);
        Assert.True(result.TotalInterestPaid > 0m);
        Assert.Null(result.DebtFreeDate); // Not paid off
    }

    [Fact]
    public void Simulate_WithDebtPayment_ReducesDebtBalance()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[]
            {
                new DebtAccount("Credit Card", 1000m, 0.18m, 25m)
            },
            Events: new[]
            {
                new SimulationEvent(
                    new DateTime(2024, 1, 5),
                    SimulationEventType.DebtPayment,
                    "Payment",
                    500m,
                    "Credit Card"
                )
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // Cash reduced by payment
        Assert.Equal(500m, result.FinalCashBalance); // 1000 - 500
        // Debt reduced by payment (plus some interest)
        Assert.True(result.FinalDebtBalances["Credit Card"] < 1000m);
        Assert.True(result.FinalDebtBalances["Credit Card"] > 500m); // Interest accrued
    }

    [Fact]
    public void Simulate_WithDebtCharge_IncreasesDebtBalance()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[]
            {
                new DebtAccount("Credit Card", 500m, 0.18m, 25m)
            },
            Events: new[]
            {
                new SimulationEvent(
                    new DateTime(2024, 1, 5),
                    SimulationEventType.DebtCharge,
                    "Purchase",
                    300m,
                    "Credit Card"
                )
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // Debt increased by charge (plus interest)
        Assert.True(result.FinalDebtBalances["Credit Card"] > 800m);
    }

    [Fact]
    public void Simulate_PayingOffDebt_SetsDebtFreeDate()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var payoffDate = new DateTime(2024, 1, 5);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 2000m,
            Debts: new[]
            {
                new DebtAccount("Credit Card", 1000m, 0.18m, 25m)
            },
            Events: new[]
            {
                new SimulationEvent(
                    payoffDate,
                    SimulationEventType.DebtPayment,
                    "Payoff",
                    1100m, // More than enough to cover debt + interest
                    "Credit Card"
                )
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.NotNull(result.DebtFreeDate);
        Assert.True(result.DebtFreeDate >= payoffDate);
        Assert.True(result.FinalDebtBalances["Credit Card"] <= 0.01m);
    }

    #endregion

    #region Multiple Debt Tests

    [Fact]
    public void Simulate_WithMultipleDebts_TracksEachSeparately()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 2000m,
            Debts: new[]
            {
                new DebtAccount("Card 1", 1000m, 0.18m, 25m),
                new DebtAccount("Card 2", 500m, 0.24m, 15m)
            },
            Events: new[]
            {
                new SimulationEvent(
                    new DateTime(2024, 1, 5),
                    SimulationEventType.DebtPayment,
                    "Payment to Card 1",
                    300m,
                    "Card 1"
                ),
                new SimulationEvent(
                    new DateTime(2024, 1, 6),
                    SimulationEventType.DebtPayment,
                    "Payment to Card 2",
                    200m,
                    "Card 2"
                )
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(2, result.FinalDebtBalances.Count);
        Assert.True(result.FinalDebtBalances.ContainsKey("Card 1"));
        Assert.True(result.FinalDebtBalances.ContainsKey("Card 2"));
        // Both debts should have accrued interest
        Assert.True(result.TotalInterestPaid > 0m);
    }

    #endregion

    #region Event Processing Tests

    [Fact]
    public void Simulate_ProcessesEventsInChronologicalOrder()
    {
        // Arrange - Events out of order in input
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 0m,
            Debts: Array.Empty<DebtAccount>(),
            Events: new[]
            {
                new SimulationEvent(new DateTime(2024, 1, 5), SimulationEventType.Expense, "Expense", 100m),
                new SimulationEvent(new DateTime(2024, 1, 3), SimulationEventType.Income, "Income", 500m),
                new SimulationEvent(new DateTime(2024, 1, 7), SimulationEventType.Expense, "Expense", 50m)
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // Final should be: 0 + 500 (Jan 3) - 100 (Jan 5) - 50 (Jan 7) = 350
        Assert.Equal(350m, result.FinalCashBalance);
    }

    [Fact]
    public void Simulate_WithMultipleEventsOnSameDay_ProcessesAll()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 5);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: new[]
            {
                new SimulationEvent(new DateTime(2024, 1, 3), SimulationEventType.Income, "Income 1", 100m),
                new SimulationEvent(new DateTime(2024, 1, 3), SimulationEventType.Income, "Income 2", 200m),
                new SimulationEvent(new DateTime(2024, 1, 3), SimulationEventType.Expense, "Expense", 50m)
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // 1000 + 100 + 200 - 50 = 1250
        Assert.Equal(1250m, result.FinalCashBalance);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void Simulate_GeneratesSnapshotForEachDay()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 5);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(5, result.Snapshots.Count);
        Assert.Equal(startDate, result.Snapshots.First().Date);
        Assert.Equal(endDate, result.Snapshots.Last().Date);
    }

    [Fact]
    public void Simulate_SnapshotsContainCorrectData()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 5);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[]
            {
                new DebtAccount("Card", 500m, 0.18m, 25m)
            },
            Events: Array.Empty<SimulationEvent>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        foreach (var snapshot in result.Snapshots)
        {
            Assert.NotEqual(default(DateTime), snapshot.Date);
            Assert.True(snapshot.CashBalance >= 0);
            Assert.NotNull(snapshot.DebtBalances);
            Assert.True(snapshot.TotalDebt >= 0);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Simulate_WithZeroAPR_NoInterestAccrued()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[]
            {
                new DebtAccount("0% Card", 1000m, 0m, 25m) // 0% APR
            },
            Events: Array.Empty<SimulationEvent>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(1000m, result.FinalDebtBalances["0% Card"]);
        Assert.Equal(0m, result.TotalInterestPaid);
    }

    [Fact]
    public void Simulate_PaymentExceedingDebtBalance_PaysOffCompletely()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 5);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 5000m,
            Debts: new[]
            {
                new DebtAccount("Card", 100m, 0.18m, 25m)
            },
            Events: new[]
            {
                new SimulationEvent(
                    new DateTime(2024, 1, 3),
                    SimulationEventType.DebtPayment,
                    "Large Payment",
                    1000m, // Way more than debt
                    "Card"
                )
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // Should only deduct the actual debt amount (plus interest), not the full payment
        Assert.True(result.FinalDebtBalances["Card"] <= 0.01m);
        // Paid ~100 + 2 days interest, not full 1000
        Assert.True(result.FinalCashBalance > 4890m && result.FinalCashBalance < 5000m);
    }

    [Fact]
    public void Simulate_OneDaySimulation_Works()
    {
        // Arrange
        var date = new DateTime(2024, 1, 1);
        var input = new ForwardSimulationInput(
            StartDate: date,
            EndDate: date,
            InitialCash: 1000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Single(result.Snapshots);
        Assert.Equal(1000m, result.FinalCashBalance);
    }

    #endregion
}
