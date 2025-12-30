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

public class GoalEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GoalEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = "TestDatabase_Goals_" + Guid.NewGuid();
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

    #region GetAllGoals Tests

    [Fact]
    public async Task GetAllGoals_EmptyDatabase_ReturnsEmptyList()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/goals");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goals = await response.Content.ReadFromJsonAsync<List<GoalDto>>();
        Assert.NotNull(goals);
        Assert.Empty(goals);
    }

    [Fact]
    public async Task GetAllGoals_WithGoals_ReturnsOrderedByPriority()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create goals with different priorities
        await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Low Priority",
            Type: "SavingsGoal",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 3,
            Notes: null
        ));

        await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "High Priority",
            Type: "SavingsGoal",
            TargetAmount: 5000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));

        // Act
        var response = await client.GetAsync("/api/goals");

        // Assert
        var goals = await response.Content.ReadFromJsonAsync<List<GoalDto>>();
        Assert.NotNull(goals);
        Assert.Equal(2, goals.Count);
        Assert.Equal("High Priority", goals[0].Name);
        Assert.Equal("Low Priority", goals[1].Name);
    }

    #endregion

    #region GetGoalById Tests

    [Fact]
    public async Task GetGoalById_ExistingGoal_ReturnsGoal()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Test Goal",
            Type: "InvestmentTarget",
            TargetAmount: 50000,
            TargetDate: DateTime.UtcNow.AddYears(5),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: "Test notes"
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalDto>();

        // Act
        var response = await client.GetAsync($"/api/goals/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var goal = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(goal);
        Assert.Equal("Test Goal", goal.Name);
        Assert.Equal("InvestmentTarget", goal.Type);
        Assert.Equal(50000, goal.TargetAmount);
    }

    [Fact]
    public async Task GetGoalById_NonExistingGoal_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/goals/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GetGoalProgress Tests

    [Fact]
    public async Task GetGoalProgress_ExistingGoal_ReturnsProgressDetails()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Progress Test",
            Type: "SavingsGoal",
            TargetAmount: 10000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalDto>();

        // Act
        var response = await client.GetAsync($"/api/goals/{created!.Id}/progress");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var progress = await response.Content.ReadFromJsonAsync<GoalProgressDto>();
        Assert.NotNull(progress);
        Assert.Equal(created.Id, progress.GoalId);
        Assert.Equal("Progress Test", progress.GoalName);
        Assert.Equal(10000, progress.TargetValue);
    }

    #endregion

    #region CreateGoal Tests

    [Fact]
    public async Task CreateGoal_ValidSavingsGoal_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Vacation Fund",
            Type: "SavingsGoal",
            TargetAmount: 5000,
            TargetDate: DateTime.UtcNow.AddMonths(6),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: "Summer vacation"
        ));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var goal = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(goal);
        Assert.Equal("Vacation Fund", goal.Name);
        Assert.Equal("SavingsGoal", goal.Type);
        Assert.Equal(5000, goal.TargetAmount);
        Assert.True(goal.IsActive);
    }

    [Fact]
    public async Task CreateGoal_ValidDebtFreeGoal_SucceedsWithoutTargetAmount()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Be Debt Free",
            Type: "DebtFree",
            TargetAmount: null,
            TargetDate: DateTime.UtcNow.AddYears(2),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var goal = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(goal);
        Assert.Equal("DebtFree", goal.Type);
        Assert.Null(goal.TargetAmount);
    }

    [Fact]
    public async Task CreateGoal_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "",
            Type: "SavingsGoal",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: null,
            Notes: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGoal_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Invalid Goal",
            Type: "InvalidType",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: null,
            Notes: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGoal_SavingsGoalWithoutAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Missing Amount",
            Type: "SavingsGoal",
            TargetAmount: null,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: null,
            Notes: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGoal_PastTargetDate_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Past Goal",
            Type: "SavingsGoal",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddDays(-1),
            LinkedAccountIds: null,
            Priority: null,
            Notes: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region UpdateGoal Tests

    [Fact]
    public async Task UpdateGoal_ValidUpdate_ReturnsUpdatedGoal()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "Original Name",
            Type: "SavingsGoal",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalDto>();

        // Act
        var response = await client.PutAsJsonAsync($"/api/goals/{created!.Id}", new UpdateGoalRequest(
            Name: "Updated Name",
            Type: null,
            TargetAmount: 2000,
            TargetDate: null,
            LinkedAccountIds: null,
            Priority: 2,
            Notes: "Added notes",
            IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(2000, updated.TargetAmount);
        Assert.Equal(2, updated.Priority);
    }

    [Fact]
    public async Task UpdateGoal_DeactivateGoal_SetsIsActiveFalse()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "To Deactivate",
            Type: "SavingsGoal",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalDto>();

        // Act
        var response = await client.PutAsJsonAsync($"/api/goals/{created!.Id}", new UpdateGoalRequest(
            Name: null, Type: null, TargetAmount: null, TargetDate: null,
            LinkedAccountIds: null, Priority: null, Notes: null, IsActive: false
        ));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateGoal_NonExistingGoal_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync("/api/goals/99999", new UpdateGoalRequest(
            Name: "Update",
            Type: null, TargetAmount: null, TargetDate: null,
            LinkedAccountIds: null, Priority: null, Notes: null, IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region DeleteGoal Tests

    [Fact]
    public async Task DeleteGoal_ExistingGoal_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: "To Delete",
            Type: "SavingsGoal",
            TargetAmount: 1000,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<GoalDto>();

        // Act
        var response = await client.DeleteAsync($"/api/goals/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deleted
        var getResponse = await client.GetAsync($"/api/goals/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteGoal_NonExistingGoal_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/goals/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Goal Types Tests

    [Theory]
    [InlineData("DebtFree")]
    [InlineData("InvestmentTarget")]
    [InlineData("SavingsGoal")]
    [InlineData("NetWorthMilestone")]
    public async Task CreateGoal_AllGoalTypes_Succeed(string goalType)
    {
        // Arrange
        var client = _factory.CreateClient();
        decimal? targetAmount = goalType == "DebtFree" ? null : 10000;

        // Act
        var response = await client.PostAsJsonAsync("/api/goals", new CreateGoalRequest(
            Name: $"Test {goalType}",
            Type: goalType,
            TargetAmount: targetAmount,
            TargetDate: DateTime.UtcNow.AddYears(1),
            LinkedAccountIds: null,
            Priority: 1,
            Notes: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var goal = await response.Content.ReadFromJsonAsync<GoalDto>();
        Assert.NotNull(goal);
        Assert.Equal(goalType, goal.Type);
    }

    #endregion
}
