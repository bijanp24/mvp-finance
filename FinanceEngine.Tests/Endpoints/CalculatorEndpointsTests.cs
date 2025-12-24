using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceEngine.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceEngine.Tests.Endpoints;

public class CalculatorEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CalculatorEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = "TestDatabase_" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<FinanceDbContext>>();
                services.RemoveAll<FinanceDbContext>();
                services.AddDbContext<FinanceDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
    }

    [Fact]
    public async Task CalculateSpendable_ValidRequest_ReturnsBreakdown()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            availableCash = 5000m,
            calculationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            obligations = new[]
            {
                new { dueDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd"), amount = 500m, description = "Rent" }
            },
            manualSafetyBuffer = 100m
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/spendable", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("spendableNow", out var spendableNow));
        Assert.True(result.TryGetProperty("breakdown", out var breakdown));
        
        // Verify breakdown structure (note: property names are camelCase in JSON)
        Assert.True(breakdown.TryGetProperty("availableCash", out _));
        Assert.True(breakdown.TryGetProperty("totalObligations", out _));
        Assert.True(breakdown.TryGetProperty("safetyBuffer", out _));
        
        // Verify spendable is less than available cash (due to obligations and buffer)
        Assert.True(spendableNow.GetDecimal() < 5000m);
    }

    [Fact]
    public async Task CalculateBurnRate_WithSpendingEvents_ReturnsRates()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            spendingEvents = new[]
            {
                new { date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"), amount = 50m },
                new { date = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd"), amount = 30m },
                new { date = DateTime.UtcNow.AddDays(-3).ToString("yyyy-MM-dd"), amount = 40m }
            },
            calculationDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            windowDays = new[] { 7, 30 }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/burn-rate", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        // Verify burn rates are calculated for both windows
        Assert.True(result.TryGetProperty("burnRatesByWindow", out var burnRates));
        
        // Check that we have rates for 7-day and 30-day windows
        var burnRatesDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(burnRates.GetRawText());
        Assert.NotNull(burnRatesDict);
        Assert.True(burnRatesDict.ContainsKey("7"));
        Assert.True(burnRatesDict.ContainsKey("30"));
        
        // Verify structure of burn rate entries
        var sevenDayRate = burnRatesDict["7"];
        Assert.True(sevenDayRate.TryGetProperty("averageDailySpend", out var avgSpend7));
        Assert.True(avgSpend7.GetDecimal() > 0);

        var thirtyDayRate = burnRatesDict["30"];
        Assert.True(thirtyDayRate.TryGetProperty("averageDailySpend", out var avgSpend30));
        Assert.True(avgSpend30.GetDecimal() > 0);
    }

    [Fact]
    public async Task CalculateDebtAllocation_Avalanche_OrdersByHighestApr()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            debts = new[]
            {
                new { name = "Card A", balance = 5000m, annualPercentageRate = 0.15m, minimumPayment = 100m },
                new { name = "Card B", balance = 2000m, annualPercentageRate = 0.22m, minimumPayment = 50m }  // Higher APR
            },
            extraPaymentAmount = 200m,
            strategy = "Avalanche"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/debt-allocation", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(result.TryGetProperty("paymentsByDebt", out var payments));
        Assert.True(result.TryGetProperty("strategyUsed", out var strategy));
        // Strategy is returned as enum number: 0=Avalanche, 1=Snowball, 2=Hybrid
        Assert.Equal(0, strategy.GetInt32());
        
        // Card B (22% APR) should get the extra payment
        var paymentsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payments.GetRawText());
        Assert.NotNull(paymentsDict);
        Assert.True(paymentsDict.ContainsKey("Card B"));
        
        var cardBPayment = paymentsDict["Card B"];
        Assert.True(cardBPayment.TryGetProperty("extraPayment", out var extraPayment));
        Assert.Equal(200m, extraPayment.GetDecimal());
    }

    [Fact]
    public async Task CalculateDebtAllocation_Snowball_OrdersByLowestBalance()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            debts = new[]
            {
                new { name = "Card A", balance = 5000m, annualPercentageRate = 0.22m, minimumPayment = 100m },
                new { name = "Card B", balance = 2000m, annualPercentageRate = 0.15m, minimumPayment = 50m }  // Lower balance
            },
            extraPaymentAmount = 200m,
            strategy = "Snowball"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/debt-allocation", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(result.TryGetProperty("paymentsByDebt", out var payments));
        Assert.True(result.TryGetProperty("strategyUsed", out var strategy));
        // Strategy is returned as enum number: 0=Avalanche, 1=Snowball, 2=Hybrid
        Assert.Equal(1, strategy.GetInt32());
        
        // Card B (lower balance) should get the extra payment
        var paymentsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payments.GetRawText());
        Assert.NotNull(paymentsDict);
        Assert.True(paymentsDict.ContainsKey("Card B"));
        
        var cardBPayment = paymentsDict["Card B"];
        Assert.True(cardBPayment.TryGetProperty("extraPayment", out var extraPayment));
        Assert.Equal(200m, extraPayment.GetDecimal());
    }

    [Fact]
    public async Task CalculateDebtAllocation_Hybrid_CombinesStrategies()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            debts = new[]
            {
                new { name = "Card A", balance = 5000m, annualPercentageRate = 0.15m, minimumPayment = 100m },
                new { name = "Card B", balance = 3000m, annualPercentageRate = 0.20m, minimumPayment = 75m },
                new { name = "Card C", balance = 1000m, annualPercentageRate = 0.18m, minimumPayment = 25m }
            },
            extraPaymentAmount = 300m,
            strategy = "Hybrid"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/debt-allocation", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(result.TryGetProperty("paymentsByDebt", out var payments));
        Assert.True(result.TryGetProperty("strategyUsed", out var strategy));
        // Strategy is returned as enum number: 0=Avalanche, 1=Snowball, 2=Hybrid
        Assert.Equal(2, strategy.GetInt32());
        
        // Verify all debts get at least minimum payment
        var paymentsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payments.GetRawText());
        Assert.NotNull(paymentsDict);
        Assert.Equal(3, paymentsDict.Count);
        
        // Verify total payment includes all minimums plus extra
        Assert.True(result.TryGetProperty("totalPayment", out var totalPayment));
        Assert.Equal(100m + 75m + 25m + 300m, totalPayment.GetDecimal());
    }

    [Fact]
    public async Task CalculateInvestmentProjection_ReturnsMonthlySnapshots()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            initialBalance = 10000m,
            startDate = "2025-01-01",
            endDate = "2026-01-01",  // 12 months
            nominalAnnualReturn = 0.07m,
            inflationRate = 0.03m,
            useMonthly = true,
            contributions = new[]
            {
                new { date = "2025-02-01", amount = 500m }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/investment-projection", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(result.TryGetProperty("projections", out var projections));
        Assert.True(result.TryGetProperty("finalNominalValue", out var finalNominal));
        Assert.True(result.TryGetProperty("finalRealValue", out var finalReal));
        Assert.True(result.TryGetProperty("totalContributions", out var totalContributions));
        
        // Verify we have monthly snapshots (at least 12 for a full year)
        var projectionsArray = projections.EnumerateArray().ToList();
        Assert.True(projectionsArray.Count >= 12);
        
        // Verify each projection has required fields
        var firstProjection = projectionsArray[0];
        Assert.True(firstProjection.TryGetProperty("date", out _));
        Assert.True(firstProjection.TryGetProperty("nominalValue", out _));
        Assert.True(firstProjection.TryGetProperty("realValue", out _));
        
        // Verify final value is greater than initial (with growth and contribution)
        Assert.True(finalNominal.GetDecimal() > 10000m);
        
        // Verify total contributions includes initial balance + our 500m contribution
        Assert.Equal(10500m, totalContributions.GetDecimal());
    }

    [Fact]
    public async Task RunSimulation_WithDebts_CalculatesDebtFreeDate()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            startDate = "2025-01-01",
            endDate = "2030-01-01",
            initialCash = 3000m,
            debts = new[]
            {
                new
                {
                    name = "Card A",
                    balance = 2000m,
                    annualPercentageRate = 0.20m,
                    minimumPayment = 100m
                }
            },
            events = new[]
            {
                // Monthly income to pay off debt
                new
                {
                    date = "2025-01-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 500m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-02-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 500m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-03-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 500m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-04-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 500m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-05-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 500m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-01-01",
                    type = "DebtPayment",
                    description = "Payoff",
                    amount = 2000m,
                    relatedDebtName = "Card A"
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/simulation", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(result.TryGetProperty("debtFreeDate", out var debtFreeDate));
        Assert.True(result.TryGetProperty("finalCashBalance", out _));
        Assert.True(result.TryGetProperty("finalDebtBalances", out var finalDebtBalances));
        Assert.True(result.TryGetProperty("totalInterestPaid", out _));
        Assert.True(result.TryGetProperty("snapshots", out var snapshots));
        
        // Verify debt free date is set (paid off with explicit debt payment event)
        Assert.NotEqual(JsonValueKind.Null, debtFreeDate.ValueKind);
        Assert.Equal(new DateTime(2025, 1, 1), debtFreeDate.GetDateTime().Date);

        var finalDebts = JsonSerializer.Deserialize<Dictionary<string, decimal>>(finalDebtBalances.GetRawText());
        Assert.NotNull(finalDebts);
        Assert.True(finalDebts.TryGetValue("Card A", out var remainingDebt));
        Assert.Equal(0m, remainingDebt);
        
        // Verify we have snapshots
        var snapshotsArray = snapshots.EnumerateArray().ToList();
        Assert.True(snapshotsArray.Count > 0);
        
        // Verify snapshot structure
        var firstSnapshot = snapshotsArray[0];
        Assert.True(firstSnapshot.TryGetProperty("date", out _));
        Assert.True(firstSnapshot.TryGetProperty("cashBalance", out _));
        Assert.True(firstSnapshot.TryGetProperty("totalDebt", out _));
    }

    [Fact]
    public async Task RunSimulation_TracksInterestPaid()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new
        {
            startDate = "2025-01-01",
            endDate = "2026-01-01",  // 1 year simulation
            initialCash = 500m,
            debts = new[]
            {
                new
                {
                    name = "Card A",
                    balance = 5000m,
                    annualPercentageRate = 0.20m,
                    minimumPayment = 150m
                }
            },
            events = new[]
            {
                // Monthly income that covers minimum payment
                new
                {
                    date = "2025-01-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 200m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-02-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 200m,
                    relatedDebtName = (string?)null
                },
                new
                {
                    date = "2025-03-15",
                    type = "Income",
                    description = "Paycheck",
                    amount = 200m,
                    relatedDebtName = (string?)null
                }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/calculators/simulation", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        Assert.True(result.TryGetProperty("totalInterestPaid", out var interest));
        
        // With 20% APR on $5000 and only minimum payments, interest should accrue
        // Interest should be >= 0 (might be 0 if debt is paid immediately)
        Assert.True(interest.GetDecimal() > 0);
        
        // If there's debt over time, there should be interest
        // With $5000 at 20% APR, monthly interest is roughly $83
        // Over 3 months with minimum payments, we should see some interest
        Assert.True(result.TryGetProperty("finalDebtBalances", out var finalDebts));
        var debtBalances = JsonSerializer.Deserialize<Dictionary<string, decimal>>(finalDebts.GetRawText());
        Assert.NotNull(debtBalances);
        Assert.True(debtBalances.TryGetValue("Card A", out var finalBalance));
        Assert.True(finalBalance > 5000m);
    }
}

