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

public class CategoryEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CategoryEndpointsTests(WebApplicationFactory<Program> factory)
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

    #region GetAllCategories Tests

    [Fact]
    public async Task GetAllCategories_ReturnsSeededCategories()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);
        Assert.True(categories.Count >= 15); // At least the 15 seeded categories
    }

    [Fact]
    public async Task GetAllCategories_ReturnsOrderedBySortOrder()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/categories");

        // Assert
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);

        // Verify ordering
        for (int i = 1; i < categories.Count; i++)
        {
            Assert.True(categories[i - 1].SortOrder <= categories[i].SortOrder);
        }
    }

    [Fact]
    public async Task GetAllCategories_WithActiveOnlyFalse_ReturnsInactiveCategories()
    {
        // Arrange
        var client = _factory.CreateClient();

        // First create and deactivate a category
        var createResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Inactive Test",
            Type: "OneTime",
            Icon: "test",
            Color: "#000000",
            SortOrder: 100
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        await client.PutAsJsonAsync($"/api/categories/{created!.Id}", new UpdateCategoryRequest(
            Name: null, Type: null, Icon: null, Color: null, SortOrder: null, IsActive: false
        ));

        // Act
        var response = await client.GetAsync("/api/categories?activeOnly=false");

        // Assert
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.NotNull(categories);
        Assert.Contains(categories, c => c.Name == "Inactive Test" && !c.IsActive);
    }

    #endregion

    #region GetCategoryById Tests

    [Fact]
    public async Task GetCategoryById_ExistingCategory_ReturnsCategory()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Get seeded category (Housing = id 1)
        var response = await client.GetAsync("/api/categories/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        Assert.Equal(1, category.Id);
        Assert.Equal("Housing", category.Name);
        Assert.Equal("Recurring", category.Type);
    }

    [Fact]
    public async Task GetCategoryById_NonExistingCategory_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/categories/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region CreateCategory Tests

    [Fact]
    public async Task CreateCategory_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateCategoryRequest(
            Name: "Test Category",
            Type: "OneTime",
            Icon: "star",
            Color: "#FF5733",
            SortOrder: 50
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        Assert.Equal("Test Category", category.Name);
        Assert.Equal("OneTime", category.Type);
        Assert.Equal("star", category.Icon);
        Assert.Equal("#FF5733", category.Color);
        Assert.Equal(50, category.SortOrder);
        Assert.True(category.IsActive);
    }

    [Fact]
    public async Task CreateCategory_RecurringType_ReturnsCreated()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateCategoryRequest(
            Name: "New Recurring",
            Type: "Recurring",
            Icon: "repeat",
            Color: "#00FF00",
            SortOrder: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(category);
        Assert.Equal("Recurring", category.Type);
    }

    [Fact]
    public async Task CreateCategory_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateCategoryRequest(
            Name: "",
            Type: "OneTime",
            Icon: null,
            Color: null,
            SortOrder: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new CreateCategoryRequest(
            Name: "Test",
            Type: "InvalidType",
            Icon: null,
            Color: null,
            SortOrder: null
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/categories", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create first category
        await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Unique Category",
            Type: "OneTime",
            Icon: null,
            Color: null,
            SortOrder: null
        ));

        // Try to create duplicate
        var response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Unique Category",
            Type: "Recurring",
            Icon: null,
            Color: null,
            SortOrder: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_DuplicateNameCaseInsensitive_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create first category
        await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Case Test",
            Type: "OneTime",
            Icon: null,
            Color: null,
            SortOrder: null
        ));

        // Try to create with different case
        var response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "CASE TEST",
            Type: "OneTime",
            Icon: null,
            Color: null,
            SortOrder: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region UpdateCategory Tests

    [Fact]
    public async Task UpdateCategory_ValidRequest_ReturnsUpdated()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a category first
        var createResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Update Test",
            Type: "OneTime",
            Icon: "old",
            Color: "#000000",
            SortOrder: 10
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        // Act
        var response = await client.PutAsJsonAsync($"/api/categories/{created!.Id}", new UpdateCategoryRequest(
            Name: "Updated Name",
            Type: "Recurring",
            Icon: "new",
            Color: "#FFFFFF",
            SortOrder: 20,
            IsActive: true
        ));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Recurring", updated.Type);
        Assert.Equal("new", updated.Icon);
        Assert.Equal("#FFFFFF", updated.Color);
        Assert.Equal(20, updated.SortOrder);
    }

    [Fact]
    public async Task UpdateCategory_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Partial Test",
            Type: "OneTime",
            Icon: "star",
            Color: "#123456",
            SortOrder: 30
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        // Act - Only update name
        var response = await client.PutAsJsonAsync($"/api/categories/{created!.Id}", new UpdateCategoryRequest(
            Name: "New Name Only",
            Type: null,
            Icon: null,
            Color: null,
            SortOrder: null,
            IsActive: null
        ));

        // Assert
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(updated);
        Assert.Equal("New Name Only", updated.Name);
        Assert.Equal("OneTime", updated.Type); // Unchanged
        Assert.Equal("star", updated.Icon);     // Unchanged
        Assert.Equal("#123456", updated.Color); // Unchanged
        Assert.Equal(30, updated.SortOrder);    // Unchanged
    }

    [Fact]
    public async Task UpdateCategory_Deactivate_SetsIsActiveFalse()
    {
        // Arrange
        var client = _factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Deactivate Test",
            Type: "OneTime",
            Icon: null,
            Color: null,
            SortOrder: null
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        // Act
        var response = await client.PutAsJsonAsync($"/api/categories/{created!.Id}", new UpdateCategoryRequest(
            Name: null, Type: null, Icon: null, Color: null, SortOrder: null, IsActive: false
        ));

        // Assert
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>();
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateCategory_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PutAsJsonAsync("/api/categories/9999", new UpdateCategoryRequest(
            Name: "Test", Type: null, Icon: null, Color: null, SortOrder: null, IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_InvalidType_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Update seeded category with invalid type
        var response = await client.PutAsJsonAsync("/api/categories/1", new UpdateCategoryRequest(
            Name: null, Type: "Invalid", Icon: null, Color: null, SortOrder: null, IsActive: null
        ));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DeleteCategory Tests

    [Fact]
    public async Task DeleteCategory_ExistingCategoryNoRelations_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Create a category to delete
        var createResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest(
            Name: "Delete Test",
            Type: "OneTime",
            Icon: null,
            Color: null,
            SortOrder: null
        ));
        var created = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        // Act
        var response = await client.DeleteAsync($"/api/categories/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await client.GetAsync($"/api/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_NonExisting_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync("/api/categories/9999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
