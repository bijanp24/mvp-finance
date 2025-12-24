# WI-P4-001: AccountEndpoints Integration Tests

## Objective
Create integration tests for all `/api/accounts` endpoints using the existing test patterns.

## Context
- Existing test pattern in `FinanceEngine.Tests/Endpoints/EventEndpointsTests.cs`
- Uses `WebApplicationFactory<Program>` with in-memory database
- xUnit with `[Fact]` attributes
- AccountEndpoints has 6 endpoints to test

## Files to Create

### `FinanceEngine.Tests/Endpoints/AccountEndpointsTests.cs`

```csharp
using System.Net;
using System.Net.Http.Json;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceEngine.Tests.Endpoints;

public class AccountEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccountEndpointsTests(WebApplicationFactory<Program> factory)
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

### 1. GetAllAccounts_ReturnsOk
```csharp
[Fact]
public async Task GetAllAccounts_ReturnsOk()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/accounts");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
    Assert.NotNull(accounts);
}
```

### 2. GetAccountById_ExistingId_ReturnsAccount
- Create an account first via POST
- GET that account by ID
- Assert 200 OK and correct data returned

### 3. GetAccountById_NonExistentId_ReturnsNotFound
- GET `/api/accounts/99999`
- Assert 404 NotFound

### 4. CreateAccount_ValidCashAccount_ReturnsCreated
```csharp
[Fact]
public async Task CreateAccount_ValidCashAccount_ReturnsCreated()
{
    var client = _factory.CreateClient();
    var request = new
    {
        name = "Test Checking",
        type = "Cash",
        initialBalance = 1000.00m
    };

    var response = await client.PostAsJsonAsync("/api/accounts", request);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var account = await response.Content.ReadFromJsonAsync<AccountDto>();
    Assert.NotNull(account);
    Assert.Equal("Test Checking", account.Name);
    Assert.Equal("Cash", account.Type);
}
```

### 5. CreateAccount_ValidDebtAccount_WithApr_ReturnsCreated
```csharp
var request = new
{
    name = "Test Credit Card",
    type = "Debt",
    initialBalance = 5000.00m,
    annualPercentageRate = 0.1999m,  // 19.99%
    minimumPayment = 100.00m
};
```
- Assert APR and minimum payment are stored correctly

### 6. UpdateAccount_ValidRequest_ReturnsUpdated
- Create an account
- PUT with updated name/balance
- Assert 200 and updated values returned

### 7. DeleteAccount_ExistingId_ReturnsNoContent
- Create an account
- DELETE that account
- Assert 204 NoContent
- GET the account again - should return 404 (soft deleted)

### 8. GetAccountBalance_WithEvents_CalculatesCorrectly
- Create a Cash account with initialBalance = 1000
- Create an Income event for +500
- Create an Expense event for -200
- GET `/api/accounts/{id}/balance`
- Assert balance = 1300 (1000 + 500 - 200)

## DTOs Reference

From `FinanceEngine.Api/Models/AccountModels.cs` or similar:

```csharp
// Request
record CreateAccountRequest(
    string Name,
    string Type,  // "Cash" | "Debt" | "Investment"
    decimal InitialBalance,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null
);

record UpdateAccountRequest(
    string? Name = null,
    decimal? InitialBalance = null,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null
);

// Response
record AccountDto(
    int Id,
    string Name,
    string Type,
    decimal InitialBalance,
    decimal CurrentBalance,  // Calculated from events
    decimal? AnnualPercentageRate,
    decimal? MinimumPayment
);
```

## Endpoints Summary

| Method | Endpoint | Success Code |
|--------|----------|--------------|
| GET | `/api/accounts` | 200 |
| GET | `/api/accounts/{id}` | 200 / 404 |
| POST | `/api/accounts` | 201 |
| PUT | `/api/accounts/{id}` | 200 / 404 |
| DELETE | `/api/accounts/{id}` | 204 / 404 |
| GET | `/api/accounts/{id}/balance` | 200 / 404 |

## Acceptance Criteria

- [ ] All 8 tests pass
- [ ] Tests use in-memory database (isolated)
- [ ] Tests follow AAA pattern (Arrange/Act/Assert)
- [ ] Balance calculation test verifies event sourcing works

## Verification

```bash
dotnet test --filter "FullyQualifiedName~AccountEndpoints"
# Should output: "Passed: 8"
```

## Existing Code References

- `EventEndpointsTests.cs` - Test pattern to follow
- `SettingsEndpointsTests.cs` - Another test pattern example
- `AccountEndpoints.cs` - Endpoint implementations
- `IAccountService.cs` / `AccountService.cs` - Business logic
