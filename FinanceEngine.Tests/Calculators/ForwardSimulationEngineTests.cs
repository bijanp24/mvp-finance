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

    #region Investment Tests

    [Fact]
    public void Simulate_WithInvestments_TracksGrowth()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 10000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("401k", 50000m, 0.07m) },
            RecurringContributions: Array.Empty<SimulationContribution>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.True(result.FinalInvestmentBalance > 50000m, "Investment should grow over time");
        Assert.True(result.TotalInvestmentGrowth > 0m, "Total growth should be positive");
        Assert.True(result.FinalInvestmentBalances["401k"].Balance > 50000m, "401k should grow");
        Assert.True(result.FinalInvestmentBalances["401k"].TotalGrowth > 0m);
    }

    [Fact]
    public void Simulate_WithContributions_ReducesCashIncreasesInvestments()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 31);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 10000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("401k", 1000m, 0.07m) },
            RecurringContributions: new[]
            {
                new SimulationContribution(new DateTime(2025, 1, 15), 500m, "Cash", "401k"),
                new SimulationContribution(new DateTime(2025, 1, 30), 500m, "Cash", "401k")
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(9000m, result.FinalCashBalance, 2); // 10000 - 500 - 500
        Assert.True(result.FinalInvestmentBalance > 2000m, "Investment should be contributions + initial + growth");
        Assert.Equal(1000m, result.TotalContributed);
        Assert.Equal(1000m, result.FinalInvestmentBalances["401k"].TotalContributed);
    }

    [Fact]
    public void Simulate_NetWorth_CombinesAllAccounts()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 5000m,
            Debts: new[] { new DebtAccount("Loan", 2000m, 0.05m, 100m) },
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("401k", 10000m, 0.07m) },
            RecurringContributions: Array.Empty<SimulationContribution>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        var expectedNetWorth = result.FinalCashBalance + result.FinalInvestmentBalance - result.FinalDebtBalances.Values.Sum();
        Assert.Equal(expectedNetWorth, result.FinalNetWorth, 2);
        Assert.True(result.FinalNetWorth > 10000m, "Net worth should be positive");
    }

    [Fact]
    public void Simulate_MillionaireDate_DetectedCorrectly()
    {
        // Arrange - Start with $999,000 in investments at high return
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 5000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("Portfolio", 995000m, 0.10m) },
            RecurringContributions: Array.Empty<SimulationContribution>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.NotNull(result.MillionaireDate);
        Assert.True(result.MillionaireDate >= startDate);
        Assert.True(result.MillionaireDate <= endDate);
        
        // Verify net worth at millionaire date is >= 1M
        var millionaireSnapshot = result.Snapshots.First(s => s.Date == result.MillionaireDate.Value);
        Assert.True(millionaireSnapshot.NetWorth >= 1_000_000m);
    }

    [Fact]
    public void Simulate_NoInvestments_BackwardsCompatible()
    {
        // Arrange - Old-style input without investments
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[] { new DebtAccount("Card", 500m, 0.18m, 25m) },
            Events: new[]
            {
                new SimulationEvent(new DateTime(2024, 1, 5), SimulationEventType.Income, "Paycheck", 2000m)
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(10, result.Snapshots.Count);
        Assert.Equal(3000m, result.FinalCashBalance); // 1000 + 2000
        Assert.Equal(0m, result.FinalInvestmentBalance);
        Assert.Equal(0m, result.TotalInvestmentGrowth);
        Assert.Equal(0m, result.TotalContributed);
        Assert.Empty(result.FinalInvestmentBalances);
        Assert.Null(result.MillionaireDate);
    }

    [Fact]
    public void Simulate_MultipleInvestmentAccounts_TrackedSeparately()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 6, 30);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 10000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[]
            {
                new InvestmentAccount("401k", 50000m, 0.07m),
                new InvestmentAccount("IRA", 30000m, 0.06m),
                new InvestmentAccount("Brokerage", 20000m, 0.08m)
            },
            RecurringContributions: new[]
            {
                new SimulationContribution(new DateTime(2025, 1, 15), 500m, "Cash", "401k"),
                new SimulationContribution(new DateTime(2025, 2, 15), 300m, "Cash", "IRA")
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(3, result.FinalInvestmentBalances.Count);
        Assert.True(result.FinalInvestmentBalances["401k"].Balance > 50000m);
        Assert.True(result.FinalInvestmentBalances["IRA"].Balance > 30000m);
        Assert.True(result.FinalInvestmentBalances["Brokerage"].Balance > 20000m);
        Assert.Equal(500m, result.FinalInvestmentBalances["401k"].TotalContributed);
        Assert.Equal(300m, result.FinalInvestmentBalances["IRA"].TotalContributed);
        Assert.Equal(0m, result.FinalInvestmentBalances["Brokerage"].TotalContributed);
        Assert.Equal(800m, result.TotalContributed);
    }

    [Fact]
    public void Simulate_InvestmentGrowth_CompoundsDaily()
    {
        // Arrange - Test that daily compounding works correctly
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31); // Full year
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 0m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("Test", 10000m, 0.10m) }, // 10% annual
            RecurringContributions: Array.Empty<SimulationContribution>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // With 10% annual rate, should be approximately 10000 * 1.10 = 11000
        // Due to daily compounding, it will be slightly higher
        Assert.True(result.FinalInvestmentBalance >= 11000m, "Should grow by at least 10%");
        Assert.True(result.FinalInvestmentBalance <= 11100m, "Should not grow by more than 11%");
        Assert.True(result.TotalInvestmentGrowth >= 1000m);
    }

    [Fact]
    public void Simulate_NetWorthSnapshot_UpdatesDaily()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 5);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 1000m,
            Debts: new[] { new DebtAccount("Loan", 500m, 0.05m, 50m) },
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("401k", 5000m, 0.07m) },
            RecurringContributions: Array.Empty<SimulationContribution>()
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        Assert.Equal(5, result.Snapshots.Count);
        foreach (var snapshot in result.Snapshots)
        {
            // Net worth should equal cash + investments - debt
            var calculatedNetWorth = snapshot.CashBalance + 
                                    snapshot.InvestmentBalances.Values.Sum(i => i.Balance) - 
                                    snapshot.TotalDebt;
            Assert.Equal(calculatedNetWorth, snapshot.NetWorth, 2);
        }
    }

    [Fact]
    public void Simulate_ContributionToNonExistentAccount_Ignored()
    {
        // Arrange
        var startDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 1, 10);
        var input = new ForwardSimulationInput(
            StartDate: startDate,
            EndDate: endDate,
            InitialCash: 10000m,
            Debts: Array.Empty<DebtAccount>(),
            Events: Array.Empty<SimulationEvent>(),
            InvestmentAccounts: new[] { new InvestmentAccount("401k", 1000m, 0.07m) },
            RecurringContributions: new[]
            {
                new SimulationContribution(new DateTime(2025, 1, 5), 500m, "Cash", "NonExistent")
            }
        );

        // Act
        var result = ForwardSimulationEngine.Simulate(input);

        // Assert
        // Cash should still be deducted even if target doesn't exist
        Assert.Equal(9500m, result.FinalCashBalance, 2);
        // But contribution shouldn't be tracked since it went nowhere
        Assert.Equal(500m, result.TotalContributed);
    }

    #endregion
}
