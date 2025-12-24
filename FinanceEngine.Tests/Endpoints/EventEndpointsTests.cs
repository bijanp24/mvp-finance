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

public class EventEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EventEndpointsTests(WebApplicationFactory<Program> factory)
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
    public async Task CreateEvent_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateEventRequest(
            Date: DateTime.UtcNow,
            Type: "Income",
            Amount: 1000m,
            Description: "Test income",
            AccountId: 1,
            TargetAccountId: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/events", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var evt = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.NotNull(evt);
        Assert.Equal("Income", evt.Type);
        Assert.Equal(1000m, evt.Amount);
    }

    [Fact]
    public async Task UpdateEvent_ValidRequest_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create an event first
        var createRequest = new CreateEventRequest(
            Date: DateTime.UtcNow,
            Type: "Expense",
            Amount: 50m,
            Description: "Original",
            AccountId: 1,
            TargetAccountId: null
        );
        var createResponse = await client.PostAsJsonAsync("/api/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();

        // Update the event
        var updateRequest = new UpdateEventRequest(
            Amount: 75m,
            Description: "Updated"
        );

        // Act
        var response = await client.PutAsJsonAsync($"/api/events/{createdEvent!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var updated = await response.Content.ReadFromJsonAsync<EventDto>();
        Assert.NotNull(updated);
        Assert.Equal(75m, updated.Amount);
        Assert.Equal("Updated", updated.Description);
    }

    [Fact]
    public async Task UpdateEvent_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateEventRequest(Amount: 100m);

        // Act
        var response = await client.PutAsJsonAsync("/api/events/99999", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_ExistingEvent_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Create an event
        var createRequest = new CreateEventRequest(
            Date: DateTime.UtcNow,
            Type: "Expense",
            Amount: 25m,
            Description: "To delete",
            AccountId: 1,
            TargetAccountId: null
        );
        var createResponse = await client.PostAsJsonAsync("/api/events", createRequest);
        var createdEvent = await createResponse.Content.ReadFromJsonAsync<EventDto>();

        // Act
        var response = await client.DeleteAsync($"/api/events/{createdEvent!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/events/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/events");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.NotNull(events);
    }

    [Fact]
    public async Task GetRecentEvents_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/events/recent?days=30");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var events = await response.Content.ReadFromJsonAsync<List<EventDto>>();
        Assert.NotNull(events);
    }
}


