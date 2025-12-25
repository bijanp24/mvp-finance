using System.Net;
using System.Net.Http.Json;
using FinanceEngine.Api.Endpoints;
using FinanceEngine.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceEngine.Tests.Endpoints;

public class RecurringContributionEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RecurringContributionEndpointsTests(WebApplicationFactory<Program> factory)
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
    public async Task GetAll_ReturnsAllContributions()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create test accounts
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        // Create a recurring contribution
        var createRequest = new CreateRecurringContributionRequest(
            Name: "Monthly 401k",
            Amount: 500m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );
        await client.PostAsJsonAsync("/api/recurring-contributions", createRequest);

        // Act
        var response = await client.GetAsync("/api/recurring-contributions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contributions = await response.Content.ReadFromJsonAsync<List<RecurringContributionDto>>();
        Assert.NotNull(contributions);
        Assert.NotEmpty(contributions);
    }

    [Fact]
    public async Task GetById_ExistingId_ReturnsContribution()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var createRequest = new CreateRecurringContributionRequest(
            Name: "Weekly Savings",
            Amount: 100m,
            Frequency: "Weekly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );
        var createResponse = await client.PostAsJsonAsync("/api/recurring-contributions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringContributionDto>();

        // Act
        var response = await client.GetAsync($"/api/recurring-contributions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var contribution = await response.Content.ReadFromJsonAsync<RecurringContributionDto>();
        Assert.NotNull(contribution);
        Assert.Equal(created.Id, contribution.Id);
        Assert.Equal("Weekly Savings", contribution.Name);
        Assert.Equal(100m, contribution.Amount);
        Assert.Equal("Weekly", contribution.Frequency);
    }

    [Fact]
    public async Task GetById_NonExistingId_Returns404()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/recurring-contributions/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var request = new CreateRecurringContributionRequest(
            Name: "BiWeekly 401k",
            Amount: 250m,
            Frequency: "BiWeekly",
            NextContributionDate: DateTime.UtcNow.AddDays(14),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var contribution = await response.Content.ReadFromJsonAsync<RecurringContributionDto>();
        Assert.NotNull(contribution);
        Assert.Equal("BiWeekly 401k", contribution.Name);
        Assert.Equal(250m, contribution.Amount);
        Assert.Equal("BiWeekly", contribution.Frequency);
        Assert.True(contribution.IsActive);
        Assert.Equal("Test Cash", contribution.SourceAccountName);
        Assert.Equal("Test 401k", contribution.TargetAccountName);
    }

    [Fact]
    public async Task Create_InvalidSourceAccount_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var request = new CreateRecurringContributionRequest(
            Name: "Test Contribution",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: 99999, // Non-existent
            TargetAccountId: investmentAccount.Id
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_SourceEqualsTarget_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);

        var request = new CreateRecurringContributionRequest(
            Name: "Test Contribution",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: cashAccount.Id // Same as source
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_SourceNotCash_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var investmentAccount1 = await CreateTestAccount(client, "Test 401k 1", "Investment", 10000m);
        var investmentAccount2 = await CreateTestAccount(client, "Test 401k 2", "Investment", 5000m);

        var request = new CreateRecurringContributionRequest(
            Name: "Test Contribution",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: investmentAccount1.Id, // Not Cash
            TargetAccountId: investmentAccount2.Id
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_TargetIsDebt_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var debtAccount = await CreateTestAccount(client, "Test Credit Card", "Debt", 2000m, 0.18m, 50m);

        var request = new CreateRecurringContributionRequest(
            Name: "Test Contribution",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: debtAccount.Id // Debt not allowed
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ValidRequest_ReturnsUpdated()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var createRequest = new CreateRecurringContributionRequest(
            Name: "Original Name",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );
        var createResponse = await client.PostAsJsonAsync("/api/recurring-contributions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringContributionDto>();

        var updateRequest = new UpdateRecurringContributionRequest(
            Name: "Updated Name",
            Amount: 200m,
            Frequency: "BiWeekly",
            NextContributionDate: DateTime.UtcNow.AddDays(14),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id,
            IsActive: true
        );

        // Act
        var response = await client.PutAsJsonAsync($"/api/recurring-contributions/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<RecurringContributionDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(200m, updated.Amount);
        Assert.Equal("BiWeekly", updated.Frequency);
    }

    [Fact]
    public async Task Delete_SetsIsActiveFalse()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var createRequest = new CreateRecurringContributionRequest(
            Name: "To Delete",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );
        var createResponse = await client.PostAsJsonAsync("/api/recurring-contributions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringContributionDto>();

        // Act
        var response = await client.DeleteAsync($"/api/recurring-contributions/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify it's soft deleted (still exists but IsActive = false)
        var getResponse = await client.GetAsync($"/api/recurring-contributions/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var contribution = await getResponse.Content.ReadFromJsonAsync<RecurringContributionDto>();
        Assert.NotNull(contribution);
        Assert.False(contribution.IsActive);
    }

    [Fact]
    public async Task Toggle_FlipsActiveStatus()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var createRequest = new CreateRecurringContributionRequest(
            Name: "To Toggle",
            Amount: 100m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );
        var createResponse = await client.PostAsJsonAsync("/api/recurring-contributions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringContributionDto>();

        // Act - Toggle off
        var toggleResponse1 = await client.PatchAsync($"/api/recurring-contributions/{created!.Id}/toggle", null);
        Assert.Equal(HttpStatusCode.OK, toggleResponse1.StatusCode);
        var toggled1 = await toggleResponse1.Content.ReadFromJsonAsync<RecurringContributionDto>();
        Assert.False(toggled1!.IsActive);

        // Act - Toggle back on
        var toggleResponse2 = await client.PatchAsync($"/api/recurring-contributions/{created.Id}/toggle", null);
        Assert.Equal(HttpStatusCode.OK, toggleResponse2.StatusCode);
        var toggled2 = await toggleResponse2.Content.ReadFromJsonAsync<RecurringContributionDto>();
        Assert.True(toggled2!.IsActive);
    }

    [Fact]
    public async Task Preview_ReturnsNext12Occurrences()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var createRequest = new CreateRecurringContributionRequest(
            Name: "Monthly Preview Test",
            Amount: 500m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.Today.AddDays(5),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );
        var createResponse = await client.PostAsJsonAsync("/api/recurring-contributions", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<RecurringContributionDto>();

        // Act
        var response = await client.GetAsync($"/api/recurring-contributions/{created!.Id}/preview");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var preview = await response.Content.ReadFromJsonAsync<ContributionPreviewResponse>();
        Assert.NotNull(preview);
        Assert.Equal(created.Id, preview.Id);
        Assert.Equal("Monthly Preview Test", preview.Name);
        
        var occurrences = preview.Occurrences.ToList();
        Assert.Equal(12, occurrences.Count);
        
        // Verify all amounts are correct
        Assert.All(occurrences, o => Assert.Equal(500m, o.Amount));
        
        // Verify dates are in ascending order
        for (int i = 1; i < occurrences.Count; i++)
        {
            Assert.True(occurrences[i].Date > occurrences[i - 1].Date);
        }
    }

    [Fact]
    public async Task Create_ZeroAmount_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var request = new CreateRecurringContributionRequest(
            Name: "Zero Amount",
            Amount: 0m,
            Frequency: "Monthly",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidFrequency_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        var cashAccount = await CreateTestAccount(client, "Test Cash", "Cash", 5000m);
        var investmentAccount = await CreateTestAccount(client, "Test 401k", "Investment", 10000m);

        var request = new CreateRecurringContributionRequest(
            Name: "Invalid Frequency",
            Amount: 100m,
            Frequency: "InvalidFrequency",
            NextContributionDate: DateTime.UtcNow.AddDays(7),
            SourceAccountId: cashAccount.Id,
            TargetAccountId: investmentAccount.Id
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/recurring-contributions", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Helper methods
    private async Task<TestAccountDto> CreateTestAccount(
        HttpClient client,
        string name,
        string type,
        decimal initialBalance,
        decimal? apr = null,
        decimal? minimumPayment = null)
    {
        var request = new CreateAccountRequest(
            Name: name,
            Type: type,
            InitialBalance: initialBalance,
            AnnualPercentageRate: apr,
            MinimumPayment: minimumPayment
        );

        var response = await client.PostAsJsonAsync("/api/accounts", request);
        response.EnsureSuccessStatusCode();
        var account = await response.Content.ReadFromJsonAsync<TestAccountDto>();
        return account!;
    }
}

// Helper DTOs for testing
public record TestAccountDto(
    int Id,
    string Name,
    string Type,
    decimal InitialBalance,
    decimal? AnnualPercentageRate,
    decimal? MinimumPayment
);

public record CreateAccountRequest(
    string Name,
    string Type,
    decimal InitialBalance,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null
);

public record CreateRecurringContributionRequest(
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId
);

public record UpdateRecurringContributionRequest(
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId,
    bool IsActive
);

public record RecurringContributionDto(
    int Id,
    string Name,
    decimal Amount,
    string Frequency,
    DateTime NextContributionDate,
    int SourceAccountId,
    int TargetAccountId,
    string? SourceAccountName,
    string? TargetAccountName,
    bool IsActive,
    DateTime CreatedAt
);

public record ContributionPreviewResponse(
    int Id,
    string Name,
    IEnumerable<ContributionOccurrence> Occurrences
);

public record ContributionOccurrence(
    DateTime Date,
    decimal Amount
);

