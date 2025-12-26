using System.Net;
using System.Net.Http.Json;
using FinanceEngine.Api.Endpoints;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceEngine.Tests.Endpoints;

public class BudgetEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BudgetEndpointsTests(WebApplicationFactory<Program> factory)
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

    private async Task<BudgetDto> CreateTestBudget(HttpClient client, int categoryId = 1, decimal amount = 500m)
    {
        var request = new CreateBudgetRequest(
            CategoryId: categoryId,
            Amount: amount,
            Frequency: "Monthly",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: "Test budget"
        );

        var response = await client.PostAsJsonAsync("/api/budgets", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BudgetDto>())!;
    }

    #region GetAllBudgets Tests

    [Fact]
    public async Task GetAllBudgets_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/budgets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetDto>>();
        Assert.NotNull(budgets);
        Assert.Empty(budgets);
    }

    [Fact]
    public async Task GetAllBudgets_WithBudgets_ReturnsList()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateTestBudget(client, categoryId: 1, amount: 100m);
        await CreateTestBudget(client, categoryId: 2, amount: 200m);

        // Act
        var response = await client.GetAsync("/api/budgets");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetDto>>();
        Assert.NotNull(budgets);
        Assert.Equal(2, budgets.Count);
    }

    [Fact]
    public async Task GetAllBudgets_FilterByCategoryId_ReturnsFiltered()
    {
        // Arrange
        var client = _factory.CreateClient();
        await CreateTestBudget(client, categoryId: 1, amount: 100m);
        await CreateTestBudget(client, categoryId: 2, amount: 200m);
        await CreateTestBudget(client, categoryId: 1, amount: 300m);

        // Act
        var response = await client.GetAsync("/api/budgets?categoryId=1");

        // Assert
        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetDto>>();
        Assert.NotNull(budgets);
        Assert.Equal(2, budgets.Count);
        Assert.All(budgets, b => Assert.Equal(1, b.CategoryId));
    }

    [Fact]
    public async Task GetAllBudgets_ActiveOnlyFalse_ReturnsInactive()
    {
        // Arrange
        var client = _factory.CreateClient();
        var budget = await CreateTestBudget(client);

        // Deactivate budget
        await client.PutAsJsonAsync($"/api/budgets/{budget.Id}", new UpdateBudgetRequest(
            CategoryId: null, Amount: null, Frequency: null, EffectiveDate: null,
            EndDate: null, ClearEndDate: null, LinkedAccountId: null, ClearLinkedAccount: null,
            Notes: null, IsActive: false
        ));

        // Act
        var response = await client.GetAsync("/api/budgets?activeOnly=false");

        // Assert
        var budgets = await response.Content.ReadFromJsonAsync<List<BudgetDto>>();
        Assert.NotNull(budgets);
        Assert.Contains(budgets, b => b.Id == budget.Id && !b.IsActive);
    }

    #endregion

    #region GetBudgetById Tests

    [Fact]
    public async Task GetBudgetById_ExistingBudget_ReturnsBudget()
    {
        // Arrange
        var client = _factory.CreateClient();
        var created = await CreateTestBudget(client);

        // Act
        var response = await client.GetAsync($"/api/budgets/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var budget = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(budget);
        Assert.Equal(created.Id, budget.Id);
        Assert.Equal(created.Amount, budget.Amount);
    }

    [Fact]
    public async Task GetBudgetById_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/budgets/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region CreateBudget Tests

    [Fact]
    public async Task CreateBudget_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 1,
            Amount: 1000m,
            Frequency: "Monthly",
            EffectiveDate: new DateTime(2025, 1, 1),
            EndDate: null,
            LinkedAccountId: null,
            Notes: "Test notes"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var budget = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(budget);
        Assert.Equal(1, budget.CategoryId);
        Assert.Equal("Housing", budget.CategoryName); // Seeded category
        Assert.Equal(1000m, budget.Amount);
        Assert.Equal("Monthly", budget.Frequency);
        Assert.Equal("Test notes", budget.Notes);
        Assert.True(budget.IsActive);
    }

    [Fact]
    public async Task CreateBudget_WeeklyFrequency_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 6, // Groceries
            Amount: 150m,
            Frequency: "Weekly",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var budget = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(budget);
        Assert.Equal("Weekly", budget.Frequency);
    }

    [Fact]
    public async Task CreateBudget_BiWeeklyFrequency_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 8, // Transportation
            Amount: 200m,
            Frequency: "BiWeekly",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var budget = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(budget);
        Assert.Equal("BiWeekly", budget.Frequency);
    }

    [Fact]
    public async Task CreateBudget_WithEndDate_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var effectiveDate = new DateTime(2025, 1, 1);
        var endDate = new DateTime(2025, 12, 31);
        var request = new CreateBudgetRequest(
            CategoryId: 1,
            Amount: 500m,
            Frequency: "Monthly",
            EffectiveDate: effectiveDate,
            EndDate: endDate,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var budget = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(budget);
        Assert.Equal(endDate, budget.EndDate);
    }

    [Fact]
    public async Task CreateBudget_InvalidCategoryId_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 9999,
            Amount: 500m,
            Frequency: "Monthly",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBudget_ZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 1,
            Amount: 0m,
            Frequency: "Monthly",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBudget_NegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 1,
            Amount: -100m,
            Frequency: "Monthly",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBudget_InvalidFrequency_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 1,
            Amount: 500m,
            Frequency: "Daily",
            EffectiveDate: DateTime.UtcNow.Date,
            EndDate: null,
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBudget_EndDateBeforeEffectiveDate_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateBudgetRequest(
            CategoryId: 1,
            Amount: 500m,
            Frequency: "Monthly",
            EffectiveDate: new DateTime(2025, 6, 1),
            EndDate: new DateTime(2025, 1, 1),
            LinkedAccountId: null,
            Notes: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/budgets", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region UpdateBudget Tests

    [Fact]
    public async Task UpdateBudget_ValidRequest_ReturnsUpdated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var created = await CreateTestBudget(client);

        // Act
        var response = await client.PutAsJsonAsync($"/api/budgets/{created.Id}", new UpdateBudgetRequest(
            CategoryId: 2, // Utilities
            Amount: 750m,
            Frequency: "BiWeekly",
            EffectiveDate: null,
            EndDate: null,
            ClearEndDate: null,
            LinkedAccountId: null,
            ClearLinkedAccount: null,
            Notes: "Updated notes",
            IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(updated);
        Assert.Equal(2, updated.CategoryId);
        Assert.Equal("Utilities", updated.CategoryName);
        Assert.Equal(750m, updated.Amount);
        Assert.Equal("BiWeekly", updated.Frequency);
        Assert.Equal("Updated notes", updated.Notes);
    }

    [Fact]
    public async Task UpdateBudget_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var client = _factory.CreateClient();
        var created = await CreateTestBudget(client);

        // Act - Only update amount
        var response = await client.PutAsJsonAsync($"/api/budgets/{created.Id}", new UpdateBudgetRequest(
            CategoryId: null,
            Amount: 999m,
            Frequency: null,
            EffectiveDate: null,
            EndDate: null,
            ClearEndDate: null,
            LinkedAccountId: null,
            ClearLinkedAccount: null,
            Notes: null,
            IsActive: null
        ));

        // Assert
        var updated = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(updated);
        Assert.Equal(999m, updated.Amount);
        Assert.Equal(created.CategoryId, updated.CategoryId); // Unchanged
        Assert.Equal(created.Frequency, updated.Frequency);   // Unchanged
    }

    [Fact]
    public async Task UpdateBudget_Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var client = _factory.CreateClient();
        var created = await CreateTestBudget(client);

        // Act
        var response = await client.PutAsJsonAsync($"/api/budgets/{created.Id}", new UpdateBudgetRequest(
            CategoryId: null, Amount: null, Frequency: null, EffectiveDate: null,
            EndDate: null, ClearEndDate: null, LinkedAccountId: null, ClearLinkedAccount: null,
            Notes: null, IsActive: false
        ));

        // Assert
        var updated = await response.Content.ReadFromJsonAsync<BudgetDto>();
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateBudget_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync("/api/budgets/9999", new UpdateBudgetRequest(
            CategoryId: null, Amount: 100m, Frequency: null, EffectiveDate: null,
            EndDate: null, ClearEndDate: null, LinkedAccountId: null, ClearLinkedAccount: null,
            Notes: null, IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBudget_InvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var created = await CreateTestBudget(client);

        // Act
        var response = await client.PutAsJsonAsync($"/api/budgets/{created.Id}", new UpdateBudgetRequest(
            CategoryId: null, Amount: -50m, Frequency: null, EffectiveDate: null,
            EndDate: null, ClearEndDate: null, LinkedAccountId: null, ClearLinkedAccount: null,
            Notes: null, IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DeleteBudget Tests

    [Fact]
    public async Task DeleteBudget_Existing_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        var created = await CreateTestBudget(client);

        // Act
        var response = await client.DeleteAsync($"/api/budgets/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await client.GetAsync($"/api/budgets/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteBudget_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/budgets/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
