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

public class CreditActionPlanEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreditActionPlanEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = "TestDatabase_CreditPlan_" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<FinanceDbContext>));
                services.AddDbContext<FinanceDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }

    private async Task SeedCards()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        if (db.Accounts.Any()) return;

        db.Accounts.AddRange(
            new AccountEntity { Name = "High APR Card", Type = AccountType.Debt, InitialBalance = 4000m, AnnualPercentageRate = 0.28m, MinimumPayment = 120m, IsActive = true },
            new AccountEntity { Name = "Low APR Card", Type = AccountType.Debt, InitialBalance = 3000m, AnnualPercentageRate = 0.12m, MinimumPayment = 80m, IsActive = true },
            new AccountEntity { Name = "Checking", Type = AccountType.Cash, InitialBalance = 25000m, IsActive = true });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Calculate_ReservesEmergencyFundThenPaysHighestApr()
    {
        var client = _factory.CreateClient();
        await SeedCards();

        var request = new
        {
            windfall = 25000m,
            emergencyFundMonths = 6,
            monthlyEssentialExpenses = 3000m,
            strategy = "Avalanche"
        };

        var response = await client.PostAsJsonAsync("/api/credit-action-plan/calculate", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(result);
        Assert.Equal("Avalanche", result.Strategy);
        Assert.Equal(18000m, result.EmergencyFundReserved); // 6 x 3000
        Assert.Equal(7000m, result.TotalDebtBefore);        // 4000 + 3000
        // $7,000 left after reserve clears all $7,000 of debt.
        Assert.Equal(0m, result.TotalDebtAfter);
        Assert.Equal("High APR Card", result.Steps[0].DebtName); // highest APR first
        Assert.True(result.Steps[0].IsFullyPaid);
    }

    [Fact]
    public async Task Calculate_NegativeWindfall_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/credit-action-plan/calculate", new { windfall = -5m });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Defaults_ReturnsDebtsAndSuggestedWindfall()
    {
        var client = _factory.CreateClient();
        await SeedCards();

        var result = await client.GetFromJsonAsync<DefaultsResponse>("/api/credit-action-plan/defaults");

        Assert.NotNull(result);
        Assert.Equal(25000m, result.SuggestedWindfall); // checking balance
        Assert.Equal(2, result.Debts.Count);
        Assert.Equal(6, result.DefaultEmergencyFundMonths);
    }

    private class PlanResponse
    {
        public string Strategy { get; set; } = "";
        public decimal EmergencyFundReserved { get; set; }
        public decimal TotalDebtBefore { get; set; }
        public decimal TotalDebtAfter { get; set; }
        public decimal TotalInterestSaved { get; set; }
        public List<StepDto> Steps { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    private class StepDto
    {
        public string DebtName { get; set; } = "";
        public decimal LumpSumApplied { get; set; }
        public bool IsFullyPaid { get; set; }
    }

    private class DefaultsResponse
    {
        public decimal SuggestedWindfall { get; set; }
        public decimal MonthlyEssentialExpenses { get; set; }
        public int DefaultEmergencyFundMonths { get; set; }
        public List<PlanDebtDto> Debts { get; set; } = new();
    }

    private class PlanDebtDto
    {
        public string Name { get; set; } = "";
        public decimal Balance { get; set; }
    }
}
