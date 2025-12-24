# WI-P4-002: CalculatorEndpoints Integration Tests

## Objective
Create integration tests for all `/api/calculators` endpoints.

## Context
- 5 calculator endpoints to test
- Each endpoint has specific business logic
- Tests should verify calculations are correct, not just HTTP status

## Files to Create

### `FinanceEngine.Tests/Endpoints/CalculatorEndpointsTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
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

    // Implement these 8 tests...
}
```

## Required Tests

### 1. CalculateSpendable_ValidRequest_ReturnsBreakdown

```csharp
[Fact]
public async Task CalculateSpendable_ValidRequest_ReturnsBreakdown()
{
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

    var response = await client.PostAsJsonAsync("/api/calculators/spendable", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(result.TryGetProperty("spendableNow", out _));
    Assert.True(result.TryGetProperty("breakdown", out _));
}
```

### 2. CalculateBurnRate_WithSpendingEvents_ReturnsRates

```csharp
[Fact]
public async Task CalculateBurnRate_WithSpendingEvents_ReturnsRates()
{
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

    var response = await client.PostAsJsonAsync("/api/calculators/burn-rate", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    // Verify burn rates are calculated
}
```

### 3. CalculateDebtAllocation_Avalanche_OrdersByHighestApr

```csharp
[Fact]
public async Task CalculateDebtAllocation_Avalanche_OrdersByHighestApr()
{
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

    var response = await client.PostAsJsonAsync("/api/calculators/debt-allocation", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    // Card B (22% APR) should get extra payment first
}
```

### 4. CalculateDebtAllocation_Snowball_OrdersByLowestBalance

```csharp
[Fact]
public async Task CalculateDebtAllocation_Snowball_OrdersByLowestBalance()
{
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
    // Card B (lower balance) should get extra payment first
}
```

### 5. CalculateDebtAllocation_Hybrid_CombinesStrategies

```csharp
[Fact]
public async Task CalculateDebtAllocation_Hybrid_CombinesStrategies()
{
    // Test Hybrid strategy
    var request = new
    {
        debts = new[] { /* ... */ },
        extraPaymentAmount = 200m,
        strategy = "Hybrid"
    };
    // Verify hybrid approach is applied
}
```

### 6. CalculateInvestmentProjection_ReturnsMonthlySnapshots

```csharp
[Fact]
public async Task CalculateInvestmentProjection_ReturnsMonthlySnapshots()
{
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

    var response = await client.PostAsJsonAsync("/api/calculators/investment-projection", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(result.TryGetProperty("projections", out var projections));
    Assert.True(projections.GetArrayLength() >= 12);  // Monthly snapshots
}
```

### 7. RunSimulation_WithDebts_CalculatesDebtFreeDate

```csharp
[Fact]
public async Task RunSimulation_WithDebts_CalculatesDebtFreeDate()
{
    var client = _factory.CreateClient();
    var request = new
    {
        startDate = "2025-01-01",
        endDate = "2030-01-01",
        initialCash = 5000m,
        debts = new[]
        {
            new
            {
                name = "Card A",
                balance = 5000m,
                annualPercentageRate = 0.20m,
                minimumPayment = 200m
            }
        }
    };

    var response = await client.PostAsJsonAsync("/api/calculators/simulation", request);

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(result.TryGetProperty("debtFreeDate", out _));
}
```

### 8. RunSimulation_TracksInterestPaid

```csharp
[Fact]
public async Task RunSimulation_TracksInterestPaid()
{
    // Same setup as above
    // Assert totalInterestPaid is > 0 and reasonable
    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
    Assert.True(result.TryGetProperty("totalInterestPaid", out var interest));
    Assert.True(interest.GetDecimal() > 0);
}
```

## Endpoints Summary

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/calculators/spendable` | Calculate safe-to-spend amount |
| POST | `/api/calculators/burn-rate` | Calculate spending rate over time windows |
| POST | `/api/calculators/debt-allocation` | Allocate extra payments across debts |
| POST | `/api/calculators/investment-projection` | Project investment growth |
| POST | `/api/calculators/simulation` | Run full debt payoff simulation |

## Request/Response Types

See `FinanceEngine.Api/Endpoints/CalculatorEndpoints.cs` for full request types:
- `SpendableRequest`
- `BurnRateRequest`
- `DebtAllocationRequest`
- `InvestmentProjectionRequest`
- `SimulationRequest`

## Acceptance Criteria

- [ ] All 8 tests pass
- [ ] Spendable calculation returns breakdown
- [ ] Burn rate returns 7-day and 30-day rates
- [ ] Debt allocation strategies work correctly (Avalanche, Snowball, Hybrid)
- [ ] Investment projection returns monthly snapshots
- [ ] Simulation calculates debt-free date and interest paid

## Verification

```bash
dotnet test --filter "FullyQualifiedName~CalculatorEndpoints"
# Should output: "Passed: 8"
```

## Existing Code References

- `CalculatorEndpoints.cs` - Endpoint implementations
- `FinanceEngine/Calculators/` - Calculator implementations
- `FinanceEngine/Models/Inputs/` - Input models
