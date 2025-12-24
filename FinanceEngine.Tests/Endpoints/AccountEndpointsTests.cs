using System.Net;
using System.Net.Http.Json;
using FinanceEngine.Api.Endpoints;
using FinanceEngine.Api.Models;
using FinanceEngine.Data;
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

    [Fact]
    public async Task GetAllAccounts_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/accounts");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountDto>>();
        Assert.NotNull(accounts);
    }

    [Fact]
    public async Task GetAccountById_ExistingId_ReturnsAccount()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create an account first
        var createRequest = new CreateAccountRequest(
            Name: "Test Savings",
            Type: "Cash",
            InitialBalance: 5000m
        );
        var createResponse = await client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Act
        var response = await client.GetAsync($"/api/accounts/{createdAccount!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);
        Assert.Equal(createdAccount.Id, account.Id);
        Assert.Equal("Test Savings", account.Name);
        Assert.Equal("Cash", account.Type);
        Assert.Equal(5000m, account.InitialBalance);
    }

    [Fact]
    public async Task GetAccountById_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/accounts/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_ValidCashAccount_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateAccountRequest(
            Name: "Test Checking",
            Type: "Cash",
            InitialBalance: 1000.00m
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);
        Assert.Equal("Test Checking", account.Name);
        Assert.Equal("Cash", account.Type);
        Assert.Equal(1000.00m, account.InitialBalance);
    }

    [Fact]
    public async Task CreateAccount_ValidDebtAccount_WithApr_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateAccountRequest(
            Name: "Test Credit Card",
            Type: "Debt",
            InitialBalance: 5000.00m,
            AnnualPercentageRate: 0.1999m,  // 19.99%
            MinimumPayment: 100.00m
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/accounts", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var account = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(account);
        Assert.Equal("Test Credit Card", account.Name);
        Assert.Equal("Debt", account.Type);
        Assert.Equal(5000.00m, account.InitialBalance);
        Assert.Equal(0.1999m, account.AnnualPercentageRate);
        Assert.Equal(100.00m, account.MinimumPayment);
    }

    [Fact]
    public async Task UpdateAccount_ValidRequest_ReturnsUpdated()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create an account first
        var createRequest = new CreateAccountRequest(
            Name: "Original Name",
            Type: "Debt",
            InitialBalance: 2000m,
            AnnualPercentageRate: 0.15m,
            MinimumPayment: 50m
        );
        var createResponse = await client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Update the account
        var updateRequest = new UpdateAccountRequest(
            Name: "Updated Name",
            AnnualPercentageRate: 0.20m,
            MinimumPayment: 75m
        );

        // Act
        var response = await client.PutAsJsonAsync($"/api/accounts/{createdAccount!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var updated = await response.Content.ReadFromJsonAsync<AccountDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Debt", updated.Type);
        Assert.Equal(2000m, updated.InitialBalance);
        Assert.Equal(0.20m, updated.AnnualPercentageRate);
        Assert.Equal(75m, updated.MinimumPayment);
    }

    [Fact]
    public async Task DeleteAccount_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create an account
        var createRequest = new CreateAccountRequest(
            Name: "To Delete",
            Type: "Cash",
            InitialBalance: 500m
        );
        var createResponse = await client.PostAsJsonAsync("/api/accounts", createRequest);
        var createdAccount = await createResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Act
        var response = await client.DeleteAsync($"/api/accounts/{createdAccount!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        // Verify the account is soft deleted (returns 404)
        var getResponse = await client.GetAsync($"/api/accounts/{createdAccount.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAccountBalance_WithEvents_CalculatesCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create a Cash account with initialBalance = 1000
        var createAccountRequest = new CreateAccountRequest(
            Name: "Test Balance Account",
            Type: "Cash",
            InitialBalance: 1000m
        );
        var createAccountResponse = await client.PostAsJsonAsync("/api/accounts", createAccountRequest);
        var account = await createAccountResponse.Content.ReadFromJsonAsync<AccountDto>();

        // Create an Income event for +500
        var incomeEvent = new CreateEventRequest(
            Date: DateTime.UtcNow,
            Type: "Income",
            Amount: 500m,
            Description: "Test Income",
            AccountId: account!.Id
        );
        await client.PostAsJsonAsync("/api/events", incomeEvent);

        // Create an Expense event for -200
        var expenseEvent = new CreateEventRequest(
            Date: DateTime.UtcNow,
            Type: "Expense",
            Amount: 200m,
            Description: "Test Expense",
            AccountId: account.Id
        );
        await client.PostAsJsonAsync("/api/events", expenseEvent);

        // Act
        var response = await client.GetAsync($"/api/accounts/{account.Id}/balance");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<BalanceResponse>();
        Assert.NotNull(result);
        Assert.Equal(account.Id, result.AccountId);
        Assert.Equal(1300m, result.Balance); // 1000 + 500 - 200 = 1300
    }
}

// Helper record for balance response deserialization
public record BalanceResponse(int AccountId, decimal Balance);

// Helper record for event creation (matching EventEndpoints)
public record CreateEventRequest(
    DateTime Date,
    string Type,
    decimal Amount,
    string? Description = null,
    int? AccountId = null,
    int? TargetAccountId = null
);

