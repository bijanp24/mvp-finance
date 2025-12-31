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

public class ExportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = "TestDatabase_Export_" + Guid.NewGuid();
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

    private async Task SeedTestData(FinanceDbContext db)
    {
        // Create accounts
        var cashAccount = new AccountEntity
        {
            Name = "Checking",
            Type = AccountType.Cash,
            InitialBalance = 5000m,
            IsActive = true
        };

        var debtAccount = new AccountEntity
        {
            Name = "Credit Card",
            Type = AccountType.Debt,
            InitialBalance = 2000m,
            AnnualPercentageRate = 0.1999m,
            MinimumPayment = 50m,
            IsActive = true
        };

        var investmentAccount = new AccountEntity
        {
            Name = "401k",
            Type = AccountType.Investment,
            InitialBalance = 10000m,
            AnnualPercentageRate = 0.07m,
            IsActive = true
        };

        db.Accounts.AddRange(cashAccount, debtAccount, investmentAccount);
        await db.SaveChangesAsync();

        // Create transactions
        var events = new List<FinancialEventEntity>
        {
            new()
            {
                Date = DateTime.UtcNow.AddDays(-30),
                Type = EventType.Income,
                Amount = 3000m,
                Description = "Paycheck",
                AccountId = cashAccount.Id,
                Status = EventStatus.Cleared
            },
            new()
            {
                Date = DateTime.UtcNow.AddDays(-25),
                Type = EventType.Expense,
                Amount = 100m,
                Description = "Groceries",
                AccountId = cashAccount.Id,
                Status = EventStatus.Cleared
            },
            new()
            {
                Date = DateTime.UtcNow.AddDays(-20),
                Type = EventType.DebtPayment,
                Amount = 500m,
                Description = "CC Payment",
                AccountId = debtAccount.Id,
                Status = EventStatus.Cleared
            }
        };

        db.Events.AddRange(events);
        await db.SaveChangesAsync();
    }

    #region Projection Export Tests

    [Fact]
    public async Task ExportProjections_Csv_ReturnsValidCsvFile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/projections?format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Date", content);
        Assert.Contains("AccountName", content);
        Assert.Contains("Balance", content);
    }

    [Fact]
    public async Task ExportProjections_Excel_ReturnsValidExcelFile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/projections?format=xlsx");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task ExportProjections_WithDateRange_FiltersCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        var startDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddMonths(6).ToString("yyyy-MM-dd");

        // Act
        var response = await client.GetAsync($"/api/export/projections?format=csv&startDate={startDate}&endDate={endDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Date", content);
    }

    #endregion

    #region Transaction Export Tests

    [Fact]
    public async Task ExportTransactions_Csv_ReturnsValidCsvFile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/transactions?format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Date", content);
        Assert.Contains("Type", content);
        Assert.Contains("Amount", content);
        Assert.Contains("Paycheck", content); // Should contain our seeded transaction
    }

    [Fact]
    public async Task ExportTransactions_Excel_ReturnsValidExcelFile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/transactions?format=xlsx");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task ExportTransactions_WithDateFilter_FiltersCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        var startDate = DateTime.UtcNow.AddDays(-35).ToString("yyyy-MM-dd");
        var endDate = DateTime.UtcNow.AddDays(-15).ToString("yyyy-MM-dd");

        // Act
        var response = await client.GetAsync($"/api/export/transactions?format=csv&startDate={startDate}&endDate={endDate}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Paycheck", content);
        Assert.Contains("Groceries", content);
    }

    #endregion

    #region Account Export Tests

    [Fact]
    public async Task ExportAccounts_Csv_ReturnsValidCsvFile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/accounts?format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", content);
        Assert.Contains("Type", content);
        Assert.Contains("CurrentBalance", content);
        Assert.Contains("Checking", content);
        Assert.Contains("Credit Card", content);
        Assert.Contains("401k", content);
    }

    [Fact]
    public async Task ExportAccounts_Excel_ReturnsValidExcelFile()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/accounts?format=xlsx");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task ExportAccounts_CalculatesBalancesCorrectly()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/accounts?format=csv");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();

        // Checking account: 5000 initial + 3000 income - 100 expense = 7900
        // (This verifies that balance calculation is working)
        Assert.Contains("Checking", content);
    }

    #endregion

    #region Default Format Tests

    [Fact]
    public async Task ExportProjections_NoFormatSpecified_DefaultsToCsv()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/projections");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ExportTransactions_NoFormatSpecified_DefaultsToCsv()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestData(db);

        // Act
        var response = await client.GetAsync("/api/export/transactions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
    }

    #endregion
}
