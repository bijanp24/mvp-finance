using System.Net;
using System.Net.Http.Json;
using FinanceEngine.Api.Endpoints;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceEngine.Tests.Endpoints;

public class SettingsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SettingsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext configuration
                services.RemoveAll<DbContextOptions<FinanceDbContext>>();
                services.RemoveAll<FinanceDbContext>();

                // Add in-memory database for testing
                services.AddDbContext<FinanceDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_" + Guid.NewGuid());
                });
            });
        });
    }

    [Fact]
    public async Task GetSettings_ReturnsDefaultSettings_WhenNoSettingsExist()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var settings = await response.Content.ReadFromJsonAsync<SettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal("BiWeekly", settings.PayFrequency);
        Assert.Equal(2500m, settings.PaycheckAmount);
        Assert.Equal(100m, settings.SafetyBuffer);
        Assert.Null(settings.NextPaycheckDate);
    }

    [Fact]
    public async Task UpdateSettings_CreatesNewSettings_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateSettingsRequest(
            PayFrequency: "Weekly",
            PaycheckAmount: 1500m,
            SafetyBuffer: 200m,
            NextPaycheckDate: DateTime.UtcNow.AddDays(7)
        );

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var settings = await response.Content.ReadFromJsonAsync<SettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal("Weekly", settings.PayFrequency);
        Assert.Equal(1500m, settings.PaycheckAmount);
        Assert.Equal(200m, settings.SafetyBuffer);
        Assert.NotNull(settings.NextPaycheckDate);
    }

    [Fact]
    public async Task UpdateSettings_InvalidPayFrequency_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateSettingsRequest(
            PayFrequency: "InvalidFrequency",
            PaycheckAmount: 1500m,
            SafetyBuffer: 200m,
            NextPaycheckDate: null
        );

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_NegativePaycheckAmount_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateSettingsRequest(
            PayFrequency: "BiWeekly",
            PaycheckAmount: -100m,
            SafetyBuffer: 200m,
            NextPaycheckDate: null
        );

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_NegativeSafetyBuffer_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new UpdateSettingsRequest(
            PayFrequency: "BiWeekly",
            PaycheckAmount: 2500m,
            SafetyBuffer: -100m,
            NextPaycheckDate: null
        );

        // Act
        var response = await client.PutAsJsonAsync("/api/settings", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSettings_DeactivatesPreviousSettings()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create first settings
        var request1 = new UpdateSettingsRequest(
            PayFrequency: "Weekly",
            PaycheckAmount: 1500m,
            SafetyBuffer: 200m,
            NextPaycheckDate: null
        );
        await client.PutAsJsonAsync("/api/settings", request1);

        // Create second settings
        var request2 = new UpdateSettingsRequest(
            PayFrequency: "BiWeekly",
            PaycheckAmount: 2500m,
            SafetyBuffer: 100m,
            NextPaycheckDate: null
        );
        await client.PutAsJsonAsync("/api/settings", request2);

        // Act - Get current settings
        var response = await client.GetAsync("/api/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var settings = await response.Content.ReadFromJsonAsync<SettingsDto>();
        Assert.NotNull(settings);
        Assert.Equal("BiWeekly", settings.PayFrequency);
        Assert.Equal(2500m, settings.PaycheckAmount);
        Assert.Equal(100m, settings.SafetyBuffer);
    }

    [Fact]
    public async Task GetSettings_ReturnsActiveSetting_WhenMultipleExist()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create multiple settings
        await client.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(
            PayFrequency: "Weekly",
            PaycheckAmount: 1000m,
            SafetyBuffer: 50m,
            NextPaycheckDate: null
        ));

        await client.PutAsJsonAsync("/api/settings", new UpdateSettingsRequest(
            PayFrequency: "Monthly",
            PaycheckAmount: 5000m,
            SafetyBuffer: 500m,
            NextPaycheckDate: null
        ));

        // Act
        var response = await client.GetAsync("/api/settings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var settings = await response.Content.ReadFromJsonAsync<SettingsDto>();
        Assert.NotNull(settings);
        // Should return the most recent (Monthly)
        Assert.Equal("Monthly", settings.PayFrequency);
        Assert.Equal(5000m, settings.PaycheckAmount);
        Assert.Equal(500m, settings.SafetyBuffer);
    }
}

